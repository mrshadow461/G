using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

namespace TheOtherRoles{
    
    public class JackInTheBox {
        public static System.Collections.Generic.List<JackInTheBox> AllJackInTheBoxes = new System.Collections.Generic.List<JackInTheBox>();
        public static int JackInTheBoxLimit = 3;
        private static Sprite jackInTheBoxSprite;
        public static bool boxesConvertedToVents = false;

        public static Sprite getJackInTheBoxSprite() {
            if (jackInTheBoxSprite) return jackInTheBoxSprite;
            jackInTheBoxSprite = Helpers.loadSpriteFromResources("TheOtherRoles.Resources.JackInTheBox.png", 300f);
            return jackInTheBoxSprite;
        }

        private GameObject gameObject;
        private Vent vent;

        public JackInTheBox(Vector2 p) {
            gameObject = new GameObject("JackInTheBox");
            var referenceVent = UnityEngine.Object.FindObjectOfType<Vent>(); 
            Vector3 position = new Vector3(p.x, p.y, referenceVent.transform.position.z);
            gameObject.transform.position = position;

            if (PlayerControl.LocalPlayer == Trickster.trickster) {
                // Only render the box for the Jack-In-The-Box player
                var boxRenderer = gameObject.AddComponent<SpriteRenderer>();
                boxRenderer.sprite = getJackInTheBoxSprite();
            }

            TheOtherRolesPlugin.Instance.Log.LogInfo("Createvent ");
            vent = UnityEngine.Object.Instantiate<Vent>(referenceVent, referenceVent.transform.parent);
            vent.transform.position = gameObject.transform.position;
            vent.Left = null;
            vent.Right = null;
            vent.Center = null;
            vent.EnterVentAnim = null;
            vent.ExitVentAnim = null;
            vent.Id = ShipStatus.Instance.GIDPCPOEFBC.Select(x => x.Id).Max() + 1; // Make sure we have a unique id
            TheOtherRolesPlugin.Instance.Log.LogInfo("Replace vent Sprite");
            var ventRenderer = vent.GetComponent<SpriteRenderer>();
            ventRenderer.sprite = getJackInTheBoxSprite();
            vent.LNMJKMLHMIM = ventRenderer;
            vent.gameObject.SetActive(false);

            TheOtherRolesPlugin.Instance.Log.LogInfo("Add created vent to AllVents");
            var allVentsList = ShipStatus.Instance.GIDPCPOEFBC.ToList();
            allVentsList.Add(vent);
            ShipStatus.Instance.GIDPCPOEFBC = allVentsList.ToArray();

            gameObject.SetActive(true);

            AllJackInTheBoxes.Add(this);
        }

        public void convertToVent() {
            gameObject.SetActive(false);
            vent.gameObject.SetActive(true);
            return;
        }

        public static void convertToVents() {
            foreach(var box in AllJackInTheBoxes) {
                box.convertToVent();
            }
            connectVents();
            boxesConvertedToVents = true;
            return;
        }

        public static bool hasJackInTheBoxLimitReached() {
            return (AllJackInTheBoxes.Count >= JackInTheBoxLimit);
        }

        private static void connectVents() {
            for(var i = 0;i < AllJackInTheBoxes.Count - 1;i++) {
                var a = AllJackInTheBoxes[i];
                var b = AllJackInTheBoxes[i + 1];
                a.vent.Right = b.vent;
                b.vent.Left = a.vent;
            }
            // Connect first with last
            AllJackInTheBoxes.First().vent.Left = AllJackInTheBoxes.Last().vent;
            AllJackInTheBoxes.Last().vent.Right = AllJackInTheBoxes.First().vent;
        }

        public static void clearJackInTheBoxes() {
            boxesConvertedToVents = false;
            AllJackInTheBoxes = new List<JackInTheBox>();
        }
        
    }

}