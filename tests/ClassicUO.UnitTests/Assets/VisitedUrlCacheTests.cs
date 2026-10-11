using ClassicUO.Assets;
using FluentAssertions;
using Xunit;

namespace ClassicUO.UnitTests.Assets
{
    public class VisitedUrlCacheTests
    {
        [Fact]
        public void When_Empty_IsVisited_Returns_False()
        {
            var cache = new VisitedUrlCache(8);

            cache.IsVisited("https://example.com").Should().BeFalse();
            cache.Count.Should().Be(0);
        }

        [Fact]
        public void When_Marked_IsVisited_Returns_True()
        {
            var cache = new VisitedUrlCache(8);

            cache.Mark("https://example.com");

            cache.IsVisited("https://example.com").Should().BeTrue();
            cache.Count.Should().Be(1);
        }

        [Fact]
        public void When_Marked_Twice_Same_Url_No_Duplicate_Stored()
        {
            var cache = new VisitedUrlCache(8);

            cache.Mark("https://example.com");
            cache.Mark("https://example.com");

            cache.Count.Should().Be(1);
            cache.IsVisited("https://example.com").Should().BeTrue();
        }

        [Fact]
        public void When_Different_Urls_Marked_Both_Visible()
        {
            var cache = new VisitedUrlCache(8);

            cache.Mark("https://a.com");
            cache.Mark("https://b.com");

            cache.IsVisited("https://a.com").Should().BeTrue();
            cache.IsVisited("https://b.com").Should().BeTrue();
            cache.Count.Should().Be(2);
        }

        [Fact]
        public void When_Capacity_Exceeded_Oldest_Evicted()
        {
            // Capacity 3, mark 4 distinct URLs in order. The first one (oldest)
            // should be evicted; the rest remain.
            var cache = new VisitedUrlCache(3);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c");
            cache.Mark("d");

            cache.Count.Should().Be(3);
            cache.IsVisited("a").Should().BeFalse();
            cache.IsVisited("b").Should().BeTrue();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_Marking_Existing_Url_Bumps_To_Front()
        {
            // After Mark a/b/c, "a" is the LRU. Re-marking "a" promotes it; the
            // next eviction takes "b" instead.
            var cache = new VisitedUrlCache(3);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c");
            cache.Mark("a"); // re-mark — should bump "a" to MRU
            cache.Mark("d"); // evicts the new LRU, which is "b"

            cache.IsVisited("a").Should().BeTrue();
            cache.IsVisited("b").Should().BeFalse();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_IsVisited_Hits_Touches_Lru()
        {
            // IsVisited must promote the matched entry — otherwise frequently
            // displayed visited URLs that are never re-clicked would silently
            // age out and revert to the unvisited color.
            var cache = new VisitedUrlCache(3);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c");

            // Touch "a" via IsVisited; this should make it MRU.
            cache.IsVisited("a").Should().BeTrue();

            // Adding a 4th URL evicts the LRU, which should now be "b".
            cache.Mark("d");

            cache.IsVisited("a").Should().BeTrue();
            cache.IsVisited("b").Should().BeFalse();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_IsVisited_Misses_No_State_Mutation()
        {
            var cache = new VisitedUrlCache(3);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c");

            cache.IsVisited("nonexistent").Should().BeFalse();
            cache.Count.Should().Be(3);

            // The miss must not have promoted anything or evicted anything;
            // adding a 4th URL still evicts "a" (the original LRU).
            cache.Mark("d");

            cache.IsVisited("a").Should().BeFalse();
            cache.IsVisited("b").Should().BeTrue();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_Capacity_One_Single_Slot_Replaced()
        {
            var cache = new VisitedUrlCache(1);

            cache.Mark("a");
            cache.IsVisited("a").Should().BeTrue();

            cache.Mark("b");
            cache.Count.Should().Be(1);
            cache.IsVisited("a").Should().BeFalse();
            cache.IsVisited("b").Should().BeTrue();
        }

        [Fact]
        public void When_Many_Marks_Cap_Strictly_Respected()
        {
            const int capacity = 16;
            var cache = new VisitedUrlCache(capacity);

            for (int i = 0; i < 1000; i++)
            {
                cache.Mark("url-" + i);
                cache.Count.Should().BeLessThanOrEqualTo(capacity);
            }

            cache.Count.Should().Be(capacity);

            // The last `capacity` URLs should be the survivors.
            for (int i = 0; i < 1000 - capacity; i++)
            {
                cache.IsVisited("url-" + i).Should().BeFalse();
            }

            for (int i = 1000 - capacity; i < 1000; i++)
            {
                cache.IsVisited("url-" + i).Should().BeTrue();
            }
        }

        [Fact]
        public void When_Refilled_After_Eviction_Order_Preserved()
        {
            // Evict, refill — the LRU pointer must still be sane.
            var cache = new VisitedUrlCache(2);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c"); // evicts "a"; head=c, tail=b
            cache.Mark("d"); // evicts "b"; head=d, tail=c

            cache.IsVisited("a").Should().BeFalse();
            cache.IsVisited("b").Should().BeFalse();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_Marking_Tail_Then_Adding_New_Tail_Updated()
        {
            // Regression guard for the tail-pointer rewiring path: marking the
            // current tail must move it to head so that the *next* eviction
            // takes the new tail (formerly second-to-tail), not the URL we
            // just promoted.
            var cache = new VisitedUrlCache(3);

            cache.Mark("a"); // head=a, tail=a
            cache.Mark("b"); // head=b, tail=a
            cache.Mark("c"); // head=c, tail=a
            cache.Mark("a"); // promote tail; head=a, tail=b

            cache.Mark("d"); // evicts the new tail "b"

            cache.IsVisited("a").Should().BeTrue();
            cache.IsVisited("b").Should().BeFalse();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }

        [Fact]
        public void When_Marking_Head_Mark_Is_Idempotent()
        {
            // Re-marking the current head must not corrupt links — head stays
            // head, tail stays tail.
            var cache = new VisitedUrlCache(3);

            cache.Mark("a");
            cache.Mark("b");
            cache.Mark("c"); // head=c, tail=a

            cache.Mark("c"); // head=c, tail=a (unchanged)

            cache.Count.Should().Be(3);

            cache.Mark("d"); // should evict tail "a"

            cache.IsVisited("a").Should().BeFalse();
            cache.IsVisited("b").Should().BeTrue();
            cache.IsVisited("c").Should().BeTrue();
            cache.IsVisited("d").Should().BeTrue();
        }
    }
}
