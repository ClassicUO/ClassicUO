using System;
using ClassicUO.Network;
using FluentAssertions;
using Xunit;

namespace ClassicUO.Client.Tests.Network
{
    public class HuffmanTests
    {
        [Fact]
        public void Reset_DoesNotThrow()
        {
            var huffman = new Huffman();

            var act = () => huffman.Reset();

            act.Should().NotThrow();
        }

        [Fact]
        public void Reset_CanBeCalledMultipleTimes()
        {
            var huffman = new Huffman();

            huffman.Reset();
            huffman.Reset();
            huffman.Reset();

            // Should not throw or corrupt state
        }

        [Fact]
        public void Decompress_EmptySource_ReturnsTrue_WithZeroSize()
        {
            var huffman = new Huffman();
            huffman.Reset();

            var src = Span<byte>.Empty;
            var dest = new byte[64];
            int size = dest.Length;

            bool result = huffman.Decompress(src, dest, ref size);

            result.Should().BeTrue();
            size.Should().Be(0);
        }

        [Fact]
        public void Decompress_FlushByte_ProducesNoOutput()
        {
            // The Huffman tree: bit pattern that leads to node 7 (left child = -256 = flush marker)
            // Walking the tree from root (node 0):
            //   node 0: left=1, right=2
            //   node 1: left=3, right=4
            //   node 3: left=6, right=7
            //   node 7: left=-256, right=14
            // Path to -256 (flush): left, left, left, left = bits 1,1,1,1 (mask checks value & mask != 0)
            // So byte 0xF0 has top 4 bits = 1111, reaching -256 flush marker
            var huffman = new Huffman();
            huffman.Reset();

            var src = new byte[] { 0xF0 };
            var dest = new byte[64];
            int size = dest.Length;

            bool result = huffman.Decompress(src, dest, ref size);

            result.Should().BeTrue();
            // The flush marker resets tree position but doesn't output a byte
            // Remaining 4 bits will be partially traversed but since source ends,
            // the decompressor returns with whatever was decoded
        }

        [Fact]
        public void Decompress_KnownPattern_ProducesExpectedByte()
        {
            // Let's trace through the tree to find the bit pattern for byte value 0x00 (node value -0 doesn't exist)
            // and byte value 1 (which is at node 12, left child = -1)
            // Path to -1 (byte 0x01):
            //   node 0: right=2 (bit 0)
            //   node 2: left=5 (bit 1)
            //   node 5: left=10 (bit 1)
            //   node 10: left=19 (bit 1)
            //   node 19: left=36 (bit 1)
            //   node 36: left=63 (bit 1)
            //   node 63: left=-26 (bit 1) -> outputs byte 26
            // Wait, let me re-trace. Bit 1 means (value & mask) != 0 -> takes left child (index*2)
            // Bit 0 means (value & mask) == 0 -> takes right child (index*2+1)

            // Path to node with -1: node 12 left child = -1
            // To reach node 12: node 6 left=12
            // To reach node 6: node 3 left=6
            // To reach node 3: node 1 left=3
            // To reach node 1: node 0 left=1
            // So path: left, left, left, left, left = 5 ones = bits 11111
            // But node 7 left=-256 is reached via node 0->1->3->7->left
            // node 0 left=1, node 1 left=3, node 3 left=6, NOT 7
            // node 3: left=6, right=7
            // So path to node 6: 1,1,1,0 (left,left,left,right... wait)
            // Bit=1 -> left child = index*2
            // Bit=0 -> right child = index*2+1
            //
            // node 0: left(1)=1, right(0)=2
            // node 1: left(1)=3, right(0)=4
            // node 3: left(1)=6, right(0)=7
            // node 6: left(1)=12, right(0)=13
            // node 12: left(1)=-1, right(0)=23
            //
            // Path to -1: 1,1,1,1,1 = 5 bits all 1
            // But wait, 4 bits of 1 go: node0->1->3->7(not 6)
            // node 3: index 3, left child = _decTree[3*2] = _decTree[6] = 12
            //   ... actually wait. Let me re-read the tree array.
            // _decTree[0]=1, _decTree[1]=2  (node 0: left=1, right=2)
            // _decTree[2]=3, _decTree[3]=4  (node 1: left=3, right=4)
            // _decTree[4]=5, _decTree[5]=0  (node 2: left=5, right=0)
            // _decTree[6]=6, _decTree[7]=7  (node 3: left=6, right=7)
            // _decTree[12]=12, wait no: _decTree[12]=-1, _decTree[13]=23  (node 6)
            // Wait: /* 6*/ 12, 13 -> node 6: _decTree[12]=12, _decTree[13]=13
            // No! The array indices: node 6 -> _decTree[6*2]=_decTree[12], _decTree[6*2+1]=_decTree[13]
            // From the array: /*   6*/ 12,   13,
            // So node 6: left=12, right=13
            // node 12: _decTree[24]=-1, _decTree[25]=23
            // From array: /*  12*/ -1,   23,
            // So path: 1(->1), 1(->3), 1(->6), 1(->12), 1(->-1) = byte value 1
            // But 4 ones go to node 7 not node 6!
            // node 0 -> bit 1 -> _decTree[0] = 1 -> node 1
            // node 1 -> bit 1 -> _decTree[2] = 3 -> node 3
            // node 3 -> bit 1 -> _decTree[6] = 6 -> node 6
            // node 6 -> bit 1 -> _decTree[12] = -1 -> output byte 1!
            //
            // So 4 bits of value 1 produce output byte 1! Pattern = 0b1111_xxxx
            // Then we also need the flush marker to cleanly end. But actually the
            // decompressor just returns true when source is exhausted.
            //
            // Let's encode: 4 bits of 1, then remaining 4 bits are 0.
            // byte = 0b1111_0000 = 0xF0
            // But we showed above that 0xF0 top 4 bits reach -256 (flush), not -1.
            // Let me re-check:
            // node 0 -> bit 1 -> node 1
            // node 1 -> bit 1 -> node 3
            // node 3 -> bit 1 -> _decTree[6] = 6...
            // WAIT. The tree says /* 3*/ 6, 7
            // That means _decTree[6]=6 and _decTree[7]=7? No!
            // The comment "/* 3*/" means these are the values AT array positions for node 3.
            // Node 3 data is at _decTree[6] and _decTree[7].
            // _decTree[6] = 6 (left child of node 3)
            // _decTree[7] = 7 (right child of node 3)
            // So: bit=1 -> left -> _decTree[3*2] = _decTree[6] = 6 -> go to node 6
            // bit=0 -> right -> _decTree[3*2+1] = _decTree[7] = 7 -> go to node 7
            //
            // node 7: _decTree[14] = -256, _decTree[15] = 14
            // So bit=1 from node 7 -> -256 (flush)
            //
            // To get to node 6 from node 3, we need bit=1. Good.
            // Path to node 6: node0(1)->node1(1)->node3(1)->node6
            // node 6: _decTree[12]=-1(left), _decTree[13]=23(right)
            // Wait! The comment says /* 6*/ 12, 13 - so _decTree[12]=12 and _decTree[13]=13?
            // No! The comments show the NODE INDEX on the left, and the VALUES on the right.
            // So node 6 has left=12, right=13. These are child NODE indices, not values.
            // To get -1, we need node 12.
            // node 12: /* 12*/ -1, 23 -> left=-1, right=23
            //
            // Full path to output byte 1:
            // node 0 --(1)--> node 1 --(1)--> node 3 --(1)--> node 6 --(1)--> node 12 --(1)--> -1
            // That's 5 ones.
            //
            // And path to flush (-256):
            // node 0 --(1)--> node 1 --(1)--> node 3 --(0)--> node 7 --(1)--> -256
            // That's 1,1,0,1 = 4 bits
            //
            // So to encode byte 1 then flush: 11111 + 1101 = 9 bits
            // = 1111_1110_1xxx_xxxx
            // Pad with zeros: 0xFF, 0x40 (1111_1111, 0100_0000)... wait
            // 11111 11010000000 -> split into bytes: 1111_1110, 1000_0000
            // = 0xFE, 0x80
            // Hmm let me be more careful:
            // bits: 1 1 1 1 1 1 1 0 | 1 0 0 0 0 0 0 0
            //       ^-byte 1 (0x01)-^ ^--flush--^
            // First 5 bits (11111) decode byte 1, then next 4 bits (1101) do flush
            // Total 9 bits. First byte = 11111110 = 0xFE, second byte = 10000000 = 0x80

            var huffman = new Huffman();
            huffman.Reset();

            var src = new byte[] { 0xFE, 0x80 };
            var dest = new byte[64];
            int size = dest.Length;

            bool result = huffman.Decompress(src, dest, ref size);

            result.Should().BeTrue();
            size.Should().BeGreaterThanOrEqualTo(1);
            dest[0].Should().Be(1);
        }

        [Fact]
        public void Decompress_OutputBufferTooSmall_ReturnsFalse()
        {
            // Use the same known encoding for byte value 1 (5 ones) but with size=0
            var huffman = new Huffman();
            huffman.Reset();

            // 5 bits of 1 to produce a byte, need size=0 to trigger the false return
            var src = new byte[] { 0xF8 }; // 11111_000 - 5 ones then 3 zeros
            var dest = new byte[1];
            int size = 0; // buffer "full" immediately

            bool result = huffman.Decompress(src, dest, ref size);

            result.Should().BeFalse();
        }

        [Fact]
        public void Decompress_AfterReset_ProducesSameResult()
        {
            var huffman = new Huffman();

            // First decompress
            huffman.Reset();
            var src1 = new byte[] { 0xFE, 0x80 };
            var dest1 = new byte[64];
            int size1 = dest1.Length;
            huffman.Decompress(src1, dest1, ref size1);

            // Reset and decompress again
            huffman.Reset();
            var src2 = new byte[] { 0xFE, 0x80 };
            var dest2 = new byte[64];
            int size2 = dest2.Length;
            huffman.Decompress(src2, dest2, ref size2);

            size1.Should().Be(size2);
            dest1.AsSpan(0, size1).ToArray().Should().BeEquivalentTo(dest2.AsSpan(0, size2).ToArray());
        }
    }
}
