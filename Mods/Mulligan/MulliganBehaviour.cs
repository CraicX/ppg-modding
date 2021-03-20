using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;


namespace Mulligan
{ 

    public class MulliganBehaviour : MonoBehaviour
    {

        private static ObjectState[] SavedScene;
        private static List<LayeringOrder> SortingLayersList = new List<LayeringOrder>();
        public int ApplyLayerOrdering                       = 0;
        private List<Flipper> FlippingAholes                 = new List<Flipper>();
        private List<int> NoFlip                             = new List<int>();
        public static bool SlightMovement                    = false;
        public static float Intensity                        = 1f;
        public static float Speed                            = 1f;
        private static int LastSceneNumber                   = 1;


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


            if (SlightMovement) {
                {
                    float num               = MulliganBehaviour.Speed * Time.time;
                    base.transform.position = MulliganBehaviour.Intensity * Utils.GetPerlin2Mapped(num, num + 7892.387f);
                }

            }

            bool winKeyPressed = (Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightAlt));
            bool altKeyPressed = (Input.GetKey(KeyCode.LeftAlt));

            if (!altKeyPressed)
            {

                //
                //  heal peoples
                //
                if (Input.GetKeyUp(Mulligan.HealAll.keyCode))
                {
                    HealPeople(true);
                }
                else if (Input.GetKeyUp(Mulligan.HealSelected.keyCode))
                {

                    //
                    //  Heal all selected people
                    //
                    HealPeople(false);
                }
                else if (Input.GetKeyUp(Mulligan.FlipSelected.keyCode))
                {
                    //
                    //  flip items & humans
                    //
                    FlipSelectedItems();
                }

                else if (Input.GetKeyUp(Mulligan.RandomAction.keyCode))
                {

                    //
                    //  Experiments
                    //
                    if (winKeyPressed) { RandomExperiments2(); }
                    else RandomExperiments();

                }

                else if (Input.GetKeyUp(Mulligan.LayerUp.keyCode))
                {

                    //
                    //  Items forward
                    //
                    if (winKeyPressed) { AdjustSortingLayer("Top"); }
                    else { AdjustSortingOrder(10); }

                }

                else if (Input.GetKeyUp(Mulligan.LayerDown.keyCode))
                {

                    //
                    //  Items backward
                    //
                    if (winKeyPressed) { AdjustSortingLayer("Bottom"); }
                    else { AdjustSortingOrder(-10); }

                }

                else if (Input.GetKeyUp(Mulligan.MotorSpeedU.keyCode))
                {

                    //
                    //  Car speed slower
                    //
                    float speedAdjustment = winKeyPressed ? -100f : -25f;

                    AdjustCarSpeed(speedAdjustment);

                }


                else if (Input.GetKeyUp(Mulligan.MotorSpeedD.keyCode))
                {

                    //
                    //  Car speed faster
                    //
                    float speedAdjustment = winKeyPressed ? 100f : 25f;

                    AdjustCarSpeed(speedAdjustment);

                }
                else if (Input.GetKeyUp(Mulligan.SceneSave.keyCode))
                {

                    if (winKeyPressed)
                    {
                        DialogBox dialog = (DialogBox)null;
                        dialog = DialogBoxManager.TextEntry(
                            "Save to which scene #? (0-9)",
                            LastSceneNumber.ToString(),
                            new DialogButton("Save", true, new UnityAction[1]
                            {
                                (UnityAction) (() => SaveSceneNum(dialog))
                            }), new DialogButton("Cancel", true, (UnityAction[]) Array.Empty<UnityAction>()));

                        
                        void SaveSceneNum(DialogBox d)
                        {
                            string intext = d.EnteredText;
                            if (intext == "") intext = LastSceneNumber.ToString();

                            if (int.TryParse(intext, out int sceneNumber)
                                && (sceneNumber >= 0 || sceneNumber <= 9)) SceneSaveAsContraption(sceneNumber);
                            
                           else NotificationControllerBehaviour.Show("Invalid Scene #! Try (0-9)");

                        }
                    }
                    else
                    {

                        //
                        //  Save Scene
                        //
                        SceneSave();
                    }

                }
                else if (Input.GetKeyUp(Mulligan.SceneReset.keyCode))
                {

                    if (winKeyPressed)
                    {
                        DialogBox dialog = (DialogBox)null;
                        dialog = DialogBoxManager.TextEntry(
                            "Load which scene #? (0-9)",
                            LastSceneNumber.ToString(),
                            new DialogButton("Load", true, new UnityAction[1]
                            {
                                (UnityAction) (() => LoadSceneNum(dialog))
                            }), new DialogButton("Cancel", true, (UnityAction[]) Array.Empty<UnityAction>()));

                        void LoadSceneNum(DialogBox d)
                        {
                            string intext = d.EnteredText;
                            if (intext == "") intext = LastSceneNumber.ToString();

                            if (int.TryParse(intext, out int sceneNumber)
                                && (sceneNumber >= 0 || sceneNumber <= 9)) SceneLoadAsContraption(sceneNumber);

                            else NotificationControllerBehaviour.Show("Invalid Scene #! Try (0-9)");

                        }
                    }
                    else
                    {
                        //
                        //  Restore Scene
                        //
                        SceneRestore(Mulligan.ModOptions.clearBeforeRestoring);
                    }

                }

            } else {

                if (Input.GetKeyUp(Mulligan.PeepSit.keyCode))           ChangePose((int)PoseState.Sitting);
                else if (Input.GetKeyUp(Mulligan.PeepWalk.keyCode))     ChangePose((int)PoseState.Walking);
                else if (Input.GetKeyUp(Mulligan.PeepStand.keyCode))    ChangePose((int)PoseState.Rest);
                else if (Input.GetKeyUp(Mulligan.PeepCower.keyCode))    ChangePose((int)PoseState.Protective);
                else if (Input.GetKeyUp(Mulligan.PeepStumble.keyCode))  ChangePose((int)PoseState.Stumbling);
                else if (Input.GetKeyUp(Mulligan.PeepPain.keyCode))     ChangePose((int)PoseState.WrithingInPain);
                else if (Input.GetKeyUp(Mulligan.PeepFlailing.keyCode)) ChangePose((int)PoseState.Flailing);
                else if (Input.GetKeyUp(Mulligan.PeepSwimming.keyCode)) ChangePose((int)PoseState.Swimming);
            }
            
            if (ApplyLayerOrdering > 0)
            {
                --ApplyLayerOrdering;

                ReapplyLayerOrder();
            }

                
        }

        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: RANDOM EXPERIMENTS
        //
        public void RandomExperiments()
        {
            SetToBackground(1);
        }

        public void RandomExperiments2()
        {
            SetToBackground(-1);
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
                
                foreach (var person in people) {
                    healedCount++;
                    
                    foreach (var limb in person.Limbs) {
                        limb.gameObject.AddComponent<LifePoison>().Limb = limb;
                    }
                }
            }
            if (Mulligan.ModOptions.useVerboseFeed) NotificationControllerBehaviour.Show("# Healed: " + healedCount);

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
                    {
                        if (flipperTmp.instanceID == instID) canFlip = false;

                    }

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


            if (Mulligan.ModOptions.useVerboseFeed) NotificationControllerBehaviour.Show("Items flipped: " + countedItems + "   Humans flipped: " + countedHumans);

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

        NotificationControllerBehaviour.Show("SortingLayer set for <b>" + countedItems + "</b> items to " + layerName);

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

            NotificationControllerBehaviour.Show("SortingOrder set for <b>" + countedItems + "</b> items (delta: "+delta+")");
        }



        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SCENE SAVE
        //
        public void SceneSave()
        {

            SortingLayersList.Clear();

            var SelectedObjects = new List<GameObject>();

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

            if (Mulligan.ModOptions.useVerboseFeed) NotificationControllerBehaviour.Show("<b>Saved scene containing " + SelectedObjects.Count + "</b> items");

        }


        public void SceneSaveAsContraption(int sceneNumber = 1)
        {

            LastSceneNumber = sceneNumber;

            List<LayeringOrder> SortingContraption = new List<LayeringOrder>();

            var SelectedObjects    = new List<GameObject>();
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

            File.WriteAllText(ContraptionFile + "_layers.json", JsonConvert.SerializeObject(SortingContraption, Formatting.Indented));

            NotificationControllerBehaviour.Show("Saved Scene: <b>"+ContraptionName+"</b>");

        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SCENE RESTORE
        //
        public void SceneRestore( bool preWipe = true )
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

            if (Mulligan.ModOptions.useVerboseFeed) NotificationControllerBehaviour.Show("Restored Scene");

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
            SortingLayersList = JsonConvert.DeserializeObject<List<LayeringOrder>>(File.ReadAllText(ContraptionFile + "_layers.json"));

            NotificationControllerBehaviour.Show("Loaded Scene: <b>" + ContraptionName + "</b>");

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
                
            if (Mulligan.ModOptions.useVerboseFeed) NotificationControllerBehaviour.Show(verboseMsg);

        }



        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: CHANGE POSE
        //
        public void ChangePose(int poseNum) {

            //
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

                LayeringOrder LO;

                LayeringOrder[] TempList = SortingLayersList.ToArray();

                int i = -1;

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
        public void SetToBackground(int delta)
        {

            int countedItems = 0;

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                SelectedObject.gameObject.SetLayer(SelectedObject.gameObject.layer + delta);

                countedItems++;

            }

            NotificationControllerBehaviour.Show("set to background (delta: " + delta + ")");
        
       }


        

    }

}

