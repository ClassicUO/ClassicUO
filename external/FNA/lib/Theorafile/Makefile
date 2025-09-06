# Makefile for Theorafile
# Written by Ethan "flibitijibibo" Lee

# Detect cross targets
TRIPLET=$(shell $(CC) -dumpmachine)
WINDOWS_TARGET=0
APPLE_TARGET=0
ifeq ($(OS), Windows_NT) # cygwin/msys2
	WINDOWS_TARGET=1
endif
ifneq (,$(findstring w64-mingw32,$(TRIPLET)))
	WINDOWS_TARGET=1
endif
ifneq (,$(findstring w64-windows,$(TRIPLET)))
	WINDOWS_TARGET=1
endif
ifneq (,$(findstring apple-darwin,$(TRIPLET)))
	APPLE_TARGET=1
endif
ifneq (,$(findstring x86_64,$(TRIPLET)))
	DEFINES += -DOC_X86_ASM -DOC_X86_64_ASM
endif
ifneq (,$(findstring i686,$(TRIPLET)))
	DEFINES += -DOC_X86_ASM
endif

# Compiler
ifeq ($(WINDOWS_TARGET),1)
	TARGET = dll
	LDFLAGS += -static-libgcc
else ifeq ($(APPLE_TARGET),1)
	CC += -mmacosx-version-min=10.9
	TARGET = dylib
	CFLAGS += -fpic -fPIC
	LDFLAGS += -install_name @rpath/libtheorafile.dylib
else
	TARGET = so
	CFLAGS += -fpic -fPIC
endif

CFLAGS += -O3

SRCDIR = $(dir $(MAKEFILE_LIST))

vpath %.c $(SRCDIR)

# Includes
INCLUDES = -I$(SRCDIR) -I$(SRCDIR)/lib -I$(SRCDIR)/lib/ogg -I$(SRCDIR)/lib/vorbis -I$(SRCDIR)/lib/theora

# Source
TFSRC = \
	theorafile.c \
	lib/ogg/bitwise.c \
	lib/ogg/framing.c \
	lib/vorbis/analysis.c \
	lib/vorbis/bitrate.c \
	lib/vorbis/block.c \
	lib/vorbis/codebook.c \
	lib/vorbis/envelope.c \
	lib/vorbis/floor0.c \
	lib/vorbis/floor1.c \
	lib/vorbis/vinfo.c \
	lib/vorbis/lookup.c \
	lib/vorbis/lpc.c \
	lib/vorbis/lsp.c \
	lib/vorbis/mapping0.c \
	lib/vorbis/mdct.c \
	lib/vorbis/psy.c \
	lib/vorbis/registry.c \
	lib/vorbis/res0.c \
	lib/vorbis/sharedbook.c \
	lib/vorbis/smallft.c \
	lib/vorbis/synthesis.c \
	lib/vorbis/window.c \
	lib/theora/apiwrapper.c \
	lib/theora/bitpack.c \
	lib/theora/decapiwrapper.c \
	lib/theora/decinfo.c \
	lib/theora/decode.c \
	lib/theora/dequant.c \
	lib/theora/fragment.c \
	lib/theora/huffdec.c \
	lib/theora/idct.c \
	lib/theora/tinfo.c \
	lib/theora/internal.c \
	lib/theora/quant.c \
	lib/theora/state.c \
	lib/theora/x86/mmxfrag.c \
	lib/theora/x86/mmxidct.c \
	lib/theora/x86/mmxstate.c \
	lib/theora/x86/x86state.c

# Targets
all: $(TFSRC)
	$(CC) $(CFLAGS) -shared -o libtheorafile.$(TARGET) $^ $(INCLUDES) $(DEFINES) -lm $(LDFLAGS)

clean:
	rm -f libtheorafile.$(TARGET)

test:
	$(CC) -g -o theorafile-test sdl2test/sdl2test.c $(TFSRC) $(INCLUDES) $(DEFINES) `sdl2-config --cflags --libs` -lm
