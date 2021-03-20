//
//	Mulligan - 
//
//	People Playground Mod
//
//
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;


namespace Mulligan
{
    public class Mulligan
    {
        public static class ModOptions 
        {
            public static bool useVerboseFeed       = false;
            public static bool clearBeforeRestoring = true;
            
        }

        public static Modification saveScene;
                                                                                                      
        public static KeyMap HealSelected = new KeyMap( "HealSelected",  "Heal Selected Humans",            KeyCode.Keypad1 );
        public static KeyMap HealAll      = new KeyMap( "HealAll",       "Heal All Humans",                 KeyCode.Keypad2 );
        public static KeyMap FlipSelected = new KeyMap( "FlipSelected",  "Flip Selected Items",             KeyCode.Keypad4 );
        public static KeyMap LayerDown    = new KeyMap( "LayerDown",     "Move Selected Down 1 Layer",      KeyCode.Keypad7 );
        public static KeyMap LayerUp      = new KeyMap( "LayerUp",       "Move Selected Up 1 Layer",        KeyCode.Keypad8 );
        public static KeyMap SceneSave    = new KeyMap( "SceneSave",     "Save the entire scene to memory", KeyCode.Keypad0 );
        public static KeyMap SceneReset   = new KeyMap( "SceneReset",    "Reset the scene",                 KeyCode.KeypadMultiply );
        public static KeyMap MotorSpeedD  = new KeyMap( "MotorSpeedD",   "Makes cars slower",               KeyCode.Keypad3 );
        public static KeyMap MotorSpeedU  = new KeyMap( "MotorSpeedU",   "Makes cars faster",               KeyCode.Keypad6 );
        public static KeyMap PeepWalk     = new KeyMap( "PeepWalk",      "Walk selected humans",            KeyCode.Keypad1, true );
        public static KeyMap PeepStand    = new KeyMap( "PeepStand",     "Selected humans Stand up",        KeyCode.Keypad2, true );
        public static KeyMap PeepSit      = new KeyMap( "PeepSit",       "Selected humans Sit Down",        KeyCode.Keypad3, true );
        public static KeyMap PeepCower    = new KeyMap( "PeepCower",     "Selected humans Cower in Fear",   KeyCode.Keypad4, true );
        public static KeyMap PeepStumble  = new KeyMap( "PeepStumble",   "Stumble around aimlessly",        KeyCode.Keypad5, true );
        public static KeyMap PeepPain     = new KeyMap( "PeepPain",      "Keel over in pain",               KeyCode.Keypad6, true );
        public static KeyMap PeepFlailing = new KeyMap( "PeepFlailing",  "Flailing",                        KeyCode.Keypad7, true );
        public static KeyMap PeepSwimming = new KeyMap( "PeepSwimming",  "Swimming",                        KeyCode.Keypad8, true );
        public static KeyMap RandomAction = new KeyMap( "RandomAction",  "Debug Action",                    KeyCode.Keypad5, false );

        public static List<KeyMap> KeyMaps = new List<KeyMap>();

        public class KeyMap
        {
            public string name;
            public string title;
            public KeyCode keyCode;
            public bool modifier;

            public KeyMap(string _name, string _title, KeyCode _keyCode, bool _modifier=false)
            {
                this.name     = _name;
                this.title    = _title;
                this.keyCode  = _keyCode;
                this.modifier = _modifier;
            }

        }



        public static void Main()
        {

            ModAPI.Register<MulliganBehaviour>();

        }

    }
}  

         

