using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        Inventory Inv = (Inventory)target;
        if(GUILayout.Button("Recreate Inventory Sprites"))
        {
            //Delete all existing icons.
            InventoryItem[] AllItems;
            Dictionary<string, InventoryItem> Dictionary;
            Inventory.LoadDatabase(out AllItems, out Dictionary);
            //Clear file at path Resources/UI/ItemIcons
            string[] ExistingFiles = System.IO.Directory.GetFiles(Application.dataPath + "/Resources/Icons");
            Debug.Log("Deleting Pre-Existing Icons..");
            string Deleted = "";
            foreach (string file in ExistingFiles)
            {
                Deleted += file + "\n";
                System.IO.File.Delete(file);
            }
            Debug.Log("Deleted: " + Deleted);
            //Create all the icons.
            foreach (InventoryItem item in AllItems)
            {
                Debug.Log(item.name + "(" + item.ItemName + ")");
                GameObject obj = item.DisplayItem;
                GameObject RuntimeObj = Instantiate(obj, Inv.ItemPosition.position, Inv.ItemPosition.rotation);
                RuntimeObj.transform.localScale *= 2f;
                //transform.SetPositionAndRotation((Vector3.forward * .5f + Vector3.right * .75f + Vector3.up * 1.2f) * .25f, Quaternion.Euler(new Vector3(-15f, -45, 15f)));
                //transform.LookAt(Vector3.zero);
                //DestroyImmediate(ItemCamera.targetTexture);
                Inv.RendCam.targetTexture = new RenderTexture(Inv.IconSize, Inv.IconSize, 32); //We apparently need to increase the Bit-Depth in order to set the skybox to transparent.
                Texture2D snapshot = new Texture2D(Inv.IconSize, Inv.IconSize, TextureFormat.RGBA32, false);
                //How can we clear out the background?
                Inv.RendCam.Render();
                RenderTexture.active = Inv.RendCam.targetTexture;
                snapshot.ReadPixels(new Rect(0, 0, Inv.IconSize, Inv.IconSize), 0, 0);
                //Before we write the texture, look for all black pixels, and make them transperant.
                //A loop in a loop is a horrible idea, but let's do it.
                for (int x = 0; x < Inv.IconSize; x++)
                {
                    for (int y = 0; y < Inv.IconSize; y++)
                    {
                        Color pix = snapshot.GetPixel(x, y);
                        if(pix == Color.black)
                        {
                            snapshot.SetPixel(x, y, Color.clear);
                        }
                    }
                }
                snapshot.Apply();
                byte[] bytes = snapshot.EncodeToPNG();
                string filename = string.Format("{0}/Resources/Icons/" + item.name + ".png", Application.dataPath);
                System.IO.File.WriteAllBytes(filename, bytes);
                RenderTexture.active = null;
                Inv.RendCam.targetTexture = null;
                DestroyImmediate(RuntimeObj);
                DestroyImmediate(snapshot);
            }
            Debug.Log("Saved " + AllItems.Length + " Icons, These can be Found at: Resources/Icons");
        }
    }
}
