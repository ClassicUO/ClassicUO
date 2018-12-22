using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ClassicUO.Utility;

namespace ClassicUO.Game
{
    static class Constants
    {
        public const int CHARACTER_ANIMATION_DELAY = 80;
        public const int ITEM_EFFECT_ANIMATION_DELAY = 50;

        public const int MAX_STEP_COUNT = 5;
        public const int TURN_DELAY = 100;
        public const int TURN_DELAY_FAST = 45;
        public const int WALKING_DELAY = 750;
        public const int PLAYER_WALKING_DELAY = 150;
        public const int DEFAULT_CHARACTER_HEIGHT = 16;
        public const int DEFAULT_BLOCK_HEIGHT = 16;

        public const float TIME_FADEOUT_TEXT = 1000;
        public const float TIME_DISPLAY_SYSTEM_MESSAGE_TEXT = 10000;

        public const int USED_LAYER_COUNT = 25;


        public const int CLEAR_TEXTURES_DELAY = 3000;
        public const int MAX_ANIMATIONS_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 5;
        public const int MAX_ART_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_GUMP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_SOUND_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 20;
        public const int MAX_MAP_OBJECT_REMOVED_BY_GARBAGE_COLLECTOR = 50;

        public const int DRAG_ITEMS_DISTANCE = 3;

        public const int MAX_FAST_WALK_STACK_SIZE = 5;

        public const int MAX_HOUSE_DISTANCE = 50;

        public const float FOLIAGE_ALPHA = .7f;
        public const int ALPHA_TIME = 25;
    }
}
