
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
public class ChunkCreator : MonoBehaviour
{
    public Queue<Chunk> ToGenerate = new Queue<Chunk>();

    public int WorldSize;
    public float ChunkSize, SurfaceLevel;
    public Vector3Int WorldPos;
    public Vector3 Offset;
    public bool ShowGrid;
    public static ChunkCreator Singleton;
    public List<Chunk> ToCleanup = new List<Chunk>();
    public Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();
    public GameObject Prefab;
    private void Start()
    {
        Singleton = this;
        InvokeRepeating(nameof(Tick), 0f, .2f);
        CheckUnload = Time.time + 2;
    }
    Chunk GetChunk(Vector3Int key)
    {
        Chunks.TryGetValue(key, out Chunk value);
        return value;
    }
    public void ModifyChunks(Vector3 Point, float Radius, bool subtract)
    {
        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                for (int z = 0; z < WorldSize; z++)
                {
                    Chunk chunk = GetChunk(new Vector3Int(x, y, z) + WorldPos);
                    if (chunk != null)
                    {
                        Bounds bounds = new Bounds(chunk.transform.position + new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f, new Vector3(ChunkSize, ChunkSize, ChunkSize));
                        if (bounds.Contains(Point))
                        {
                            chunk.Modifcation(Point, Radius, subtract);
                        }
                        float MinDist = Vector3.Distance(Point, bounds.ClosestPoint(Point));
                        float val = MinDist - Radius;
                        if (0 > val)
                        {
                            //This chunk was effected.
                            chunk.Modifcation(Point, Radius, subtract);
                        }
                    }

                }
            }
        }
    }
    float CheckUnload;
    void Tick()
    {
        if (ToGenerate.Count != 0)
        {
            while (ToGenerate.Peek() == null && ToGenerate.Count != 0)
            {
                Debug.Log("Removed a null chunk from generation qeue.");
                ToGenerate.Dequeue();
            }
        }
        //Recalculate Centre.
        Vector3 Raw = FirstPersonBody.Singleton.Body.transform.position;
        Raw /= ChunkSize;
        List<Chunk> KeepChunks = new List<Chunk>();
        WorldPos = new Vector3Int(Mathf.RoundToInt(Raw.x), Mathf.RoundToInt(Raw.y), Mathf.RoundToInt(Raw.z));
        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                for (int z = 0; z < WorldSize; z++)
                {
                    //Don't Bother creating chunks that are above the threshold (0).
                    Vector3Int Position = new Vector3Int(x, y, z) + WorldPos;
                    if (!Chunks.ContainsKey(Position))
                    {
                        Vector3 Pos = ((Vector3)WorldPos * ChunkSize) + (new Vector3(x, y, z) * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                        GameObject obj = Instantiate(Prefab, Pos, Quaternion.identity, transform);
                        Chunk chunkRef = obj.GetComponent<Chunk>();
                        Chunks.Add(Position, obj.GetComponent<Chunk>());
                        chunkRef.NoiseOffset = Position; //Null?
                        chunkRef.Key = Position;
                        //Check to see if we have any data chunks on this position.
                        if (SavedChunks.ContainsKey(Position))//There's an issue here somewhere.
                        {
                            SavedChunks.TryGetValue(Position, out DataChunk chunk);
                            Debug.Log("Enqeued Chunk " + Position + ", " + chunk.Items.Count + " Items.");
                            obj.GetComponent<Chunk>().SavedData = chunk;
                            SavedChunks.Remove(Position);
                        }
                        KeepChunks.Add(chunkRef); //Also include chunks we just made in the KeepChunks list, so chunks that just got made (and had objects in them) don't get destroyed instantly.
                    }
                    else
                    {
                        if (CheckUnload <= Time.time) KeepChunks.Add(GetChunk(Position));
                    }
                }
            }
        }
        if (CheckUnload <= Time.time)
        {
            foreach (KeyValuePair<Vector3Int, Chunk> pair in Chunks)
            {
                if (!KeepChunks.Contains(pair.Value))
                {
                    pair.Value.MarkForCleanup();
                    //LOD:
                    //Find what items are within this chunk
                    //Store those items.
                    DataChunk chunk = new DataChunk(pair.Key);
                    Bounds bounds = new Bounds(pair.Value.transform.position + new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f, new Vector3(ChunkSize, ChunkSize, ChunkSize));
                    if (ActiveItems.Count != 0)
                    {
                        for (int i = ActiveItems.Count - 1; i != -1; i--)
                        {
                            if (bounds.Contains(ActiveItems[i].transform.position))
                            {
                                //This chunk contains this item.
                                //So we should create a data chunk.
                                if (!chunk.Exists)
                                {
                                    chunk.Exists = true;
                                }
                                chunk.Items.Add(new LODItem.LodItemData(ActiveItems[i]));
                                Destroy(ActiveItems[i].gameObject);
                                ActiveItems.RemoveAt(i);
                            }
                        }
                    }
                    if (chunk.Exists)
                    {
                        //This chunk exists, save it.
                        SavedChunks.Add(pair.Key, chunk);
                    }
                }
            }
            if (ToCleanup.Count != 0)
            {
                for (int i = 0; i < ToCleanup.Count; i++)
                {
                    if (ToCleanup[i].SavedData.Exists)
                    {
                        if(!SavedChunks.ContainsKey(ToCleanup[i].Key))
                        { SavedChunks.Add(ToCleanup[i].Key, ToCleanup[i].SavedData); }
                    }
                    Chunks.Remove(ToCleanup[i].Key);
                    Destroy(ToCleanup[i].gameObject); 
                }
                Debug.Log("Cleaned up " + ToCleanup.Count + " Out of range chunks.");
                ToCleanup.Clear();
            }
            CheckUnload = Time.time + 2;
        }
    }
    [System.Serializable]
    public struct Float3
    {
        public float x, y, z;
        public Float3(Vector3 input)
        {
            x = input.x;
            y = input.y;
            z = input.z;
        }
        public void MakeVec(Float3 input, out Vector3 output)
        {
            output = new Vector3(input.x, input.y, input.z);
        }
    }
    [System.Serializable]
    public struct Key3D
    {
        public int x, y, z;
        public Key3D(Vector3Int vector3)
        {
            x = vector3.x;
            y = vector3.y;
            z = vector3.z;
        }
        public void makeVec(Key3D input, out Vector3Int output)
        {
            output = new Vector3Int(input.x, input.y, input.z);
        }
    }
    [System.Serializable]
    public class ChunkWorld
    {
        public Dictionary<Key3D, DataChunk> Chunks;
        public ChunkWorld(ChunkCreator world)
        {
            Chunks = new Dictionary<Key3D, DataChunk>();
            foreach (KeyValuePair<Vector3Int, Chunk> pair in world.Chunks) //This only accounts for active chunks
            {
                //LOD:
                //Find what items are within this chunk
                //Store those items.
                DataChunk chunk = new DataChunk(pair.Key);
                Bounds bounds = new Bounds(pair.Value.transform.position + new Vector3(world.ChunkSize, world.ChunkSize, world.ChunkSize) * .5f, new Vector3(world.ChunkSize, world.ChunkSize, world.ChunkSize));
                if (world.ActiveItems.Count != 0)
                {
                    for (int i = world.ActiveItems.Count - 1; i != -1; i--)
                    {
                        if (bounds.Contains(world.ActiveItems[i].transform.position))
                        {
                            //This chunk contains this item.
                            //So we should create a data chunk.
                            if (!chunk.Exists)
                            {
                                chunk.Exists = true;
                            }
                            chunk.Items.Add(new LODItem.LodItemData(world.ActiveItems[i]));
                        }
                    }
                }
                if (chunk.Exists)
                {
                    //This chunk exists, save it.
                    Chunks.Add(new Key3D(pair.Key), chunk);
                }

            }
            foreach (KeyValuePair<Vector3Int, DataChunk> pair in world.SavedChunks)
            {
                //This chunk exists, save it.
                Chunks.Add(new Key3D(pair.Key), pair.Value);
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (Chunks == null) return;
        Gizmos.color = Color.blue;
        if (ShowGrid)
        {
            for (int x = 0; x < WorldSize; x++)
            {
                for (int y = 0; y < WorldSize; y++)
                {
                    for (int z = 0; z < WorldSize; z++)
                    {
                        Vector3 Pos = ((Vector3)WorldPos * ChunkSize) + (new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f) + (new Vector3(x, y, z) * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                        Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize);
                        Gizmos.DrawWireCube(Pos, Size);
                    }
                }
            }
        }
        else
        {
            Vector3 Pos = ((Vector3)WorldPos * ChunkSize);
            Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize) * WorldSize;
            Gizmos.DrawWireCube(Pos, Size);
        }
        if (SavedChunks.Count != 0)
        {
            Gizmos.color = Color.red;
            foreach (KeyValuePair<Vector3Int, DataChunk> keyValuePair in SavedChunks)
            {
                Vector3 Pos = (new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f) + ((Vector3)keyValuePair.Key * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize);
                Gizmos.DrawWireCube(Pos, Size);
            }
        }
        if (ActiveItems.Count != 0)
        {
            Gizmos.color = Color.blue;
            foreach (LODItem item in ActiveItems)
            {
                Gizmos.DrawCube(item.transform.position, Vector3.one);
            }
        }
    }
    #region LOD
    public List<LODItem> ActiveItems;
    public Dictionary<Vector3Int, DataChunk> SavedChunks = new Dictionary<Vector3Int, DataChunk>();
    [System.Serializable]
    public struct DataChunk
    {
        public bool Exists;
        //A chunk that has items within it.
        public List<LODItem.LodItemData> Items;
        public Key3D ChunkCoord;
        public DataChunk(Key3D coordinate)
        {
            Items = new List<LODItem.LodItemData>();
            Exists = false;
            ChunkCoord = coordinate;
        }
        public DataChunk(Vector3Int coordinate)
        {
            Items = new List<LODItem.LodItemData>();
            Exists = false;
            ChunkCoord = new Key3D(coordinate);
        }
    }
    #endregion
}