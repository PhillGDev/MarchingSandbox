using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
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
        public float3 Position;
        public float3 Euler;
        public string ItemId;
        public LodItemData(LODItem item)
        {
            Position = new float3(item.transform.position);
            Euler = new float3(item.transform.eulerAngles);
            ItemId = LODItemDatabase.Singleton.GetIDFromObj(item.gameObject);
        }
    }
}
