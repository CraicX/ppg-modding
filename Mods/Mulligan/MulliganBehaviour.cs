using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Mulligan
{
    [System.Runtime.InteropServices.Guid("68534A62-21F3-4148-A344-8DB567BA8EA9")]
    public class MulliganBehaviour : MonoBehaviour
    {
        private static ObjectState[] SavedScene;
        private static List<LayeringOrder> SortingLayersList = new List<LayeringOrder>();
        private List<Flipper> FlippingAholes = new List<Flipper>();
        private List<int> NoFlip             = new List<int>();
        private static int LastSceneNumber   = 1;
        public int ApplyLayerOrdering        = 0;
        public static bool SlightMovement    = false;
        public static float Intensity        = 1f;
        public static float Speed            = 1f;
        public static bool TriggerFlip       = false;
        public static KeyCode xModifierx;

        public struct LayeringOrder 
        {
            public string sortingLayerName;
            public int sortingOrder;
        }

        public struct Flipper
        {
            public int instanceID;
            public PersonBehaviour PB;
            public Vector2 moveA;
            public Vector2 moveB;
            public Vector3 flipScale;
            public int status;
        }

        public void Update()
        {
            if( TriggerFlip ) {
                TriggerFlip = false;
                FlipSelectedItems();
            }

            KeyMap.ActiveModifier = KeyCode.None;

            if (Input.GetKey(KeyCode.LeftAlt)) KeyMap.ActiveModifier  = KeyCode.LeftAlt;
            if (Input.GetKey(KeyCode.RightAlt)) KeyMap.ActiveModifier = KeyCode.RightAlt;

            //
            //  heal peoples
            //
            if      (KeyMaps.Check("HealAll"))      HealPeople(true);
            else if (KeyMaps.Check("HealSelected")) HealPeople(false);
            else if (KeyMaps.Check("FlipSelected")) FlipSelectedItems();
            else if (KeyMaps.Check("LayerUp"))      AdjustSortingOrder(10);
            else if (KeyMaps.Check("LayerDown"))    AdjustSortingOrder(-10);
            else if (KeyMaps.Check("LayerBGround")) SetToBackground(true);
            else if (KeyMaps.Check("LayerFGround")) SetToBackground(false);
            else if (KeyMaps.Check("LayerTop"))     AdjustSortingLayer("Top");
            else if (KeyMaps.Check("LayerBottom"))  AdjustSortingLayer("Bottom");
            else if (KeyMaps.Check("QuickSave"))    QuickSave();
            else if (KeyMaps.Check("QuickReset"))   QuickReset(Mulligan.clearBeforeRestoring);
            else if (KeyMaps.Check("SaveScene"))    SaveScene();
            else if (KeyMaps.Check("LoadScene"))    LoadScene();
            else if (KeyMaps.Check("PeepSit"))      ChangePose((int)PoseState.Sitting);
            else if (KeyMaps.Check("PeepWalk"))     ChangePose((int)PoseState.Walking);
            else if (KeyMaps.Check("PeepStand"))    ChangePose((int)PoseState.Rest);
            else if (KeyMaps.Check("PeepCower"))    ChangePose((int)PoseState.Protective);
            else if (KeyMaps.Check("PeepStumble"))  ChangePose((int)PoseState.Stumbling);
            else if (KeyMaps.Check("PeepPain"))     ChangePose((int)PoseState.WrithingInPain);
            else if (KeyMaps.Check("PeepFlailing")) ChangePose((int)PoseState.Flailing);
            else if (KeyMaps.Check("PeepSwimming")) ChangePose((int)PoseState.Swimming);
            
            if (ApplyLayerOrdering > 0)
            {
                --ApplyLayerOrdering;
                ReapplyLayerOrder();
            }
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: HEAL PEOPLE
        //
        public void HealPeople( bool everybody=false )
        {
            int healedCount = 0;

            if (!everybody)   {

                foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects) {

                    PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

                    if (person) {
                        healedCount++;
                        LimbBehaviour limb = SelectedObject.GetComponent<LimbBehaviour>();

                        limb.gameObject.AddComponent<LifePoison>().Limb = limb;
                        limb.GetComponent<LifePoison>().Spreads         = false;
                    }
                }
            } else {

                PersonBehaviour[] people = FindObjectsOfType<PersonBehaviour>();
                
                foreach (PersonBehaviour person in people) {
                    healedCount++;
                    foreach (LimbBehaviour limb in person.Limbs) {
                        limb.gameObject.AddComponent<LifePoison>().Limb = limb;
                    }
                }
            }
            
            if (Mulligan.useVerbose) ModAPI.Notify("# Healed: " + healedCount);
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: FLIP PERSON/ITEMS
        //
        //  Following code was based off of what was in the "ActiveHumans" mod.
        //  Very cool to have such a neat glimpse at what is possible
        //
        public void FlipSelectedItems()
        {
            FlippingAholes.Clear();

            int countedItems  = 0;
            int countedHumans = 0;

            NoFlip.Clear();

            PersonBehaviour     PBO;
            Rigidbody2D         head; 
            Vector2             moveDif;
            Vector2             pos;
            PhysicalBehaviour   hObj;
            Flipper             flipper;

            //  Flip people first so we can handle hand held items and not double flip
            foreach (PhysicalBehaviour Selected in SelectionController.Main.SelectedObjects)
            {
                PBO = Selected.gameObject.GetComponentInParent<PersonBehaviour>();

                if (PBO)
                {
                    bool canFlip = true;
                    int instID   = PBO.GetInstanceID();

                    foreach (Flipper flipperTmp in FlippingAholes)
                        if (flipperTmp.instanceID == instID) canFlip = false;

                    if (canFlip)
                    {
                        flipper = new Flipper()
                        {

                            PB         = PBO,
                            flipScale  = PBO.transform.localScale,
                            instanceID = PBO.GetInstanceID(),
                            status     = 1
                        };

                        flipper.flipScale.x *= -1;

                        FlippingAholes.Add(flipper);

                        foreach (LimbBehaviour limb in flipper.PB.Limbs)
                        {
                            if (limb == flipper.PB.Limbs[1]) flipper.moveB = limb.transform.position;
                            
                            if (limb.HasJoint)
                            {

                                limb.BreakingThreshold *= 8;

                                if (   limb.name != "LowerBody" 
                                    && limb.name != "MiddleBody" 
                                    && limb.name != "UpperBody" 
                                    && limb.name != "UpperArm" 
                                    && limb.name != "UpperArmFront" 
                                    && limb.name != "Head")
                                {
                                    JointAngleLimits2D t = limb.Joint.limits;
                                    t.min *= -1f;
                                    t.max *= -1f;
                                    limb.Joint.limits = t;
                                    limb.OriginalJointLimits = new Vector2(limb.OriginalJointLimits.x * -1f, limb.OriginalJointLimits.y * -1f);
                                }
                            }
                        }

                        Transform headT = flipper.PB.gameObject.transform.GetChild(5);

                        if (headT)
                        {
                            head = headT.GetComponent<Rigidbody2D>();

                            flipper.PB.transform.localScale = flipper.flipScale;

                            foreach (LimbBehaviour limb in flipper.PB.Limbs)
                            {
                                if (limb == flipper.PB.Limbs[1]) flipper.moveA = head.transform.position;
                            }

                            moveDif = flipper.moveB - flipper.moveA;

                            flipper.PB.AngleOffset *= -1f;
                            flipper.PB.transform.position = new Vector2(flipper.PB.transform.position.x + moveDif.x, flipper.PB.transform.position.y);

                            foreach (LimbBehaviour limb in flipper.PB.Limbs) if (limb.HasJoint) limb.Broken = false;

                            GripBehaviour[] grips = flipper.PB.GetComponentsInChildren<GripBehaviour>();

                            foreach (GripBehaviour grip in grips)
                            {
                                if (grip.isHolding) 
                                {
                                    hObj = grip.CurrentlyHolding;
                                    NoFlip.Add(hObj.GetHashCode());
                                    pos  = grip.transform.TransformPoint(grip.GripPosition);
                                    hObj.transform.position   = pos;
                                    hObj.transform.localScale = new Vector2(hObj.transform.localScale.x * 1f, hObj.transform.localScale.y * -1f);
                                }
                            }

                            ++countedHumans;
                        }
                    }
                }
            }

            foreach (PhysicalBehaviour Selected in SelectionController.Main.SelectedObjects)
            {
                PBO = Selected.gameObject.GetComponentInParent<PersonBehaviour>();

                if (!PBO)
                {
                    if (NoFlip.Contains(Selected.GetHashCode())) continue;

                    countedItems++;

                    Vector3 theScale = Selected.transform.localScale;
                    theScale.x *= -1;
                    Selected.transform.localScale = theScale;
                }
            }

           if (Mulligan.useVerbose) ModAPI.Notify("Items flipped: " + countedItems + "   Humans flipped: " + countedHumans);
        }

        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SAVE SCENE
        //
        private void SaveScene()
        {
            DialogBox dialog = null;
            dialog = DialogBoxManager.TextEntry(
                "Save to which scene #? (0-9)",
                LastSceneNumber.ToString(),
                new DialogButton("Save", true, new UnityAction[1] { (UnityAction) (() => SaveSceneNum(dialog)) }), 
                new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));


            void SaveSceneNum(DialogBox d)
            {
                string intext = d.EnteredText;
                if (intext == "") intext = LastSceneNumber.ToString();

                if (int.TryParse(intext, out int sceneNumber)
                    && (sceneNumber >= 0 || sceneNumber <= 9)) SceneSaveAsContraption(sceneNumber);

                else ModAPI.Notify("Invalid Scene #! Try (0-9)");
            }
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: LOAD SCENE
        //
        private void LoadScene()
        {
            DialogBox dialog = (DialogBox)null;

            dialog = DialogBoxManager.TextEntry(
                "Load which scene #? (0-9)", LastSceneNumber.ToString(),
                new DialogButton("Load", true, new UnityAction[1] { (UnityAction)(() => LoadSceneNum(dialog)) }),
                new DialogButton("Cancel", true, (UnityAction[])Array.Empty<UnityAction>()));

            void LoadSceneNum(DialogBox d)
            {
                string intext = d.EnteredText;
                if (intext == "") intext = LastSceneNumber.ToString();

                if (int.TryParse(intext, out int sceneNumber) && (sceneNumber >= 0 || sceneNumber <= 9))
                    SceneLoadAsContraption(sceneNumber);

                else ModAPI.Notify("Invalid Scene #! Try (0-9)");
            }
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: ADJUST SORTING LAYER
        //
        public void AdjustSortingLayer(string layerName)
        {
            int countedItems = 0;

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                SelectedObject.GetComponent<SpriteRenderer>().sortingLayerName = layerName;
                countedItems++;
            }

            ModAPI.Notify("SortingLayer set for <b>" + countedItems + "</b> items to " + layerName);
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: ADJUST SORTING ORDER
        //
        public void AdjustSortingOrder(int delta)
        {
            int countedItems = 0;

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                SelectedObject.GetComponent<SpriteRenderer>().sortingOrder += delta;
                countedItems++;
            }

            ModAPI.Notify("SortingOrder set for <b>" + countedItems + "</b> items (delta: "+delta+")");
        }



        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SCENE SAVE
        //
        public void QuickSave()
        {
            SortingLayersList.Clear();

            List<GameObject> SelectedObjects = new List<GameObject>();

            foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
            {
                //  Also get the sorting layers so we can re-apply it
                SpriteRenderer SR = selectedObject.GetComponent<SpriteRenderer>();

                SortingLayersList.Add(new LayeringOrder()
                {
                    sortingLayerName = SR.sortingLayerName,
                    sortingOrder     = SR.sortingOrder
                });

                SelectedObjects.Add(selectedObject.gameObject);
            }

            MulliganBehaviour.SavedScene = ObjectStateConverter.Convert(SelectedObjects.ToArray(), new Vector3());

            ModAPI.Notify("<b>Saved scene containing " + SelectedObjects.Count + "</b> items");
        }


        public void SceneSaveAsContraption(int sceneNumber = 1)
        {
            LastSceneNumber = sceneNumber;

            List<LayeringOrder> SortingContraption = new List<LayeringOrder>();

            List<GameObject> SelectedObjects    = new List<GameObject>();
            string ContraptionName = "scene_" + sceneNumber;
            string ContraptionFile = "Contraptions/" + ContraptionName + "/" + ContraptionName;

            foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
            {
                //  Also get the sorting layers so we can re-apply it
                SpriteRenderer SR = selectedObject.GetComponent<SpriteRenderer>();

                SortingContraption.Add(new LayeringOrder()
                {
                    sortingLayerName = SR.sortingLayerName,
                    sortingOrder     = SR.sortingOrder
                });

                SelectedObjects.Add(selectedObject.gameObject);
            }

            ObjectState[] objectStates = ObjectStateConverter.Convert(SelectedObjects.ToArray(), new Vector3());

            ContraptionSerialiser.SaveThumbnail(objectStates, ContraptionName);
            ContraptionSerialiser.SaveContraption(ContraptionName, objectStates);

            File.WriteAllText(ContraptionFile + ".layers", JsonConvert.SerializeObject(SortingContraption, Formatting.Indented));

            ModAPI.Notify("Saved Scene: <b>"+ContraptionName+"</b>");
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SCENE RESTORE
        //
        public void QuickReset( bool preWipe = true )
        {
            if (MulliganBehaviour.SavedScene.Length <= 0) return;

            if (preWipe)
            {
                foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
                {
                    if (selectedObject.Deletable) UnityEngine.Object.Destroy(selectedObject.gameObject);
                }
            }

            UndoControllerBehaviour.RegisterAction(
                (IUndoableAction)new PasteLoadAction(
                    (IEnumerable<UnityEngine.Object>)ObjectStateConverter.Convert(MulliganBehaviour.SavedScene,
                    new Vector3()), "Paste"));

            ModAPI.Notify("Restored Scene");

            ApplyLayerOrdering = 10;
        }

        public void SceneLoadAsContraption(int sceneNumber = 1)
        {
            string ContraptionName = "scene_" + sceneNumber;

            foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
            {
                if (selectedObject.Deletable) UnityEngine.Object.Destroy(selectedObject.gameObject);
            }
            
            string ContraptionFile      = "Contraptions/" + ContraptionName + "/" + ContraptionName;

            ContraptionMetaData myScene = new ContraptionMetaData(ContraptionName)
            {
                PathToMetadata  = ContraptionFile + ".json",
                PathToDataFile  = System.IO.Path.Combine(ContraptionFile + ".jaap"),
                PathToThumbnail = System.IO.Path.Combine(ContraptionFile + ".png")
            };
            
            Contraption contraption     = ContraptionSerialiser.LoadContraption(myScene);

            UndoControllerBehaviour.RegisterAction(
                (IUndoableAction)new PasteLoadAction(
                    (IEnumerable<UnityEngine.Object>)ObjectStateConverter.Convert(
                        contraption.ObjectStates, new Vector3()), "Paste"));

            SortingLayersList.Clear();
            SortingLayersList = JsonConvert.DeserializeObject<List<LayeringOrder>>(File.ReadAllText(ContraptionFile + ".layers"));

            ModAPI.Notify("Loaded Scene: <b>" + ContraptionName + "</b>");

            ApplyLayerOrdering = 10;
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: ADJUST CAR SPEED
        //
        public void AdjustCarSpeed( float deltaSpeed ) 
        {
            string verboseMsg = "";

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                if (!SelectedObject.gameObject.GetComponentInParent<CarBehaviour>()) continue;

                CarBehaviour carGuy = SelectedObject.gameObject.GetComponentInParent<CarBehaviour>();
                carGuy.MotorSpeed += deltaSpeed;

                verboseMsg += "[" + carGuy.name + ": " + carGuy.MotorSpeed + "]  ";
            }
                
           if (Mulligan.useVerbose) ModAPI.Notify(verboseMsg);
        }

        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: CHANGE POSE
        //
        public void ChangePose(int poseNum) {

            //  All selected people sit down
            //
            List<PersonBehaviour> PeopleAlreadyPosed = new List<PersonBehaviour>();

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

                if (person)
                {
                    if (PeopleAlreadyPosed.Contains(person)) continue;

                    PeopleAlreadyPosed.Add(person);

                    if (person.OverridePoseIndex == poseNum) person.OverridePoseIndex = -1;
                    else person.OverridePoseIndex = poseNum;

                }
            }
        }


        public void ReapplyLayerOrder()
        {

            //  Now reapply the previous layer settings
            //
            if (SortingLayersList.Count > 0)
            {
                int i = -1;
            
                LayeringOrder LO;
                LayeringOrder[] TempList = SortingLayersList.ToArray();

                foreach (PhysicalBehaviour selectedObject in Global.main.PhysicalObjectsInWorld)
                {
                    //  Also get the sorting layers so we can re-apply it
                    SpriteRenderer SR = selectedObject.GetComponent<SpriteRenderer>();

                    LO = TempList[++i];

                    SR.sortingLayerName = LO.sortingLayerName;
                    SR.sortingOrder     = LO.sortingOrder;
                }
            }
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SET TO BACKGROUND
        //
        public void SetToBackground(bool doBackground)
        {
            int countedItems = 0;
            int layerNumber  = doBackground ? 2 : 9;

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                SelectedObject.gameObject.SetLayer(layerNumber);
                countedItems++;
            }

            ModAPI.Notify("set to " + (doBackground ? "background" : "foreground") );
       }
    }
}

