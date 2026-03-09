#define __MOJOSHADER_INTERNAL__ 1
#include "mojoshader_internal.h"
#ifndef MOJOSHADER_USE_SDL_STDLIB
#include <math.h>
#endif /* MOJOSHADER_USE_SDL_STDLIB */

// Convenience functions for allocators...
#if !MOJOSHADER_FORCE_ALLOCATOR
static char zeromalloc = 0;
void * MOJOSHADERCALL MOJOSHADER_internal_malloc(int bytes, void *d)
{
    return (bytes == 0) ? &zeromalloc : malloc(bytes);
} // MOJOSHADER_internal_malloc
void MOJOSHADERCALL MOJOSHADER_internal_free(void *ptr, void *d)
{
    if ((ptr != &zeromalloc) && (ptr != NULL))
        free(ptr);
} // MOJOSHADER_internal_free
#endif

MOJOSHADER_error MOJOSHADER_out_of_mem_error = {
    "Out of memory", NULL, MOJOSHADER_POSITION_NONE
};

MOJOSHADER_parseData MOJOSHADER_out_of_mem_data = {
    1, &MOJOSHADER_out_of_mem_error, 0, 0, 0, 0,
    MOJOSHADER_TYPE_UNKNOWN, 0, 0, 0, 0, 0, 0, 0,
    0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
};


typedef struct HashItem
{
    const void *key;
    const void *value;
    struct HashItem *next;
} HashItem;

struct HashTable
{
    HashItem **table;
    uint32 table_len;
    int stackable;
    void *data;
    HashTable_HashFn hash;
    HashTable_KeyMatchFn keymatch;
    HashTable_NukeFn nuke;
    MOJOSHADER_malloc m;
    MOJOSHADER_free f;
    void *d;
};

static inline uint32 calc_hash(const HashTable *table, const void *key)
{
    return table->hash(key, table->data) & (table->table_len-1);
} // calc_hash

int hash_find(const HashTable *table, const void *key, const void **_value)
{
    HashItem *i;
    void *data = table->data;
    const uint32 hash = calc_hash(table, key);
    HashItem *prev = NULL;
    for (i = table->table[hash]; i != NULL; i = i->next)
    {
        if (table->keymatch(key, i->key, data))
        {
            if (_value != NULL)
                *_value = i->value;

            // Matched! Move to the front of list for faster lookup next time.
            //  (stackable tables have to remain in the same order, though!)
            if ((!table->stackable) && (prev != NULL))
            {
                assert(prev->next == i);
                prev->next = i->next;
                i->next = table->table[hash];
                table->table[hash] = i;
            } // if

            return 1;
        } // if

        prev = i;
    } // for

    return 0;
} // hash_find

int hash_iter(const HashTable *table, const void *key,
              const void **_value, void **iter)
{
    HashItem *item = (HashItem *) *iter;
    if (item == NULL)
        item = table->table[calc_hash(table, key)];
    else
        item = item->next;

    while (item != NULL)
    {
        if (table->keymatch(key, item->key, table->data))
        {
            *_value = item->value;
            *iter = item;
            return 1;
        } // if
        item = item->next;
    } // while

    // no more matches.
    *_value = NULL;
    *iter = NULL;
    return 0;
} // hash_iter

int hash_iter_keys(const HashTable *table, const void **_key, void **iter)
{
    HashItem *item = (HashItem *) *iter;
    uint32 idx = 0;

    if (item != NULL)
    {
        const HashItem *orig = item;
        item = item->next;
        if (item == NULL)
            idx = calc_hash(table, orig->key) + 1;
    } // if

    while (!item && (idx < table->table_len))
        item = table->table[idx++];  // skip empty buckets...

    if (item == NULL)  // no more matches?
    {
        *_key = NULL;
        *iter = NULL;
        return 0;
    } // if

    *_key = item->key;
    *iter = item;
    return 1;
} // hash_iter_keys

int hash_insert(HashTable *table, const void *key, const void *value)
{
    HashItem *item = NULL;
    const uint32 hash = calc_hash(table, key);
    if ( (!table->stackable) && (hash_find(table, key, NULL)) )
        return 0;

    // !!! FIXME: grow and rehash table if it gets too saturated.
    item = (HashItem *) table->m(sizeof (HashItem), table->d);
    if (item == NULL)
        return -1;

    item->key = key;
    item->value = value;
    item->next = table->table[hash];
    table->table[hash] = item;

    return 1;
} // hash_insert

HashTable *hash_create(void *data, const HashTable_HashFn hashfn,
              const HashTable_KeyMatchFn keymatchfn,
              const HashTable_NukeFn nukefn,
              const int stackable,
              MOJOSHADER_malloc m, MOJOSHADER_free f, void *d)
{
    const uint32 initial_table_size = 256;
    const uint32 alloc_len = sizeof (HashItem *) * initial_table_size;
    HashTable *table = (HashTable *) m(sizeof (HashTable), d);
    if (table == NULL)
        return NULL;
    memset(table, '\0', sizeof (HashTable));

    table->table = (HashItem **) m(alloc_len, d);
    if (table->table == NULL)
    {
        f(table, d);
        return NULL;
    } // if

    memset(table->table, '\0', alloc_len);
    table->table_len = initial_table_size;
    table->stackable = stackable;
    table->data = data;
    table->hash = hashfn;
    table->keymatch = keymatchfn;
    table->nuke = nukefn;
    table->m = m;
    table->f = f;
    table->d = d;
    return table;
} // hash_create

void hash_destroy(HashTable *table, const void *ctx)
{
    uint32 i;
    void *data = table->data;
    MOJOSHADER_free f = table->f;
    void *d = table->d;
    for (i = 0; i < table->table_len; i++)
    {
        HashItem *item = table->table[i];
        while (item != NULL)
        {
            HashItem *next = item->next;
            table->nuke(ctx, item->key, item->value, data);
            f(item, d);
            item = next;
        } // while
    } // for

    f(table->table, d);
    f(table, d);
} // hash_destroy

int hash_remove(HashTable *table, const void *key, const void *ctx)
{
    HashItem *item = NULL;
    HashItem *prev = NULL;
    void *data = table->data;
    const uint32 hash = calc_hash(table, key);
    for (item = table->table[hash]; item != NULL; item = item->next)
    {
        if (table->keymatch(key, item->key, data))
        {
            if (prev != NULL)
                prev->next = item->next;
            else
                table->table[hash] = item->next;

            table->nuke(ctx, item->key, item->value, data);
            table->f(item, table->d);
            return 1;
        } // if

        prev = item;
    } // for

    return 0;
} // hash_remove


// this is djb's xor hashing function.
static inline uint32 hash_string_djbxor(const char *str, size_t len)
{
    register uint32 hash = 5381;
    while (len--)
        hash = ((hash << 5) + hash) ^ *(str++);
    return hash;
} // hash_string_djbxor

static inline uint32 hash_string(const char *str, size_t len)
{
    return hash_string_djbxor(str, len);
} // hash_string

uint32 hash_hash_string(const void *sym, void *data)
{
    (void) data;
    return hash_string((const char*) sym, strlen((const char *) sym));
} // hash_hash_string

int hash_keymatch_string(const void *a, const void *b, void *data)
{
    (void) data;
    return (strcmp((const char *) a, (const char *) b) == 0);
} // hash_keymatch_string


// string -> string map...

static void stringmap_nuke_noop(const void *ctx, const void *key, const void *val, void *d) {}

static void stringmap_nuke(const void *ctx, const void *key, const void *val, void *d)
{
    StringMap *smap = (StringMap *) d;
    smap->f((void *) key, smap->d);
    smap->f((void *) val, smap->d);
} // stringmap_nuke

StringMap *stringmap_create(const int copy, MOJOSHADER_malloc m,
                            MOJOSHADER_free f, void *d)
{
    HashTable_NukeFn nuke = copy ? stringmap_nuke : stringmap_nuke_noop;
    StringMap *smap;
    smap = hash_create(0,hash_hash_string,hash_keymatch_string,nuke,0,m,f,d);
    if (smap != NULL)
        smap->data = smap;
    return smap;
} // stringmap_create

void stringmap_destroy(StringMap *smap)
{
    hash_destroy(smap, NULL);
} // stringmap_destroy

int stringmap_insert(StringMap *smap, const char *key, const char *value)
{
    assert(key != NULL);
    if (smap->nuke == stringmap_nuke_noop)  // no copy?
        return hash_insert(smap, key, value);

    int rc = -1;
    char *k = (char *) smap->m(strlen(key) + 1, smap->d);
    char *v = (char *) (value ? smap->m(strlen(value) + 1, smap->d) : NULL);
    int failed = ( (!k) || ((!v) && (value)) );

    if (!failed)
    {
        strcpy(k, key);
        if (value != NULL)
            strcpy(v, value);
        failed = ((rc = hash_insert(smap, k, v)) <= 0);
    } // if

    if (failed)
    {
        smap->f(k, smap->d);
        smap->f(v, smap->d);
    } // if

    return rc;
} // stringmap_insert

int stringmap_remove(StringMap *smap, const char *key)
{
    return hash_remove(smap, key, NULL);
} // stringmap_remove

int stringmap_find(const StringMap *smap, const char *key, const char **_value)
{
    const void *value = NULL;
    const int retval = hash_find(smap, key, &value);
    *_value = (const char *) value;
    return retval;
} // stringmap_find


// The string cache...   !!! FIXME: use StringMap internally for this.

typedef struct StringBucket
{
    char *string;
    struct StringBucket *next;
} StringBucket;

struct StringCache
{
    StringBucket **hashtable;
    uint32 table_size;
    MOJOSHADER_malloc m;
    MOJOSHADER_free f;
    void *d;
};


const char *stringcache(StringCache *cache, const char *str)
{
    return stringcache_len(cache, str, strlen(str));
} // stringcache

static const char *stringcache_len_internal(StringCache *cache,
                                            const char *str,
                                            const unsigned int len,
                                            const int addmissing)
{
    const uint8 hash = hash_string(str, len) & (cache->table_size-1);
    StringBucket *bucket = cache->hashtable[hash];
    StringBucket *prev = NULL;
    while (bucket)
    {
        const char *bstr = bucket->string;
        if ((strncmp(bstr, str, len) == 0) && (bstr[len] == 0))
        {
            // Matched! Move this to the front of the list.
            if (prev != NULL)
            {
                assert(prev->next == bucket);
                prev->next = bucket->next;
                bucket->next = cache->hashtable[hash];
                cache->hashtable[hash] = bucket;
            } // if
            return bstr; // already cached
        } // if
        prev = bucket;
        bucket = bucket->next;
    } // while

    // no match!
    if (!addmissing)
        return NULL;

    // add to the table.
    bucket = (StringBucket *) cache->m(sizeof (StringBucket) + len + 1, cache->d);
    if (bucket == NULL)
        return NULL;
    bucket->string = (char *)(bucket + 1);
    memcpy(bucket->string, str, len);
    bucket->string[len] = '\0';
    bucket->next = cache->hashtable[hash];
    cache->hashtable[hash] = bucket;
    return bucket->string;
} // stringcache_len_internal

const char *stringcache_len(StringCache *cache, const char *str,
                            const unsigned int len)
{
    return stringcache_len_internal(cache, str, len, 1);
} // stringcache_len

int stringcache_iscached(StringCache *cache, const char *str)
{
    return (stringcache_len_internal(cache, str, strlen(str), 0) != NULL);
} // stringcache_iscached

const char *stringcache_fmt(StringCache *cache, const char *fmt, ...)
{
    char buf[128];  // use the stack if reasonable.
    char *ptr = NULL;
    int len = 0;  // number of chars, NOT counting null-terminator!
    va_list ap;

    va_start(ap, fmt);
    len = vsnprintf(buf, sizeof (buf), fmt, ap);
    va_end(ap);

    if (len > sizeof (buf))
    {
        ptr = (char *) cache->m(len, cache->d);
        if (ptr == NULL)
            return NULL;

        va_start(ap, fmt);
        vsnprintf(ptr, len, fmt, ap);
        va_end(ap);
    } // if

    const char *retval = stringcache_len(cache, ptr ? ptr : buf, len);
    if (ptr != NULL)
        cache->f(ptr, cache->d);

    return retval;
} // stringcache_fmt

StringCache *stringcache_create(MOJOSHADER_malloc m, MOJOSHADER_free f, void *d)
{
    const uint32 initial_table_size = 256;
    const size_t tablelen = sizeof (StringBucket *) * initial_table_size;
    StringCache *cache = (StringCache *) m(sizeof (StringCache), d);
    if (!cache)
        return NULL;
    memset(cache, '\0', sizeof (StringCache));

    cache->hashtable = (StringBucket **) m(tablelen, d);
    if (!cache->hashtable)
    {
        f(cache, d);
        return NULL;
    } // if
    memset(cache->hashtable, '\0', tablelen);

    cache->table_size = initial_table_size;
    cache->m = m;
    cache->f = f;
    cache->d = d;
    return cache;
} // stringcache_create

void stringcache_destroy(StringCache *cache)
{
    if (cache == NULL)
        return;

    MOJOSHADER_free f = cache->f;
    void *d = cache->d;
    size_t i;

    for (i = 0; i < cache->table_size; i++)
    {
        StringBucket *bucket = cache->hashtable[i];
        cache->hashtable[i] = NULL;
        while (bucket)
        {
            StringBucket *next = bucket->next;
            f(bucket, d);
            bucket = next;
        } // while
    } // for

    f(cache->hashtable, d);
    f(cache, d);
} // stringcache_destroy


// We chain errors as a linked list with a head/tail for easy appending.
//  These get flattened before passing to the application.
typedef struct ErrorItem
{
    MOJOSHADER_error error;
    struct ErrorItem *next;
} ErrorItem;

struct ErrorList
{
    ErrorItem head;
    ErrorItem *tail;
    int count;
    MOJOSHADER_malloc m;
    MOJOSHADER_free f;
    void *d;
};

ErrorList *errorlist_create(MOJOSHADER_malloc m, MOJOSHADER_free f, void *d)
{
    ErrorList *retval = (ErrorList *) m(sizeof (ErrorList), d);
    if (retval != NULL)
    {
        memset(retval, '\0', sizeof (ErrorList));
        retval->tail = &retval->head;
        retval->m = m;
        retval->f = f;
        retval->d = d;
    } // if

    return retval;
} // errorlist_create


int errorlist_add(ErrorList *list, const char *fname,
                  const int errpos, const char *str)
{
    return errorlist_add_fmt(list, fname, errpos, "%s", str);
} // errorlist_add


int errorlist_add_fmt(ErrorList *list, const char *fname,
                      const int errpos, const char *fmt, ...)
{
    va_list ap;
    va_start(ap, fmt);
    const int retval = errorlist_add_va(list, fname, errpos, fmt, ap);
    va_end(ap);
    return retval;
} // errorlist_add_fmt


int errorlist_add_va(ErrorList *list, const char *_fname,
                     const int errpos, const char *fmt, va_list va)
{
    ErrorItem *error = (ErrorItem *) list->m(sizeof (ErrorItem), list->d);
    if (error == NULL)
        return 0;

    char *fname = NULL;
    if (_fname != NULL)
    {
        fname = (char *) list->m(strlen(_fname) + 1, list->d);
        if (fname == NULL)
        {
            list->f(error, list->d);
            return 0;
        } // if
        strcpy(fname, _fname);
    } // if

    char scratch[128];
    va_list ap;
    va_copy(ap, va);
    int len = vsnprintf(scratch, sizeof (scratch), fmt, ap);
    va_end(ap);

    // on some versions of the windows C runtime, vsnprintf() returns -1
    // if the buffer overflows instead of the length the string would have
    // been as expected.
    // In this case we make another copy of va and fetch the length only
    // with another call to _vscprintf

#if defined(_WIN32) && !defined(MOJOSHADER_USE_SDL_STDLIB)
    if (len == -1)
    {
        va_copy(ap, va);
        len = _vscprintf(fmt, ap);
        va_end(ap);
    }
#endif

    char *failstr = (char *) list->m(len + 1, list->d);
    if (failstr == NULL)
    {
        list->f(error, list->d);
        list->f(fname, list->d);
        return 0;
    } // if

    // If we overflowed our scratch buffer, that's okay. We were going to
    //  allocate anyhow...the scratch buffer just lets us avoid a second
    //  run of vsnprintf().
    if (len < sizeof (scratch))
        strcpy(failstr, scratch);  // copy it over.
    else
    {
        va_copy(ap, va);
        vsnprintf(failstr, len + 1, fmt, ap);  // rebuild it.
        va_end(ap);
    } // else

    error->error.error = failstr;
    error->error.filename = fname;
    error->error.error_position = errpos;
    error->next = NULL;

    list->tail->next = error;
    list->tail = error;

    list->count++;
    return 1;
} // errorlist_add_va


int errorlist_count(ErrorList *list)
{
    return list->count;
} // errorlist_count


MOJOSHADER_error *errorlist_flatten(ErrorList *list)
{
    if (list->count == 0)
        return NULL;

    int total = 0;
    MOJOSHADER_error *retval = (MOJOSHADER_error *)
            list->m(sizeof (MOJOSHADER_error) * list->count, list->d);
    if (retval == NULL)
        return NULL;

    ErrorItem *item = list->head.next;
    while (item != NULL)
    {
        ErrorItem *next = item->next;
        // reuse the string allocations
        memcpy(&retval[total], &item->error, sizeof (MOJOSHADER_error));
        list->f(item, list->d);
        item = next;
        total++;
    } // while

    assert(total == list->count);
    list->count = 0;
    list->head.next = NULL;
    list->tail = &list->head;
    return retval;
} // errorlist_flatten


void errorlist_destroy(ErrorList *list)
{
    if (list == NULL)
        return;

    MOJOSHADER_free f = list->f;
    void *d = list->d;
    ErrorItem *item = list->head.next;
    while (item != NULL)
    {
        ErrorItem *next = item->next;
        f((void *) item->error.error, d);
        f((void *) item->error.filename, d);
        f(item, d);
        item = next;
    } // while
    f(list, d);
} // errorlist_destroy


Buffer *buffer_create(size_t blksz, MOJOSHADER_malloc m,
                      MOJOSHADER_free f, void *d)
{
    Buffer *buffer = (Buffer *) m(sizeof (Buffer), d);
    if (buffer != NULL)
    {
        memset(buffer, '\0', sizeof (Buffer));
        buffer->block_size = blksz;
        buffer->m = m;
        buffer->f = f;
        buffer->d = d;
    } // if
    return buffer;
} // buffer_create

char *buffer_reserve(Buffer *buffer, const size_t len)
{
    // note that we make the blocks bigger than blocksize when we have enough
    //  data to overfill a fresh block, to reduce allocations.
    const size_t blocksize = buffer->block_size;

    if (len == 0)
        return NULL;

    if (buffer->tail != NULL)
    {
        const size_t tailbytes = buffer->tail->bytes;
        const size_t avail = (tailbytes >= blocksize) ? 0 : blocksize - tailbytes;
        if (len <= avail)
        {
            buffer->tail->bytes += len;
            buffer->total_bytes += len;
            assert(buffer->tail->bytes <= blocksize);
            return (char *) buffer->tail->data + tailbytes;
        } // if
    } // if

    // need to allocate a new block (even if a previous block wasn't filled,
    //  so this buffer is contiguous).
    const size_t bytecount = len > blocksize ? len : blocksize;
    const size_t malloc_len = sizeof (BufferBlock) + bytecount;
    BufferBlock *item = (BufferBlock *) buffer->m(malloc_len, buffer->d);
    if (item == NULL)
        return NULL;

    item->data = ((uint8 *) item) + sizeof (BufferBlock);
    item->bytes = len;
    item->next = NULL;
    if (buffer->tail != NULL)
        buffer->tail->next = item;
    else
        buffer->head = item;
    buffer->tail = item;

    buffer->total_bytes += len;

    return (char *) item->data;
} // buffer_reserve

int buffer_append(Buffer *buffer, const void *_data, size_t len)
{
    const uint8 *data = (const uint8 *) _data;

    // note that we make the blocks bigger than blocksize when we have enough
    //  data to overfill a fresh block, to reduce allocations.
    const size_t blocksize = buffer->block_size;

    if (len == 0)
        return 1;

    if (buffer->tail != NULL)
    {
        const size_t tailbytes = buffer->tail->bytes;
        const size_t avail = (tailbytes >= blocksize) ? 0 : blocksize - tailbytes;
        const size_t cpy = (avail > len) ? len : avail;
        if (cpy > 0)
        {
            memcpy(buffer->tail->data + tailbytes, data, cpy);
            len -= cpy;
            data += cpy;
            buffer->tail->bytes += cpy;
            buffer->total_bytes += cpy;
            assert(buffer->tail->bytes <= blocksize);
        } // if
    } // if

    if (len > 0)
    {
        assert((!buffer->tail) || (buffer->tail->bytes >= blocksize));
        const size_t bytecount = len > blocksize ? len : blocksize;
        const size_t malloc_len = sizeof (BufferBlock) + bytecount;
        BufferBlock *item = (BufferBlock *) buffer->m(malloc_len, buffer->d);
        if (item == NULL)
            return 0;

        item->data = ((uint8 *) item) + sizeof (BufferBlock);
        item->bytes = len;
        item->next = NULL;
        if (buffer->tail != NULL)
            buffer->tail->next = item;
        else
            buffer->head = item;
        buffer->tail = item;

        memcpy(item->data, data, len);
        buffer->total_bytes += len;
    } // if

    return 1;
} // buffer_append

int buffer_append_fmt(Buffer *buffer, const char *fmt, ...)
{
    va_list ap;
    va_start(ap, fmt);
    const int retval = buffer_append_va(buffer, fmt, ap);
    va_end(ap);
    return retval;
} // buffer_append_fmt

int buffer_append_va(Buffer *buffer, const char *fmt, va_list va)
{
    char scratch[256];

    va_list ap;
    va_copy(ap, va);
    const int len = vsnprintf(scratch, sizeof (scratch), fmt, ap);
    va_end(ap);

    // If we overflowed our scratch buffer, heap allocate and try again.

    if (len == 0)
        return 1;  // nothing to do.
    else if (len < sizeof (scratch))
        return buffer_append(buffer, scratch, len);

    char *buf = (char *) buffer->m(len + 1, buffer->d);
    if (buf == NULL)
        return 0;
    va_copy(ap, va);
    vsnprintf(buf, len + 1, fmt, ap);  // rebuild it.
    va_end(ap);
    const int retval = buffer_append(buffer, buf, len);
    buffer->f(buf, buffer->d);
    return retval;
} // buffer_append_va

size_t buffer_size(Buffer *buffer)
{
    return buffer->total_bytes;
} // buffer_size

void buffer_empty(Buffer *buffer)
{
    BufferBlock *item = buffer->head;
    while (item != NULL)
    {
        BufferBlock *next = item->next;
        buffer->f(item, buffer->d);
        item = next;
    } // while
    buffer->head = buffer->tail = NULL;
    buffer->total_bytes = 0;
} // buffer_empty

char *buffer_flatten(Buffer *buffer)
{
    char *retval = (char *) buffer->m(buffer->total_bytes + 1, buffer->d);
    if (retval == NULL)
        return NULL;
    BufferBlock *item = buffer->head;
    char *ptr = retval;
    while (item != NULL)
    {
        BufferBlock *next = item->next;
        memcpy(ptr, item->data, item->bytes);
        ptr += item->bytes;
        buffer->f(item, buffer->d);
        item = next;
    } // while
    *ptr = '\0';

    assert(ptr == (retval + buffer->total_bytes));

    buffer->head = buffer->tail = NULL;
    buffer->total_bytes = 0;

    return retval;
} // buffer_flatten

char *buffer_merge(Buffer **buffers, const size_t n, size_t *_len)
{
    Buffer *first = NULL;
    size_t len = 0;
    size_t i;
    for (i = 0; i < n; i++)
    {
        Buffer *buffer = buffers[i];
        if (buffer == NULL)
            continue;
        if (first == NULL)
            first = buffer;
        len += buffer->total_bytes;
    } // for

    char *retval = (char *) (first ? first->m(len + 1, first->d) : NULL);
    if (retval == NULL)
    {
        *_len = 0;
        return NULL;
    } // if

    *_len = len;
    char *ptr = retval;
    for (i = 0; i < n; i++)
    {
        Buffer *buffer = buffers[i];
        if (buffer == NULL)
            continue;
        BufferBlock *item = buffer->head;
        while (item != NULL)
        {
            BufferBlock *next = item->next;
            memcpy(ptr, item->data, item->bytes);
            ptr += item->bytes;
            buffer->f(item, buffer->d);
            item = next;
        } // while

        buffer->head = buffer->tail = NULL;
        buffer->total_bytes = 0;
    } // for
    *ptr = '\0';

    assert(ptr == (retval + len));

    return retval;
} // buffer_merge

void buffer_destroy(Buffer *buffer)
{
    if (buffer != NULL)
    {
        MOJOSHADER_free f = buffer->f;
        void *d = buffer->d;
        buffer_empty(buffer);
        f(buffer, d);
    } // if
} // buffer_destroy

void buffer_patch(Buffer *buffer, const size_t start,
                  const void *_data, const size_t len)
{
    if (len == 0)
        return;  // Nothing to do.

    if ((start + len) > buffer->total_bytes)
        return;  // definitely can't patch.

    // Find the start point somewhere in the center of a buffer.
    BufferBlock *item = buffer->head;
    size_t pos = 0;
    if (start > 0)
    {
        while (1)
        {
            assert(item != NULL);
            if ((pos + item->bytes) > start)  // start is in this block.
                break;

            pos += item->bytes;
            item = item->next;
        } // while
    } // if

    const uint8 *data = (const uint8 *) _data;
    size_t write_pos = start - pos;
    size_t write_remain = len;
    size_t written = 0;
    while (write_remain)
    {
        size_t write_end = write_pos + write_remain;
        if (write_end > item->bytes)
            write_end = item->bytes;

        size_t to_write = write_end - write_pos;
        memcpy(item->data + write_pos, data + written, to_write);
        write_remain -= to_write;
        written      += to_write;
        write_pos     = 0;
        item          = item->next;
    } // while
} // buffer_patch

// Based on SDL_string.c's SDL_PrintFloat function
size_t MOJOSHADER_printFloat(char *text, size_t maxlen, float arg)
{
    size_t len;
    size_t left = maxlen;
    char *textstart = text;

    int precision = 9;

    if (isnan(arg))
    {
        if (left > 3)
        {
            snprintf(text, left, "NaN");
            left -= 3;
        } // if
        text += 3;
    } // if
    else if (isinf(arg))
    {
        if (left > 3)
        {
            snprintf(text, left, "inf");
            left -= 3;
        } // if
        text += 3;
    } // else if
    else if (arg)
    {
        /* This isn't especially accurate, but hey, it's easy. :) */
        unsigned long value;

        if (arg < 0)
        {
            if (left > 1)
            {
                *text = '-';
                --left;
            } // if
            ++text;
            arg = -arg;
        } // if
        value = (unsigned long) arg;
        len = snprintf(text, left, "%lu", value);
        text += len;
        if (len >= left)
            left = (left < 1) ? left : 1;
        else
            left -= len;
        arg -= value;

        int mult = 10;
        if (left > 1)
        {
            *text = '.';
            --left;
        } // if
        ++text;
        while (precision-- > 0)
        {
            value = (unsigned long) (arg * mult);
            len = snprintf(text, left, "%lu", value);
            text += len;
            if (len >= left)
                left = (left < 1) ? left : 1;
            else
                left -= len;
            arg -= (double) value / mult;
            if (arg < 0) arg = -arg; // Sometimes that bit gets flipped...
            mult *= 10;
        } // while
    } // if
    else
    {
        if (left > 3)
        {
            snprintf(text, left, "0.0");
            left -= 3;
        } // if
        text += 3;
    } // else

    return (text - textstart);
} // MOJOSHADER_printFloat

#if SUPPORT_PROFILE_SPIRV
#include "spirv/spirv.h"
#include "spirv/GLSL.std.450.h"
void MOJOSHADER_spirv_link_attributes(const MOJOSHADER_parseData *vertex,
                                      const MOJOSHADER_parseData *pixel,
                                      int is_glspirv)
{
    int i;
    uint32 attr_loc = 0;
    uint32 vOffset, pOffset;
    int vDataLen = vertex->output_len - sizeof(SpirvPatchTable);
    int pDataLen = pixel->output_len - sizeof(SpirvPatchTable);
    SpirvPatchTable *vTable = (SpirvPatchTable *) &vertex->output[vDataLen];
    SpirvPatchTable *pTable = (SpirvPatchTable *) &pixel->output[pDataLen];
    const uint32 texcoord0Loc = pTable->attrib_offsets[MOJOSHADER_USAGE_TEXCOORD][0];

    if (is_glspirv)
    {
        // We need locations for color outputs first!
        for (i = 0; i < pixel->output_count; i++)
        {
            const MOJOSHADER_attribute* pAttr = &pixel->outputs[i];
            if (pAttr->usage != MOJOSHADER_USAGE_COLOR)
            {
                // This should be FragDepth, which is builtin
                assert(pAttr->usage == MOJOSHADER_USAGE_DEPTH);
                continue;
            } // if

            // Set the loc for the output declaration...
            pOffset = pTable->output_offsets[pAttr->index];
            assert(pOffset > 0);
            ((uint32*)pixel->output)[pOffset] = attr_loc;

            // Set the same value for the vertex output/pixel input...
            pOffset = pTable->attrib_offsets[pAttr->usage][pAttr->index];
            if (pOffset)
                ((uint32*)pixel->output)[pOffset] = attr_loc;
            vOffset = vTable->attrib_offsets[pAttr->usage][pAttr->index];
            if (vOffset)
                ((uint32*)vertex->output)[vOffset] = attr_loc;

            // ... increment location index, finally.
            attr_loc++;
        } // for
    }

    // Okay, now we can start linking pixel/vertex attributes
    for (i = 0; i < pixel->attribute_count; i++)
    {
        const MOJOSHADER_attribute *pAttr = &pixel->attributes[i];
        if (pAttr->usage == MOJOSHADER_USAGE_UNKNOWN)
            continue; // Probably something like VPOS, ignore!
        if (pAttr->usage == MOJOSHADER_USAGE_DEPTH)
            continue; // This should be FragDepth, which is builtin
        if (is_glspirv && pAttr->usage == MOJOSHADER_USAGE_COLOR && pTable->output_offsets[pAttr->index])
            continue;

        // The input may not exist in the output list!
        pOffset = pTable->attrib_offsets[pAttr->usage][pAttr->index];
        vOffset = vTable->attrib_offsets[pAttr->usage][pAttr->index];
        ((uint32 *) pixel->output)[pOffset] = attr_loc;
        if (vOffset)
            ((uint32 *) vertex->output)[vOffset] = attr_loc;
        attr_loc++;
    } // for

    // There may be outputs not present in the input list!
    for (i = 0; i < vertex->output_count; i++)
    {
        const MOJOSHADER_attribute *vAttr = &vertex->outputs[i];
        assert(vAttr->usage != MOJOSHADER_USAGE_UNKNOWN);
        if (vAttr->usage == MOJOSHADER_USAGE_POSITION && vAttr->index == 0)
            continue;
        if (vAttr->usage == MOJOSHADER_USAGE_POINTSIZE && vAttr->index == 0)
            continue;
        if (is_glspirv && vAttr->usage == MOJOSHADER_USAGE_COLOR && pTable->output_offsets[vAttr->index])
            continue;

        if (!pTable->attrib_offsets[vAttr->usage][vAttr->index])
        {
            vOffset = vTable->attrib_offsets[vAttr->usage][vAttr->index];
            ((uint32 *) vertex->output)[vOffset] = attr_loc++;
        } // if
    } // for

    // gl_PointCoord support
    if (texcoord0Loc)
    {
        if (vTable->attrib_offsets[MOJOSHADER_USAGE_POINTSIZE][0] > 0)
        {
            ((uint32 *) pixel->output)[pTable->pointcoord_var_offset + 1] = pTable->tid_pvec2i;
            ((uint32 *) pixel->output)[pTable->pointcoord_load_offset + 1] = pTable->tid_vec2;
            ((uint32 *) pixel->output)[texcoord0Loc - 1] = SpvDecorationBuiltIn;
            ((uint32 *) pixel->output)[texcoord0Loc] = SpvBuiltInPointCoord;
        } // if
        else
        {
            ((uint32 *) pixel->output)[pTable->pointcoord_var_offset + 1] = pTable->tid_pvec4i;
            ((uint32 *) pixel->output)[pTable->pointcoord_load_offset + 1] = pTable->tid_vec4;
            ((uint32 *) pixel->output)[texcoord0Loc - 1] = SpvDecorationLocation;
            // texcoord0Loc should already have attr_loc from the above work!
        } // else
    } // if
} // MOJOSHADER_spirv_link_attributes
#endif

// end of mojoshader_common.c ...

