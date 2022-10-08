using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODItem : MonoBehaviour
{
    //Functionality is handled by the ChunkCreator.
    private void Start()
    {
        //When we are created (usually we are within the vision distance when this happens too)
        ChunkCreator.Singleton.ActiveItems.Add(this);
    }
    private void OnDestroy()
    {
        ChunkCreator.Singleton.ActiveItems.Remove(this);
    }
    [System.Serializable]
    public struct LodItemData
    {
        public ChunkCreator.Float3 Position;
        public ChunkCreator.Float3 Euler;
        public string ItemId;
        public LodItemData(LODItem item)
        {
            Position =  new ChunkCreator.Float3(item.transform.position);
            Euler = new ChunkCreator.Float3(item.transform.eulerAngles);
            ItemId = LODItemDatabase.Singleton.GetIDFromObj(item.gameObject);
        }
    }
}
