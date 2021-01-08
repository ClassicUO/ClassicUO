#region license

// Copyright (C) 2020 project dust765
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
using ClassicUO.Game.Managers;
using ClassicUO.IO.Resources;
using System.Text.RegularExpressions; //REGEX

namespace ClassicUO.Game.InteropServices.Runtime.UOClassicCombat
{
    internal class UOClassicCombatCliloc
    {
        private static int[] _startBandiesAtClilocs = new int[]
        {
            500956,
            500957,
            500958,
            500959,
            500960
        };
        private static int[] _stopBandiesAtClilocs = new int[]
        {
            500955,
            500962,
            500963,
            500964,
            500965,
            500966,
            500967,
            500968,
            500969,
            503252,
            503253,
            503254,
            503255,
            503256,
            503257,
            503258,
            503259,
            503260,
            503261,
            1010058,
            1010648,
            1010650,
            1060088,
            1060167
        };
        private static int[] _disarmedAtClilocs = new int[]
        {
            501708, //I have been disarmed.
            1004007 //You have been disarmed!
        };
        public void OnMessage(string text, ushort hue, string name, bool isunicode = true)
        {
            UOClassicCombatSelf UOClassicCombatSelf = UIManager.GetGump<UOClassicCombatSelf>();
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

            //STOP BANDIES / START BANDIES / GOT DISARMED - Failsafes incase cliloc not triggering

            //SYS MESSAGES ONLY
            if (name != "System" && text.Length <= 0)
                return;

            //STOP BANDIES TIMER
            for (int i = 0; i < _stopBandiesAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_stopBandiesAtClilocs[i]) == text)
                {
                    UOClassicCombatSelf?.ClilocTriggerStopBandies();
                    return;
                }
            }

            //START BANDIES TIMER
            for (int i = 0; i < _startBandiesAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_startBandiesAtClilocs[i]) == text)
                {
                    UOClassicCombatSelf?.ClilocTriggerStartBandies();
                    return;
                }
            }

            //GOT DISARMED
            for (int i = 0; i < _disarmedAtClilocs.Length; i++)
            {
                if (ClilocLoader.Instance.GetString(_disarmedAtClilocs[i]) == text)
                {
                    UOClassicCombatSelf?.ClilocTriggerGotDisarmed();
                    UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();

                    return;
                }
            }
            if (text == "Their attack disarms you!")
            {
                UOClassicCombatSelf?.ClilocTriggerGotDisarmed();
                UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();
                return;
            }

            //GOT HAMSTRUNG
            if (text == "Their attack hamstrings you!")
            {
                UOClassicCombatSelf?.ClilocTriggerGotHamstrung();
                UOClassicCombatBuffbar?.ClilocTriggerGotHamstrung();
                return;
            }

            //DISARM
            if (text == "You will now attempt to disarm your opponents.")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmON();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmON();
                return;
            }
            if (text == "You refrain from making disarm attempts.")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmOFF();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmOFF();
                return;
            }
            //SUCCESSFUL DISARM MSGES
            if (text == "Your strike disarms your target!")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmStriked();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmStriked();
                return;
            }
            if (text == "You successfully disarm your opponent!")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmStriked();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmStriked();
                return;
            }
            //FAILED DISARM MSGES
            if (text == "You fail to disarm your opponent.")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmFailed();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmFailed();
                return;
            }
            if (text == "You failed in your attempt do disarm.")
            {
                UOClassicCombatSelf?.ClilocTriggerDisarmFailed();
                UOClassicCombatBuffbar?.ClilocTriggerDisarmFailed();
                return;
            }

            //HAMSTRING
            if (text == "You will now attempt to hamstring your opponents.")
            {
                UOClassicCombatSelf?.ClilocTriggerHamstringON();
                UOClassicCombatBuffbar?.ClilocTriggerHamstringON();
                return;
            }
            if (text == "You refrain from making hamstring attempts.")
            {
                UOClassicCombatSelf?.ClilocTriggerHamstringOFF();
                UOClassicCombatBuffbar?.ClilocTriggerHamstringOFF();
                return;
            }
            if (text == "Your attack hamstrings your target!")
            {
                UOClassicCombatSelf?.ClilocTriggerHamstringStriked();
                UOClassicCombatBuffbar?.ClilocTriggerHamstringStriked();
                return;
            }
            if (text == "You fail to hamstring your opponent.")
            {
                UOClassicCombatSelf?.ClilocTriggerHamstringFailed();
                UOClassicCombatBuffbar?.ClilocTriggerHamstringFailed();
                return;
            }

            //TRACKING
            if (text == "You begin hunting.")
            {
                UOClassicCombatSelf?.ClilocTriggerTrackingON();
                return;
            }
            if (text == "You stop hunting.")
            {
                UOClassicCombatSelf?.ClilocTriggerTrackingOFF();
                return;
            }
            if (text.StartsWith("Now tracking:"))
            {
                UOClassicCombatSelf?.ClilocTriggerTrackingActive();
                return;
            }
            if (text == "Your target is too far away to continue tracking.")
            {
                UOClassicCombatSelf?.ClilocTriggerTrackingInActive();
                return;
            }
            //
            if (text.Contains("Distance"))
            {
                UOClassicCombatSelf?.ClilocTriggerTrackingActive();
                return;
            }

            //UCCSELF CLILOC TRIGGERS
            if (text.Contains("You must have a free hand"))
            {
                UOClassicCombatSelf?.ClilocTriggerFSFreeHands();
                return;
            }
            if (text.Contains("You must wait") && text.Contains("second before using another") && text.Contains("potion"))
            {

                string seconds = Regex.Match(text, @"\d+").Value; //first number
                int secondsS = Int32.Parse(seconds);

                ushort potion = 0;

                if (text.Contains("health"))
                {
                    potion = 0x0F0C;
                }
                else if(text.Contains("cure"))
                {
                    potion = 0x0F07;
                }

                UOClassicCombatSelf?.ClilocTriggerFSWaitX(secondsS, potion);
                return;
            }
            if (text.Contains("You have been hamstrung and cannot regain stamina at the moment"))
            {
                UOClassicCombatSelf?.ClilocTriggerFSHamstrungRefreshpot();
                return;
            }
            if (text == "You are already at full health.")
            {
                UOClassicCombatSelf?.ClilocTriggerFSFullHP();
                return;
            }
            if (text == "You are not poisoned.")
            {
                UOClassicCombatSelf?.ClilocTriggerFSNoPoison();
                return;
            }
            if (text == "You decide against drinking this potion, as you are already at full stamina.")
            {
                UOClassicCombatSelf?.ClilocTriggerFSFullStamina();
                return;
            }
        }
        public void OnCliloc(uint cliloc)
        {
            UOClassicCombatSelf UOClassicCombatSelf = UIManager.GetGump<UOClassicCombatSelf>();
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

            //STOP BANDIES TIMER
            for (int i = 0; i < _stopBandiesAtClilocs.Length; i++)
            {
                if (_stopBandiesAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerStopBandies();
                    return;
                }
            }

            //START BANDIES TIMER
            for (int i = 0; i < _startBandiesAtClilocs.Length; i++)
            {
                if (_startBandiesAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerStartBandies();
                    return;
                }
            }

            //GOT DISARMED
            for (int i = 0; i < _disarmedAtClilocs.Length; i++)
            {
                if (_disarmedAtClilocs[i] == cliloc)
                {
                    UOClassicCombatSelf?.ClilocTriggerGotDisarmed();
                    UOClassicCombatBuffbar?.ClilocTriggerGotDisarmed();
                    return;
                }
            }
        }
        public void OnOwnCharacterAnimation(uint action)
        {
            UOClassicCombatBuffbar UOClassicCombatBuffbar = UIManager.GetGump<UOClassicCombatBuffbar>();

            if (action >= 9 && action <= 15 || action == 18 || action == 19 || action >= 26 && action <= 29 || action == 31)
            {
                //26 horse_attack1h_slashright_01 / 27 horse_attackbow_01 / 28 horse_attackcrossbow_01 / 29 horse_attack2h_slashright_01 / 31_punch_punc_jab 01
                //9 attacklslash1h_01 / 10 attackpierce1h_01 / 11 attackbash1h_01 / 12 attackbash2h_01 / 13 attackslash2h_01 / 14 attackpierce2h_01 / 15 combatadvanced_1h_01 / 18 attackbow_01 / 19 attackcrossbow_01
                UOClassicCombatBuffbar?.ClilocTriggerSwing();
                UOClassicCombatCollection._HarmOnSwingTrigger = true;
            }
            return;
        }
    }
}