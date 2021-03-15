using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using Newtonsoft.Json;


namespace DebugX
{
    public class DebugX
    {

        public static KeyCode dumpObjects = KeyCode.Keypad5;

        public static void Main()
        {
            Directory.CreateDirectory("dumps");

            ModAPI.Register<DebugXBehaviour>();

        }

    }


    public class DebugXBehaviour : MonoBehaviour
    {

        public void Update()
        {

            if (Input.GetKeyUp(DebugX.dumpObjects)) DumpSelectedObjects();

        }


        public static void DumpSelectedObjects()
        {

            foreach (PhysicalBehaviour SelectedObject in SelectionController.Main.SelectedObjects)
            {
                
                var dump        = ObjectDumper.Dump(SelectedObject);
                string dumpName = SelectedObject.name;

                File.AppendAllText("dumps/" + dumpName + ".txt", dump);

            }

        }


    }

}
