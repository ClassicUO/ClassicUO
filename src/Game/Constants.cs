#region license
// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.
#endregion

using System;

namespace ClassicUO.Game
{
    internal static class Constants
    {
        public const string WIKI_LINK = "https://github.com/andreakarasho/ClassicUO/wiki";

        public const int MIN_FPS = 15;
        public const int MAX_FPS = 250;
        public const int LOGIN_SCREEN_FPS = 60;

        public const int CHARACTER_ANIMATION_DELAY = 80;
        public const int ITEM_EFFECT_ANIMATION_DELAY = 50;

        public const int MAX_STEP_COUNT = 5;
        public const int TURN_DELAY = 100; // original client 12.5 fps = 80ms delay. FIXME: this patch causes a packet throttle. Reverted back to 100ms
        public const int TURN_DELAY_FAST = 45;
        public const int WALKING_DELAY = 150; // 750
        public const int PLAYER_WALKING_DELAY = 150;
        public const int DEFAULT_CHARACTER_HEIGHT = 16;
        public const int DEFAULT_BLOCK_HEIGHT = 16;

        public const float TIME_FADEOUT_TEXT = 1000;
        public const float TIME_DISPLAY_SYSTEM_MESSAGE_TEXT = 10000;

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

        public const int MAX_LAND_DATA_INDEX_COUNT = 0x4000;
        public const int MAX_STATIC_DATA_INDEX_COUNT = 0x10000;
        public const int MAX_LAND_TEXTURES_DATA_INDEX_COUNT = 0x4000;
        public const int MAX_GUMP_DATA_INDEX_COUNT = 0x10000;
        public const int MAX_SOUND_DATA_INDEX_COUNT = 0x0800;
        public const int MAX_MULTI_DATA_INDEX_COUNT = 0x2200;
        public const int MAX_MUSIC_DATA_INDEX_COUNT = 150;
        public const int MAX_ANIMATIONS_DATA_INDEX_COUNT = 2048;
        public const int MAX_LIGHTS_DATA_INDEX_COUNT = 100;

        public const ushort FIELD_REPLACE_GRAPHIC = 0x1826;
        public const ushort TREE_REPLACE_GRAPHIC = 0x0E59;

        public const int MIN_CIRCLE_OF_TRANSPARENCY_RADIUS = 20;
        public const int MAX_CIRCLE_OF_TRANSPARENCY_RADIUS = 200;

        public const int MAX_ABILITIES_COUNT = 32;

        public const int DRAG_ITEMS_DISTANCE = 3;
        public const int MIN_GUMP_DRAG_DISTANCE = 0;
        public const int MIN_PICKUP_DRAG_DISTANCE_PIXELS = 5;

        public const int MIN_VIEW_RANGE = 5;
        public const int MAX_VIEW_RANGE = 24;

        public const int OUT_RANGE_COLOR = 0x038B;
        public const int DEAD_RANGE_COLOR = 0x038E;

        public const int DEATH_SCREEN_TIMER = 1500;

        public const float SOUND_DELTA = 250;

        public const int MAX_JOURNAL_HISTORY_COUNT = 100;

        public const uint JOURNAL_LOCALSERIAL = 0xFFFFFFE1;
        public const uint SKILLSTD_LOCALSERIAL = 0xFFFFFFE2;
        public const uint PROFILE_LOCALSERIAL = 0xFFFFFFE3;

        public const byte MIN_CONTAINER_SIZE_PERC = 50;
        public const byte MAX_CONTAINER_SIZE_PERC = 200;

        public const int WEATHER_TIMER = 6 * 60 * 1000;
    }
}
