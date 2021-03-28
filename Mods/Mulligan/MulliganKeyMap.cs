using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.IO;

namespace Mulligan
{
    public static class KeyMaps
    {
        private static readonly List<KeyMap> _KMaps   = new List<KeyMap>();
        private static readonly string CustomKeysFile = "mulligan_keys.json";


        private static void Reset()
        {
            _KMaps.Clear();

            Add("HealSelected", "Heal All Humans",                KeyCode.Keypad1);
            Add("HealAll",      "Heal All Humans",                KeyCode.Keypad2);
            Add("FlipSelected", "Flip Selected Items",            KeyCode.Keypad4);
            Add("LayerBottom",  "Move to bottom layer",           KeyCode.Keypad7);
            Add("LayerTop",     "Move to top layer",              KeyCode.Keypad7, KeyCode.RightAlt);
            Add("LayerUp",      "Move Selected Up 1 Layer",       KeyCode.Keypad8);
            Add("LayerDown",    "Move Selected Down 1 Layer",     KeyCode.Keypad8, KeyCode.RightAlt);
            Add("LayerFground", "Move Selected out bground",      KeyCode.Keypad9);
            Add("LayerBground", "Move Selected to bground",       KeyCode.Keypad9, KeyCode.RightAlt);
            Add("QuickSave",    "Save entire scene to memory",    KeyCode.Keypad0);
            Add("QuickReset",   "Reset the scene",                KeyCode.KeypadMultiply);
            Add("SaveScene",    "Save scene to disk",             KeyCode.Keypad0, KeyCode.RightAlt);
            Add("LoadScene",   "Load scene from disk",            KeyCode.KeypadMultiply, KeyCode.RightAlt);
            Add("PeepWalk",     "Walk selected humans",           KeyCode.Keypad1, KeyCode.LeftAlt);
            Add("PeepStand",    "Selected humans Stand up",       KeyCode.Keypad2, KeyCode.LeftAlt);
            Add("PeepSit",      "Selected humans Sit Down",       KeyCode.Keypad3, KeyCode.LeftAlt);
            Add("PeepCower",    "Selected humans Cower",          KeyCode.Keypad4, KeyCode.LeftAlt);
            Add("PeepStumble",  "Stumble around aimlessly",       KeyCode.Keypad5, KeyCode.LeftAlt);
            Add("PeepPain",     "Keel over in pain",              KeyCode.Keypad6, KeyCode.LeftAlt);
            Add("PeepFlailing", "Flailing",                       KeyCode.Keypad7, KeyCode.LeftAlt);
            Add("PeepSwimming", "Swimming",                       KeyCode.Keypad8, KeyCode.LeftAlt);
            Add("RandomAction", "Debug Action",                   KeyCode.Keypad5);
        }

        public static void Add(KeyMap keyMap)
        {
            foreach (KeyMap _km in _KMaps) {
                if (_km.name == keyMap.name) _KMaps.Remove(_km);
            }

            _KMaps.Add(keyMap);
        }

        public static void Add(string _name, string _title, KeyCode _keyCode, KeyCode _modifier = KeyCode.None)
        {
            KeyMap keyMap = new KeyMap{ 
                name     = _name,
                title    = _title,
                keyCode  = _keyCode,
                modifier = _modifier
            };

            KeyMaps.Add(keyMap);
        }

        public static bool Check(string _name)
        {
            foreach (KeyMap _km in _KMaps) if (_km.name == _name) return _km.Check();

            return false;
        }

        public static void LoadCustomKeys()
        {
            Reset();
            
            if (!File.Exists(CustomKeysFile)) return;

            List<KeyMap> TempKeys = JsonConvert.DeserializeObject<List<KeyMap>>(File.ReadAllText(CustomKeysFile));

            foreach (KeyMap keyMapSet in TempKeys) Add(keyMapSet);

        }

        public static void SaveCustomKeys()
        {
            File.WriteAllText(CustomKeysFile, JsonConvert.SerializeObject(_KMaps, Formatting.Indented));
        }

        public static void ResetKeys()
        {
            if (File.Exists(CustomKeysFile)) File.Delete(CustomKeysFile);

            Reset();
            
            ModAPI.Notify("Original Key Schematic is set.");
        }

        public static List<KeyMap> Get()
        {
            return _KMaps;
        }

    }

    public class KeyMap
    {
        public static KeyCode ActiveModifier;
        public string name;
        public string title;
        public KeyCode keyCode;
        public KeyCode modifier;

        public bool Check()
        {
            if (!Input.GetKeyUp(this.keyCode)) return false;

            if (ActiveModifier == this.modifier) return true;

            return false;
        }

    }

}
