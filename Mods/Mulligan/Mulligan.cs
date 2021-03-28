//
//	Mulligan - 
//
//	People Playground Mod
//
//
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Mulligan
{
    public class Mulligan
    {
        public static bool useVerbose           = true;
        public static bool clearBeforeRestoring = true;

        public static void Main()
        {
            KeyMaps.LoadCustomKeys();

            ModAPI.Register<MulliganBehaviour>();

            // - - - - - - - - - - - - - - - - - - - - - -
            //  Add extra context button on each car spawn
            // - - - - - - - - - - - - - - - - - - - - - -
            ModAPI.OnItemSpawned += (object sender, UserSpawnEventArgs args) => {

                if ( args.SpawnableAsset.Prefab.name == "Car" ) {
                    
                    CarBehaviour carBehaviour = args.Instance.GetComponent<CarBehaviour>();
                    PhysicalBehaviour PB      = args.Instance.GetComponent<PhysicalBehaviour>();

                    PB.ContextMenuOptions.Buttons.Add(
                        new ContextMenuButton(
                            "SetCarSpeed", "Set Car Speed", "Modify the motorspeed", 
                            new UnityAction[1]{
                                (UnityAction) (() => ChangeCarSpeed( carBehaviour ))
                            })
                    );
                }
            };

            // - - - - - - - - - - - - - - - - - - - -
            //  Add item to Catalog: Key Remap Bollox
            // - - - - - - - - - - - - - - - - - - - -
            ModAPI.Register(
                new Modification()
                {
                    OriginalItem          = ModAPI.FindSpawnable("Brick"),
                    NameOverride          = "Mulligan Remap Keys",
                    NameToOrderByOverride = "MulliganKeys",
                    DescriptionOverride   = "Spawn this and right click to remap keys",
                    CategoryOverride      = ModAPI.FindCategory("Misc."),
                    ThumbnailOverride     = ModAPI.LoadSprite("thumb_dark.png", 5f),
                    
                    AfterSpawn = (Instance) =>
                     {
                         Instance.GetComponent<SpriteRenderer>().sortingLayerName = "Top";
                         Instance.GetComponent<SpriteRenderer>().sprite = ModAPI.LoadSprite("thumb_dark_sm.png", 2.1f);
                         Instance.FixColliders();
                         Instance.isStatic = true;
                         Instance.GetOrAddComponent<MulliganRemap>();
                     }
                }
            );

        }

        public static void ChangeCarSpeed(CarBehaviour carBehaviour)
        {
            DialogBox dialog = (DialogBox)null;
            dialog = DialogBoxManager.TextEntry(
                "Set MotorSpeed:", Math.Abs(carBehaviour.MotorSpeed).ToString(),

                new DialogButton("Set", true, new UnityAction[1] {
                    (UnityAction) (() => SetCarSpeed(carBehaviour, dialog)) }), 

                new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>())
            );
        }

        public static void SetCarSpeed(CarBehaviour carBehaviour, DialogBox diag)
        {
            if (float.TryParse(diag.EnteredText, out float mspeed) ) {
                carBehaviour.MotorSpeed = Math.Abs(mspeed) * -1;
            } else {
                ModAPI.Notify("Invalid value for motor speed");
            }

        }
    }
}  

         

