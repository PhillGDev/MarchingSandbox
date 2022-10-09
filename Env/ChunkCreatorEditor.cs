using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(ChunkCreator))]
public class ChunkCreatorEditor : Editor
{
    //Saving, provide a save file name too.
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        ChunkCreator Creator = (ChunkCreator)target;
        if (Creator.TerrainPath != "")
        {
            if (GUILayout.Button("Save World."))
            {
                Creator.Save();
            }
        }
    }
}
