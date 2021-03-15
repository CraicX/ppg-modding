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
        public static KeyMap SceneSave    = new KeyMap( "SceneSave",     "Save the entire scene to memory", KeyCode.Keypad0);
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

            ModAPI.Register(
                new Modification()
                {
                    OriginalItem          = ModAPI.FindSpawnable("Brick"),
                    NameOverride          = "Mulligan Remap Keys",
                    NameToOrderByOverride = "MulliganKeys",
                    DescriptionOverride   = "Spawn this and right click to remap keys",
                    CategoryOverride      = ModAPI.FindCategory("Misc."),
                    ThumbnailOverride     = ModAPI.LoadSprite("thumb_dark.png", 5f),
                    AfterSpawn            = (Instance) =>
         {
             Instance.GetComponent<SpriteRenderer>().sortingLayerName = "Top";
             Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("thumb_dark.png", 1f);
             Instance.FixColliders();
             Instance.GetOrAddComponent<MulliganKeysBehaviour>();
         }
                }
            );

            //
            //  Check for saved control schematic
            //
            // LoadKeys();


            ModAPI.Register<MulliganBehaviour>();

        }

        public static void LoadKeys()
        {
            string filePath = "controls.json";

            if (!File.Exists( filePath ) ) return;

            try
            {
                foreach (KeyValuePair<string, int> kvPair in JsonConvert.DeserializeObject<Dictionary<string, int>>(File.ReadAllText(filePath)))
                {
                    foreach (KeyMap keyMap in KeyMaps) 
                    {
                        if (kvPair.Key == keyMap.name) keyMap.keyCode = (KeyCode) kvPair.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Log((object)"Invalid Json Keycode.");
                Debug.LogException(ex);
            }
        }

        public static void SaveKeys()
        {
            //var AllKeys = new Dictionary<string, int>(); 

            //foreach( KeyMap keyMap in Mulligan.Objec )) 
            //{
            //    AllKeys.Add(keyMap.name, (int) keyMap.keyCode);
            //}

            //File.WriteAllText("controls.json", JsonConvert.SerializeObject((object) AllKeys, Formatting.Indented));
        }


    }

    public class MulliganKeysBehaviour : MonoBehaviour
    {

        public void Start()
        {
            List<ContextMenuButton> buttons = GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons;

            buttons.Clear();

            int i = 0;

            foreach (Mulligan.KeyMap keyM in new List<Mulligan.KeyMap>() {
                Mulligan.HealAll,
                Mulligan.HealSelected,
                Mulligan.FlipSelected,
                Mulligan.LayerDown,
                Mulligan.LayerUp,
                Mulligan.MotorSpeedU,
                Mulligan.MotorSpeedD,
                Mulligan.SceneSave,
                Mulligan.SceneReset })
            {

                string dialogMsg = "Enter a new shortcut key for: \n" + keyM.title + "\n<color=green><size=26>Default: " + keyM.keyCode + "</size></color>";

                DialogBox dialog = (DialogBox)null;

                buttons.Add(new ContextMenuButton("MulliganKM" + ++i, "_REMAP: " + keyM.title, "For remapping the keys", () =>
               {

                   dialog = DialogBoxManager.KeyEntry(dialogMsg, keyM.keyCode, new DialogButton("SET", true, new UnityAction[1] {

                        (UnityAction)(() =>  {

                            keyM.keyCode = dialog.InputKey;
                            Mulligan.SaveKeys();
                            dialog.Close();

                        })

                   }), new DialogButton("Cancel", true, new UnityAction[1] { (UnityAction)(() => dialog.Close()) }));

               }));

            }
        }
    }
}  

         

