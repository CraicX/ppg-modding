using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mulligan
{
    class MulliganRemap : MonoBehaviour
    {

        private static DialogBox gDialog   = (DialogBox)null;
        private static bool inEdit         = false;
        private static KeyCode gModifier   = KeyCode.None;
        private static string gTitle       = "";
        private static KeyCode gDefaultKey = KeyCode.None;


        public void Start()
        {
            List<ContextMenuButton> buttons = GetComponent<PhysicalBehaviour>().ContextMenuOptions.Buttons;
            buttons.Clear();
            int i = 0;

            buttons.Add(new ContextMenuButton(
                "MulliganKM" + ++i, 
                "<color=red><size=10>_REMAP: RESET ALL KEYS</size></color>", 
                "For remapping the keys", 
                () => { KeyMaps.ResetKeys(); }
            ));

            foreach (KeyMap keyM in KeyMaps.KMaps) {

                gDialog = (DialogBox)null;

                buttons.Add(new ContextMenuButton(
                    "MulliganKM" + ++i, 
                    "<color=yellow><size=8>_REMAP: " + keyM.title + "</size></color>", 
                    "For remapping the keys", 
                    () => {

                        inEdit      = true;
                        gTitle      = keyM.title;
                        gDefaultKey = keyM.keyCode;

                        string dialogMsg    = "Enter a new shortcut key\n" + gTitle + "\n<color=green><size=26>"
                                            + "Default: " + gDefaultKey.ToString() + "</size></color>\n";

                        if (keyM.modifier != KeyCode.None)
                        {
                            dialogMsg  += "<size=20><color=red>" + "SET MODIFIER:" + "</color> "
                                        + "<color=yellow>" + keyM.modifier.ToString() + "</color></size>";
                        }

                        gDialog = DialogBoxManager.KeyEntry(dialogMsg, keyM.keyCode, 
                            new DialogButton("SET", true, new UnityAction[1] {
                                (UnityAction)(() => { UpdateKeys(keyM); }) }), 
                        
                            new DialogButton("Cancel", true, new UnityAction[1] { 
                                (UnityAction)(() => { gDialog.Close(); inEdit = false; }) })
                        );
               
                }));
            }
        }

        public void UpdateKeys(KeyMap keyM)
        {
            keyM.keyCode  = gDialog.InputKey;
            keyM.modifier = gModifier;

            gDialog.Close();
            inEdit = false;

            KeyMaps.SaveCustomKeys();

            ModAPI.Notify("Custom Keys Saved");
        }

        public void Update() 
        {
            if (inEdit)
            {
                KeyCode modifierKey = KeyCode.None;
                if (Input.GetKey(KeyCode.Escape))
                {
                    gDialog.Close();
                    inEdit = false;
                    return;
                }
                if (Input.GetKey(KeyCode.Return))
                {
                    gDialog.Buttons[0].Actions[0].Invoke();
                    return;
                }

                if (Input.GetKey(KeyCode.LeftWindows))
                    modifierKey = KeyCode.LeftWindows;
                else if (Input.GetKey(KeyCode.LeftAlt))
                    modifierKey = KeyCode.LeftAlt;
                else if (Input.GetKey(KeyCode.RightAlt))
                    modifierKey = KeyCode.RightAlt;
                else if (Input.GetKey(KeyCode.RightShift))
                    modifierKey = KeyCode.RightShift;
                else if (Input.GetKey(KeyCode.RightControl))
                    modifierKey = KeyCode.RightControl;


                if (gModifier != modifierKey && modifierKey != KeyCode.None) {
                    
                    gModifier = modifierKey;

                    string dialogMsg    = "Enter a new shortcut key\n" + gTitle + "\n"
                                        + "<color=green><size=26>Default: " + gDefaultKey + "</size></color>\n\n";

                    if (gModifier != KeyCode.None)
                    {
                        dialogMsg  += "<size=16><color=red>SET MODIFIER:</color> "
                                    + "<color=yellow>" + gModifier.ToString() + "</color></size>";
                    }

                    gDialog.SetTitle(dialogMsg);

                } else {
                        
                    if(Input.GetKeyUp(gModifier)) return;
                
                }
            }
        }
    }
}
