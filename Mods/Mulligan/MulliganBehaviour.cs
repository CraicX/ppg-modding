using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private List<Flipper> FlippingAholes                 = new List<Flipper>();
        private List<int> NoFlip                             = new List<int>();
        private static int LastSceneNumber                   = 1;
        
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
                List<PhysicalBehaviour> FlipList = new List<PhysicalBehaviour>(SelectionController.Main.SelectedObjects);

                FlipSelectedItems(FlipList);
            }

            KeyMap.ActiveModifier = KeyCode.None;

            if (Input.GetKey(KeyCode.LeftAlt)) KeyMap.ActiveModifier  = KeyCode.LeftAlt;
            if (Input.GetKey(KeyCode.RightAlt)) KeyMap.ActiveModifier = KeyCode.RightAlt;

            //
            //  Check for Hotkey, Execute associated Function
            //
                 if (KeyMaps.Check("HealAll"     )) HealPeople(true);
            else if (KeyMaps.Check("HealSelected")) HealPeople(false);
            else if (KeyMaps.Check("LayerUp"     )) AdjustSortingOrder(10);
            else if (KeyMaps.Check("LayerDown"   )) AdjustSortingOrder(-10);
            else if (KeyMaps.Check("LayerBG"     )) LayerBG(true);
            else if (KeyMaps.Check("LayerFG"     )) LayerBG(false);
            else if (KeyMaps.Check("LayerTop"    )) AdjustSortingLayer("Top");
            else if (KeyMaps.Check("LayerBottom" )) AdjustSortingLayer("Bottom");
            else if (KeyMaps.Check("Follow"      )) FollowObject(SelectionController.Main.SelectedObjects);
            else if (KeyMaps.Check("QuickSave"   )) QuickSave();
            else if (KeyMaps.Check("QuickReset"  )) QuickReset(Mulligan.clearBeforeRestoring);
            else if (KeyMaps.Check("SaveScene"   )) SaveScene();
            else if (KeyMaps.Check("LoadScene"   )) LoadScene();
            else if (KeyMaps.Check("GrabItem"    )) GrabClosestItem();
            else if (KeyMaps.Check("PeepSit"     )) ChangePose((int)PoseState.Sitting);
            else if (KeyMaps.Check("PeepWalk"    )) ChangePose((int)PoseState.Walking);
            else if (KeyMaps.Check("PeepStand"   )) ChangePose((int)PoseState.Rest);
            else if (KeyMaps.Check("PeepCower"   )) ChangePose((int)PoseState.Protective);
            else if (KeyMaps.Check("PeepStumble" )) ChangePose((int)PoseState.Stumbling);
            else if (KeyMaps.Check("PeepPain"    )) ChangePose((int)PoseState.WrithingInPain);
            else if (KeyMaps.Check("PeepFlailing")) ChangePose((int)PoseState.Flailing);
            else if (KeyMaps.Check("PeepSwimming")) ChangePose((int)PoseState.Swimming);
            else if (KeyMaps.Check("FlipSelected"))
            {
                List<PhysicalBehaviour> FlipList = new List<PhysicalBehaviour>(SelectionController.Main.SelectedObjects);
                FlipSelectedItems(FlipList);
            }

            if (ApplyLayerOrdering > 0)
            {
                --ApplyLayerOrdering;
                ReapplyLayerOrder();
            }
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: GRAB CLOSEST ITEM
        //
        public void GrabClosestItem()
        {
            PersonBehaviour PBO; 
            Rigidbody2D     LowerArm;
            List<int>       PeepsWithItems  = new List<int>();
            int             peepsEquipped   = 0;
            int             peepsDropped    = 0;
            int             verbose         = Mulligan.verboseLevel;

            Mulligan.verboseLevel = 0;
            //  Look for peeps and find their front arm
            foreach (PhysicalBehaviour Selected in SelectionController.Main.SelectedObjects)
            {
                PBO = Selected.gameObject.GetComponentInParent<PersonBehaviour>();
                if (!PBO) continue;

                LowerArm = PBO.transform.GetChild(9).transform.GetChild(1).GetComponent<Rigidbody2D>();
                if (!LowerArm) continue;

                if (PeepsWithItems.Contains(LowerArm.GetInstanceID())) continue;
                PeepsWithItems.Add(LowerArm.GetInstanceID());

                Collider2D[] noCollide;

                GripBehaviour GB = LowerArm.GetComponent<GripBehaviour>();

                if (GB.isHolding)
                {
                    //  We is already holding, so drop it
                    FixedJoint2D Itemjoint;
                    GB.isHolding = false;
                    GB.CurrentlyHolding.IsWeightless = false;
                    GB.CurrentlyHolding.MakeWeightful();

                    //  Need to loop through these incase PickUpNearestObject() was also triggered
                    //  and created its own joint
                    while (Itemjoint = GB.gameObject.GetComponent<FixedJoint2D>())
                    {
                        UnityEngine.Object.DestroyImmediate(Itemjoint);
                    }

                    GB.CurrentlyHolding.beingHeldByGripper = false;
                    GB.CurrentlyHolding = (PhysicalBehaviour)null;

                    ++peepsDropped;

                    continue;
                }

                Vector2 worldPoint        = (Vector2)GB.transform.TransformPoint(GB.GripPosition);
                Vector2 NearestHoldingPos = new Vector2(0.0f, 0.0f);
                FixedJoint2D joint;

                PhysicalBehaviour phys = (PhysicalBehaviour)null;

                float num2 = float.MaxValue;

                //  loop thru objects and determine which is closest to peep
                foreach (PhysicalBehaviour physicalBehaviour in Global.main.PhysicalObjectsInWorld)
                {
                    // skip items arealy being held
                    if (physicalBehaviour.beingHeldByGripper) continue;

                    Vector2 localHoldingPoint = physicalBehaviour.GetNearestLocalHoldingPoint(worldPoint, out float distance);
                    if ((double)distance < (double)num2)
                    {
                        num2              = distance;
                        NearestHoldingPos = localHoldingPoint;
                        phys              = physicalBehaviour;
                    }
                }
                if (!(bool)(UnityEngine.Object)phys) return;

                bool PersonFlipped = PBO.transform.localScale.x  < 0.0f;
                bool ItemFlipped   = phys.transform.localScale.x < 0.0f;
                if (PersonFlipped != ItemFlipped)
                {
                    List<PhysicalBehaviour> FlipList = new List<PhysicalBehaviour>
                    {
                        phys
                    };

                    FlipSelectedItems(FlipList);
                }

                phys.IsWeightless = true;
                phys.MakeWeightless();

                GB.isHolding        = true;
                GB.CurrentlyHolding = phys;

                float ArmRotation       = LowerArm.rotation;
                phys.transform.rotation = Quaternion.Euler(0.0f, 0.0f, PersonFlipped ? ArmRotation + 95.0f : ArmRotation - 95.0f);

                //  Adjust layers of the hand and the held object so object is over body but under hand
                //  But try not to goof the layers if someone already assigned values
                int SOrder = LowerArm.GetComponent<SpriteRenderer>().sortingOrder;
                if (SOrder < 10)
                {
                    SOrder = 10;
                    LowerArm.GetComponent<SpriteRenderer>().sortingOrder = SOrder;
                }
                phys.GetComponent<SpriteRenderer>().sortingLayerName = LowerArm.GetComponent<SpriteRenderer>().sortingLayerName;
                phys.GetComponent<SpriteRenderer>().sortingOrder     = SOrder - 1;

                //  @TODO:
                //  When multiple GripPositions exist, there should be a way so this remembers the preferred GripPosition by the user
                //  Then next auto-equip will automatically choose preferred position
                phys.beingHeldByGripper = true;
                phys.transform.position += GB.transform.TransformPoint(GB.GripPosition) - phys.transform.TransformPoint((Vector3)NearestHoldingPos);

                //  Attaches object(phys) to the hand (GB : GripBehaviour)
                joint                 = GB.gameObject.AddComponent<FixedJoint2D>();
                joint.connectedBody   = phys.rigidbody;
                joint.anchor          = (Vector2)GB.GripPosition;
                joint.connectedAnchor = NearestHoldingPos;
                joint.enableCollision = false;

                //  Disables collisions between the object and person holding it.
                //  But do they remain disabled when the object switches owners?
                noCollide = GB.transform.root.GetComponentsInChildren<Collider2D>();
                foreach (Collider2D componentsInChild in phys.transform.root.GetComponentsInChildren<Collider2D>())
                {
                    foreach (Collider2D collider2 in noCollide)
                    {
                        if ((bool)(UnityEngine.Object)collider2 && (bool)(UnityEngine.Object)componentsInChild)
                            Physics2D.IgnoreCollision(componentsInChild, collider2);
                    }
                }

                ++peepsEquipped;

            }

            Mulligan.verboseLevel = verbose;

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<color=green>Equipped: " + peepsEquipped + "</color>  <color=red>Dropped: " + peepsDropped);

        }

        

        public void FollowObject( ReadOnlyCollection<PhysicalBehaviour> SelectedObjList )
        {
            FollowObject(SelectedObjList[0]);
        }

        public void FollowObject( PhysicalBehaviour SelectedObj )
        {
            Global.main.CameraControlBehaviour.CurrentlyFollowing.Add(SelectedObj);
            if (Mulligan.verboseLevel >= 2) ModAPI.Notify("Following: " + SelectedObj.name);
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
            
            if (Mulligan.verboseLevel >= 2) ModAPI.Notify("# Healed: " + healedCount);
        }


        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: FLIP PERSON/ITEMS
        //
        //  Following code was based off of what was in the "ActiveHumans" mod.
        //  Very cool to have such a neat glimpse at what is possible
        //
        public void FlipSelectedItems(List<PhysicalBehaviour> ItemsToFlip)
        {
            FlippingAholes.Clear();

            int countedItems  = 0;
            int countedHumans = 0;

            NoFlip.Clear();

            PersonBehaviour   PBO;
            Rigidbody2D       head;
            Vector2           moveDif;
            PhysicalBehaviour hObj;
            Flipper           flipper;

            //  Flip people first so we can handle hand held items and not double flip
            foreach (PhysicalBehaviour Selected in ItemsToFlip)
            {
                PBO = Selected.gameObject.GetComponentInParent<PersonBehaviour>();

                if (PBO)
                {
                    bool canFlip = true;
                    int instID = PBO.GetInstanceID();

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
                        FixedJoint2D Itemjoint;

                        foreach (LimbBehaviour limb in flipper.PB.Limbs)
                        {
                            if (limb == flipper.PB.Limbs[1]) flipper.moveB = limb.transform.position;

                            if (limb.HasJoint)
                            {
                                limb.BreakingThreshold *= 8;

                                if (limb.name != "LowerBody"
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

                                    // Break joint
                                    while (Itemjoint = grip.gameObject.GetComponent<FixedJoint2D>())
                                    {
                                        UnityEngine.Object.DestroyImmediate(Itemjoint);
                                    }
                                    
                                    //  Flip Item
                                    Vector3 theScale = hObj.transform.localScale;
                                    theScale.x *= -1.0f;
                                    hObj.transform.localScale = theScale;

                                    //  Set new item rotation
                                    hObj.transform.rotation = Quaternion.Euler(
                                        0.0f, 0.0f,
                                        grip.GetComponentInParent<Rigidbody2D>().rotation + 95.0f * (flipper.PB.transform.localScale.x < 0.0f ? 1.0f : -1.0f));

                                    //  Move item to flipped position
                                    Vector2 GripPoint = hObj.GetNearestLocalHoldingPoint(grip.transform.TransformPoint(grip.GripPosition), out float distance);
                                    hObj.transform.position += grip.transform.TransformPoint(grip.GripPosition) - 
                                        hObj.transform.TransformPoint(GripPoint);
                                    
                                    //  Create new joint
                                    FixedJoint2D joint;
                                    joint                 = grip.gameObject.AddComponent<FixedJoint2D>();
                                    joint.connectedBody   = hObj.rigidbody;
                                    joint.anchor          = (Vector2)grip.GripPosition;
                                    joint.connectedAnchor = GripPoint;
                                    joint.enableCollision = false;

                                    NoFlip.Add(hObj.GetHashCode());

                                }
                            }

                            ++countedHumans;
                        }
                    }
                }
            }

            foreach (PhysicalBehaviour Selected in ItemsToFlip)
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Items flipped: " + countedItems + "   Humans flipped: " + countedHumans);
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("SortingLayer set for <b>" + countedItems + "</b> items to " + layerName);
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("SortingOrder set for <b>" + countedItems + "</b> items (delta: "+delta+")");
        }

        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: SET TO BACKGROUND
        //
        public void LayerBG(bool isBackground)
        {

            string msg = "background";
            if (!isBackground) msg = "foreground";

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Set Items to " + msg);

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                SelectedObject.gameObject.SetLayer(isBackground ? 2 : 9);
            }


        }



        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: QUICK SAVE
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("<b>Saved scene containing " + SelectedObjects.Count + "</b> items");
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Saved Scene: <b>"+ContraptionName+"</b>");
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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Restored Scene");

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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify("Loaded Scene: <b>" + ContraptionName + "</b>");

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

            if (Mulligan.verboseLevel >= 1) ModAPI.Notify(verboseMsg);
        }

        // - - - - - - - - - - - - - - - - - - -
        //
        //  FN: CHANGE POSE
        //
        public void ChangePose(int poseNum) {

            int peepsCount = 0;
            List<PersonBehaviour> PeopleAlreadyPosed = new List<PersonBehaviour>();

            //  Loop thru all peeps within a selection and change their PoseIndex
            //
            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                PersonBehaviour person = SelectedObject.gameObject.GetComponentInParent<PersonBehaviour>();

                if (person)
                {
                    if (PeopleAlreadyPosed.Contains(person)) continue;

                    ++peepsCount;

                    PeopleAlreadyPosed.Add(person);

                    //  If the person was already doing that pose, then have them stand still instead
                    if (person.OverridePoseIndex == poseNum) person.OverridePoseIndex = -1;
                    else person.OverridePoseIndex = poseNum;

                }
            }

            if (Mulligan.verboseLevel >= 2)
            {
                PoseState myPose = (PoseState)poseNum;
                ModAPI.Notify("[<color=red>" + myPose.ToString() + "</color>] toggled for " + peepsCount);
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


        
    }
}

