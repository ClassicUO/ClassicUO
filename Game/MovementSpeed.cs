#region license
//  Copyright (C) 2018 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//  (Copyright (c) 2018 ClassicUO Development Team)
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
using ClassicUO.Game.GameObjects;

namespace ClassicUO.Game
{
    public static class MovementSpeed
    {
        private const int STEP_DELAY_MOUNT_RUN = 100;
        private const int STEP_DELAY_MOUNT_WALK = 200;
        private const int STEP_DELAY_RUN = 200;
        private const int STEP_DELAY_WALK = 400;


        public static int TimeToCompleteMovement(Mobile mobile, bool run)
        {
            if (mobile.IsMounted) return run ? STEP_DELAY_MOUNT_RUN : STEP_DELAY_MOUNT_WALK;

            return run ? STEP_DELAY_RUN : STEP_DELAY_WALK;
        }
    }
}