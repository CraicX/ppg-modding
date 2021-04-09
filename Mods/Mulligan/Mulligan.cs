//
//	Mulligan - 
//	People Playground Mod
//
using System;
using UnityEngine;

namespace Mulligan
{
    public class Mulligan
    {
        public static int  verboseLevel         = 1;
        public static bool clearBeforeRestoring = true;
        public static void OnLoad()
        {
            BindKeys();
        }
        public static void Main()
        {
            ModAPI.Register<MulliganBehaviour>();
        }

        public static bool KeyCheck(string _name)
        {
            KeyCode xModifierx = KeyCode.None;
         
            if (Input.GetKey(KeyCode.LeftAlt))  xModifierx = KeyCode.LeftAlt;
            if (Input.GetKey(KeyCode.RightAlt)) xModifierx = KeyCode.RightAlt;

            if (InputSystem.Down("Mulligan-" + _name))
            {
                // Make sure no modifier keys are being pressed unless its in the schematic
                //
                InputAction action = InputSystem.Actions["Mulligan-" + _name];

                return (bool)(action.SecondaryKey != KeyCode.None || action.SecondaryKey == xModifierx);
            }

            return false;
        }

        private static void BindKeys()
        {
            string[] KeySchematic =
            {
                "HealSelected > Heal Selected Limbs         > Keypad1",
                "HealAll      > Heal All Humans             > Keypad2",
                "Follow       > Follow Object               > Keypad3",
                "FlipSelected > Flip Selected Items         > Keypad4",
                "GrabItem     > Auto-equip closest item     > Keypad5",
                "LayerBottom  > Move to bottom layer        > End",
                "LayerTop     > Move to top layer           > Home",
                "LayerUp      > Move Selected Up 1 Layer    > PageUp",
                "LayerDown    > Move Selected Down 1 Layer  > PageDown",
                "LayerFG      > Move to foreground          > Keypad9",
                "LayerBG      > Move Selected to bground    > KeypadDivide",
                "QuickSave    > Save entire scene to memory > Keypad0",
                "QuickReset   > Reset the scene             > KeypadMultiply",
                "SaveScene    > Save scene to disk          > F5",
                "LoadScene    > Load scene from disk        > F10",
                "PeepWalk     > Action walk                 > Keypad1 > LeftAlt",
                "PeepStand    > Action stand Still          > Keypad1 > LeftAlt",
                "PeepSit      > Action sit down             > Keypad2 > LeftAlt",
                "PeepCower    > Action cower                > Keypad3 > LeftAlt",
                "PeepStumble  > Action stumble around       > Keypad4 > LeftAlt",
                "PeepPain     > Action feel the pain        > Keypad5 > LeftAlt",
                "PeepFlailing > Action flail                > Keypad6 > LeftAlt",
                "PeepSwimming > Action swim                 > Keypad7 > LeftAlt",
            };

            string            inputKey;
            InputAction       action;
            bool              initiatedMod = false;
            
            foreach (string keyMapping in KeySchematic)
            {
                string[] KeyParts = keyMapping.Split('>');

                inputKey = "Mulligan-" + KeyParts[0].Trim();

                if (!InputSystem.Has(inputKey))
                {
                    ModAPI.RegisterInput("[Mul] " + KeyParts[1].Trim(), inputKey, (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()));
                    
                    initiatedMod = true;
                    
                    if (KeyParts.Length == 4)
                    {
                        InputAction actionCreate = new InputAction(
                            (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[2].Trim()),
                            (KeyCode)Enum.Parse(typeof(KeyCode), KeyParts[3].Trim())
                        );

                        InputSystem.Actions.Add(inputKey, actionCreate);
                    }
                } else
                {
                    action = InputSystem.Actions[inputKey];
                    ModAPI.RegisterInput("[Mul] " + KeyParts[1].Trim(), inputKey, action.Key);

                    InputAction actionNew  = InputSystem.Actions[inputKey];
                    actionNew.SecondaryKey = action.SecondaryKey;
                }
            }
        }
    }
}  

         

