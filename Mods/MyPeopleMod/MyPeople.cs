using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace MyPeople
{
    public class MyPeople
    {

        private const string CategoryName = "MyPeople";

        public static void Main()
        {
            //  Use AZULE's CategoryBuilder to add in a custom section for our custom people
            //
            CategoryBuilder.Create(CategoryName, "",ModAPI.LoadSprite("category_thumb.png"));


            //
            //  Call function to handle initializing our new people
            //
            //  Instead of repeating lines of code for each person, 
            //  you could create a function to do this.  In this example, we simply need to 
            //  pass the function the filename of the persons skin png (without extension)
            //
            //  I added a 3rd parameter to allow me to define any additional clothes
            //  the person is wearing so it can automatically initialize, spawn and attach those to the person.
            //  
            //  it allows me to include any amount of clothes I want, separated by a comma.
            //  And then leading each piece of clothing we put the body part it attaches to before the colon.
            //
            //  If the clothes overlap, list them in order so the last clothes listed are on top
            //  
            //  This will add 5 new people to the game.  2 Of them will be duplicated but with different clothing options.
            //
            AddMyPerson("tubbs");
            AddMyPerson("skoal");
            AddMyPerson("skoal", "skoal_injured",   "Head:bandanage" );
            AddMyPerson("tucker");
            AddMyPerson("tucker", "tucker_rocker",   "Head:rocker_hair,Head:bandana" );


        }

        public static void AddMyPerson(string png, string personsName="", string clothes=null)
        {

            if (personsName == "") personsName = png;

            ModAPI.Register(
                new Modification()
                {
                    OriginalItem        = ModAPI.FindSpawnable("Human"),
                    NameOverride        = personsName,
                    DescriptionOverride = "My Person " + personsName,
                    CategoryOverride    = ModAPI.FindCategory(CategoryName),
                    ThumbnailOverride   = ModAPI.LoadSprite("people/" + personsName + "_thumb.png"),
                    AfterSpawn          = (Instance) =>
                    {
                        var skin  = ModAPI.LoadTexture("people/" + png + "_skin.png");
                        var flesh = ModAPI.LoadTexture("people/common_flesh.png");
                        var bone  = ModAPI.LoadTexture("people/common_bone.png");

                        var person = Instance.GetComponent<PersonBehaviour>();


                        if (clothes != null) 
                        {
                            int itemNumber = 0;

                            string[] clothesList = clothes.Split(',');

                            foreach (string clothing in clothesList) 
                            {

                                itemNumber++;

                                string[] pieces    = clothing.Split(':');
                                var bodyPart       = Instance.transform.Find(pieces[0]);
                                
                                var clothingObject                     = new GameObject("milk " + pieces[1]);
                                clothingObject.transform.SetParent(bodyPart);
                                clothingObject.transform.localPosition = new Vector3(-0f, 0f);
                                clothingObject.transform.rotation      = Quaternion.Euler(0f, 0f, 0f);
                                
                                var clothingSprite              = clothingObject.AddComponent<SpriteRenderer>();
                                clothingSprite.sprite           = ModAPI.LoadSprite("people/" + pieces[1] + ".png");
                                clothingSprite.sortingLayerName = "Top";
                                
                                foreach (var body in person.Limbs)
                                {
                                    body.transform.root.localScale *= 0.997f;
                                    body.transform.localScale = new Vector3(0.9f, 1f);
                                }
                                
                                clothingObject.transform.localScale = new Vector3(1f, 1f);

                                //  This is how we are defining which piece of clothing to show above another
                                clothingSprite.sortingOrder += itemNumber;

                            }
                        }

                    person.SetBodyTextures(skin, flesh, bone, 1);
                    
                    }
                }
            );
        }

    }

}