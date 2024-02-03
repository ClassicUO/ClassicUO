#region license

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

namespace ClassicUO.Game
{
    internal static class Constants
    {
        public const string WIKI_LINK = "https://github.com/ClassicUO/ClassicUO/wiki";

        public const int MIN_FPS = 12;
        public const int MAX_FPS = 250;
        public const int LOGIN_SCREEN_FPS = 60;

        public const int CHARACTER_ANIMATION_DELAY = 80;
        public const int ITEM_EFFECT_ANIMATION_DELAY = 50;

        public const int MAX_STEP_COUNT = 5;
        public const int TURN_DELAY = 100; // original client 12.5 fps = 80ms delay. Edit --> it causes throttling
        public const int TURN_DELAY_FAST = 45;
        public const int WALKING_DELAY = 150; // 750
        public const int PLAYER_WALKING_DELAY = 150;
        public const int DEFAULT_CHARACTER_HEIGHT = 16;
        public const int DEFAULT_BLOCK_HEIGHT = 16;

        public const float TIME_FADEOUT_TEXT = 1000;
        public const uint TIME_DISPLAY_SYSTEM_MESSAGE_TEXT = 10000;

        public const int MIN_TERRAIN_SHADOWS_LEVEL = 5;
        public const int MAX_TERRAIN_SHADOWS_LEVEL = 25;

        public const int USED_LAYER_COUNT = 23;

        public const int CLEAR_TEXTURES_DELAY = 3000;
        public const int MAX_ANIMATIONS_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_SOUND_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 50;

        public const int MAX_FAST_WALK_STACK_SIZE = 5;

        public const byte FOLIAGE_ALPHA = 76;
        public const byte ALPHA_TIME = 20;

        public const int MAX_OBJECT_HANDLES = 200;
        public const int OBJECT_HANDLES_GUMP_WIDTH = 100;
        public const int OBJECT_HANDLES_GUMP_HEIGHT = 18;

        public const int SPELLBOOK_1_SPELLS_COUNT = 64;
        public const int SPELLBOOK_2_SPELLS_COUNT = 17;
        public const int SPELLBOOK_3_SPELLS_COUNT = 10;
        public const int SPELLBOOK_4_SPELLS_COUNT = 6;
        public const int SPELLBOOK_5_SPELLS_COUNT = 8;
        public const int SPELLBOOK_6_SPELLS_COUNT = 16;
        public const int SPELLBOOK_7_SPELLS_COUNT = 30;
        public const int SPELLBOOK_8_SPELLS_COUNT = 45;

        public const int WAIT_FOR_TARGET_DELAY = 5000;

        public const int CONTAINER_RECT_STEP = 20;
        public const int CONTAINER_RECT_DEFAULT_POSITION = 40;
        public const int CONTAINER_RECT_LINESTEP = 800;
        public const int ITEM_GUMP_TEXTURE_OFFSET = 11369;

        public const int MAX_MUSIC_DATA_INDEX_COUNT = 150;


        public const ushort FIELD_REPLACE_GRAPHIC = 0x1826;
        public const ushort TREE_REPLACE_GRAPHIC = 0x0E59;

        public const int MIN_CIRCLE_OF_TRANSPARENCY_RADIUS = 50;
        public const int MAX_CIRCLE_OF_TRANSPARENCY_RADIUS = 200;

        public const int MAX_ABILITIES_COUNT = 32;

        public const int DRAG_ITEMS_DISTANCE = 3;
        public const int MIN_GUMP_DRAG_DISTANCE = 0;
        public const int MIN_PICKUP_DRAG_DISTANCE_PIXELS = 5;

        public const int MIN_VIEW_RANGE = 5;
        public const int MAX_VIEW_RANGE = 24;
        public const int MAX_CONTAINER_OPENED_ON_GROUND_RANGE = 3;

        public const int OUT_RANGE_COLOR = 0x038B;
        public const int DEAD_RANGE_COLOR = 0x038E;
        public const int DEATH_SCREEN_TIMER = 1500;

        public const ushort HIGHLIGHT_CURRENT_OBJECT_HUE = 0x014;

        public const int MAX_JOURNAL_HISTORY_COUNT = 100;

        public const byte MIN_CONTAINER_SIZE_PERC = 50;
        public const byte MAX_CONTAINER_SIZE_PERC = 200;

        public const int MALE_GUMP_OFFSET = 50000;
        public const int FEMALE_GUMP_OFFSET = 60000;

        public const int WEATHER_TIMER = 6 * 60 * 1000;

        public const int PREDICTABLE_CHUNKS = 300;
        public const int PREDICTABLE_TILE_COUNT = 64 * PREDICTABLE_CHUNKS;
        public const int PREDICTABLE_STATICS = PREDICTABLE_TILE_COUNT * 2;
        public const int PREDICTABLE_MULTIS = PREDICTABLE_TILE_COUNT * 4;

        public static readonly bool[] BAD_CONTAINER_LAYERS =
        {
            false, // invalid [body]
            true, true, true, true, true, true, true, true,
            true, true, false, true, true, true, false, false,
            true, true, true, true,
            false, // backpack
            true, true, true, false, false, false, false, false
        };
    }
}