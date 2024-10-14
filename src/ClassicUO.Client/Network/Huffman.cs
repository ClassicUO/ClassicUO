﻿#region license

// Copyright (c) 2024, andreakarasho
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 1. Redistributions of source code must retain the above copyright
//    notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
//    notice, this list of conditions and the following disclaimer in the
//    documentation and/or other materials provided with the distribution.
// 3. All advertising materials mentioning features or use of this software
//    must display the following acknowledgement:
//    This product includes software developed by andreakarasho - https://github.com/andreakarasho
// 4. Neither the name of the copyright holder nor the
//    names of its contributors may be used to endorse or promote products
//    derived from this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS ''AS IS'' AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
// WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE FOR ANY
// DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
// (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
// LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
// ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

#endregion

using System;

namespace ClassicUO.Network
{
    sealed class Huffman
    {
        private static readonly int[] _decTree = new int[]
            {
    /*   0*/ 1,    2,
    /*   1*/ 3,    4,
    /*   2*/ 5,    0,
    /*   3*/ 6,    7,
    /*   4*/ 8,    9,
    /*   5*/ 10,   11,
    /*   6*/ 12,   13,
    /*   7*/ -256, 14,
    /*   8*/ 15,   16,
    /*   9*/ 17,   18,
    /*  10*/ 19,   20,
    /*  11*/ 21,   22,
    /*  12*/ -1,   23,
    /*  13*/ 24,   25,
    /*  14*/ 26,   27,
    /*  15*/ 28,   29,
    /*  16*/ 30,   31,
    /*  17*/ 32,   33,
    /*  18*/ 34,   35,
    /*  19*/ 36,   37,
    /*  20*/ 38,   39,
    /*  21*/ 40,   -64,
    /*  22*/ 41,   42,
    /*  23*/ 43,   44,
    /*  24*/ -6,   45,
    /*  25*/ 46,   47,
    /*  26*/ 48,   49,
    /*  27*/ 50,   51,
    /*  28*/ -119, 52,
    /*  29*/ -32,  53,
    /*  30*/ 54,   -14,
    /*  31*/ 55,   -5,
    /*  32*/ 56,   57,
    /*  33*/ 58,   59,
    /*  34*/ 60,   -2,
    /*  35*/ 61,   62,
    /*  36*/ 63,   64,
    /*  37*/ 65,   66,
    /*  38*/ 67,   68,
    /*  39*/ 69,   70,
    /*  40*/ 71,   72,
    /*  41*/ -51,  73,
    /*  42*/ 74,   75,
    /*  43*/ 76,   77,
    /*  44*/ -101, -111,
    /*  45*/ -4,   -97,
    /*  46*/ 78,   79,
    /*  47*/ -110, 80,
    /*  48*/ 81,   -116,
    /*  49*/ 82,   83,
    /*  50*/ 84,   -255,
    /*  51*/ 85,   86,
    /*  52*/ 87,   88,
    /*  53*/ 89,   90,
    /*  54*/ -15,  -10,
    /*  55*/ 91,   92,
    /*  56*/ -21,  93,
    /*  57*/ -117, 94,
    /*  58*/ 95,   96,
    /*  59*/ 97,   98,
    /*  60*/ 99,   100,
    /*  61*/ -114, 101,
    /*  62*/ -105, 102,
    /*  63*/ -26,  103,
    /*  64*/ 104,  105,
    /*  65*/ 106,  107,
    /*  66*/ 108,  109,
    /*  67*/ 110,  111,
    /*  68*/ 112,  -3,
    /*  69*/ 113,  -7,
    /*  70*/ 114,  -131,
    /*  71*/ 115,  -144,
    /*  72*/ 116,  117,
    /*  73*/ -20,  118,
    /*  74*/ 119,  120,
    /*  75*/ 121,  122,
    /*  76*/ 123,  124,
    /*  77*/ 125,  126,
    /*  78*/ 127,  128,
    /*  79*/ 129,  -100,
    /*  80*/ 130,  -8,
    /*  81*/ 131,  132,
    /*  82*/ 133,  134,
    /*  83*/ -120, 135,
    /*  84*/ 136,  -31,
    /*  85*/ 137,  138,
    /*  86*/ -109, -234,
    /*  87*/ 139,  140,
    /*  88*/ 141,  142,
    /*  89*/ 143,  144,
    /*  90*/ -112, 145,
    /*  91*/ -19,  146,
    /*  92*/ 147,  148,
    /*  93*/ 149,  -66,
    /*  94*/ 150,  -145,
    /*  95*/ -13,  -65,
    /*  96*/ 151,  152,
    /*  97*/ 153,  154,
    /*  98*/ -30,  155,
    /*  99*/ 156,  157,
    /* 100*/ -99,  158,
    /* 101*/ 159,  160,
    /* 102*/ 161,  162,
    /* 103*/ -23,  163,
    /* 104*/ -29,  164,
    /* 105*/ -11,  165,
    /* 106*/ 166,  -115,
    /* 107*/ 167,  168,
    /* 108*/ 169,  170,
    /* 109*/ -16,  171,
    /* 110*/ -34,  172,
    /* 111*/ 173,  -132,
    /* 112*/ 174,  -108,
    /* 113*/ 175,  -22,
    /* 114*/ 176,  -9,
    /* 115*/ 177,  -84,
    /* 116*/ -17,  -37,
    /* 117*/ -28,  178,
    /* 118*/ 179,  180,
    /* 119*/ 181,  182,
    /* 120*/ 183,  184,
    /* 121*/ 185,  186,
    /* 122*/ 187,  -104,
    /* 123*/ 188,  -78,
    /* 124*/ 189,  -61,
    /* 125*/ -79,  -178,
    /* 126*/ -59,  -134,
    /* 127*/ 190,  -25,
    /* 128*/ -83,  -18,
    /* 129*/ 191,  -57,
    /* 130*/ -67,  192,
    /* 131*/ -98,  193,
    /* 132*/ -12,  -68,
    /* 133*/ 194,  195,
    /* 134*/ -55,  -128,
    /* 135*/ -24,  -50,
    /* 136*/ -70,  196,
    /* 137*/ -94,  -33,
    /* 138*/ 197,  -129,
    /* 139*/ -74,  198,
    /* 140*/ -82,  199,
    /* 141*/ -56,  -87,
    /* 142*/ -44,  200,
    /* 143*/ -248, 201,
    /* 144*/ -163, -81,
    /* 145*/ -52,  -123,
    /* 146*/ 202,  -113,
    /* 147*/ -48,  -41,
    /* 148*/ -122, -40,
    /* 149*/ 203,  -90,
    /* 150*/ -54,  204,
    /* 151*/ -86,  -192,
    /* 152*/ 205,  206,
    /* 153*/ 207,  -130,
    /* 154*/ -53,  208,
    /* 155*/ -133, -45,
    /* 156*/ 209,  210,
    /* 157*/ 211,  -91,
    /* 158*/ 212,  213,
    /* 159*/ -106, -88,
    /* 160*/ 214,  215,
    /* 161*/ 216,  217,
    /* 162*/ 218,  -49,
    /* 163*/ 219,  220,
    /* 164*/ 221,  222,
    /* 165*/ 223,  224,
    /* 166*/ 225,  226,
    /* 167*/ 227,  -102,
    /* 168*/ -160, 228,
    /* 169*/ -46,  229,
    /* 170*/ -127, 230,
    /* 171*/ -103, 231,
    /* 172*/ 232,  233,
    /* 173*/ -60,  234,
    /* 174*/ 235,  -76,
    /* 175*/ 236,  -121,
    /* 176*/ 237,  -73,
    /* 177*/ -149, 238,
    /* 178*/ 239,  -107,
    /* 179*/ -35,  240,
    /* 180*/ -71,  -27,
    /* 181*/ -69,  241,
    /* 182*/ -89,  -77,
    /* 183*/ -62,  -118,
    /* 184*/ -75,  -85,
    /* 185*/ -72,  -58,
    /* 186*/ -63,  -80,
    /* 187*/ 242,  -42,
    /* 188*/ -150, -157,
    /* 189*/ -139, -236,
    /* 190*/ -126, -243,
    /* 191*/ -142, -214,
    /* 192*/ -138, -206,
    /* 193*/ -240, -146,
    /* 194*/ -204, -147,
    /* 195*/ -152, -201,
    /* 196*/ -227, -207,
    /* 197*/ -154, -209,
    /* 198*/ -153, -254,
    /* 199*/ -176, -156,
    /* 200*/ -165, -210,
    /* 201*/ -172, -185,
    /* 202*/ -195, -170,
    /* 203*/ -232, -211,
    /* 204*/ -219, -239,
    /* 205*/ -200, -177,
    /* 206*/ -175, -212,
    /* 207*/ -244, -143,
    /* 208*/ -246, -171,
    /* 209*/ -203, -221,
    /* 210*/ -202, -181,
    /* 211*/ -173, -250,
    /* 212*/ -184, -164,
    /* 213*/ -193, -218,
    /* 214*/ -199, -220,
    /* 215*/ -190, -249,
    /* 216*/ -230, -217,
    /* 217*/ -169, -216,
    /* 218*/ -191, -197,
    /* 219*/ -47,  243,
    /* 220*/ 244,  245,
    /* 221*/ 246,  247,
    /* 222*/ -148, -159,
    /* 223*/ 248,  249,
    /* 224*/ -92,  -93,
    /* 225*/ -96,  -225,
    /* 226*/ -151, -95,
    /* 227*/ 250,  251,
    /* 228*/ -241, 252,
    /* 229*/ -161, -36,
    /* 230*/ 253,  254,
    /* 231*/ -135, -39,
    /* 232*/ -187, -124,
    /* 233*/ 255,  -251,
    /* 234*/ -162, -238,
    /* 235*/ -242, -38,
    /* 236*/ -43,  -125,
    /* 237*/ -215, -253,
    /* 238*/ -140, -208,
    /* 239*/ -137, -235,
    /* 240*/ -158, -237,
    /* 241*/ -136, -205,
    /* 242*/ -155, -141,
    /* 243*/ -228, -229,
    /* 244*/ -213, -168,
    /* 245*/ -224, -194,
    /* 246*/ -196, -226,
    /* 247*/ -183, -233,
    /* 248*/ -231, -167,
    /* 249*/ -174, -189,
    /* 250*/ -252, -166,
    /* 251*/ -198, -222,
    /* 252*/ -188, -179,
    /* 253*/ -223, -182,
    /* 254*/ -180, -186,
    /* 255*/ -245, -247,
};

        private int _bitNum = 8;
        private int _value, _mask, _treePos;

        public void Reset()
        {
            _bitNum = 8;
            _value = 0;
            _mask = 0;
            _treePos = 0;
        }

        public bool Decompress(Span<byte> src, Span<byte> dest, ref int size)
        {
            var destIndex = 0;
            dest.Clear();

            while (true)
            {
                if (_bitNum >= 8)
                {
                    if (src.IsEmpty)
                    {
                        size = destIndex;

                        return true;
                    }

                    _value = src[0];
                    src = src.Slice(1);

                    _bitNum = 0;
                    _mask = 0x80;
                }

                if ((_value & _mask) != 0)
                {
                    _treePos = _decTree[_treePos * 2];
                }
                else
                {
                    _treePos = _decTree[_treePos * 2 + 1];
                }

                _mask >>= 1;
                _bitNum++;

                if (_treePos <= 0)
                {
                    if (_treePos == -256)
                    {
                        _bitNum = 8;
                        _treePos = 0;
                        continue;
                    }

                    if (destIndex == size)
                    {
                        return false;
                    }

                    dest[destIndex++] = (byte)-_treePos;
                    _treePos = 0;
                }
            }
        }
    }
}