using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using Unity.Mathematics;
using UnityEngine;
public class ChunkCreator : MonoBehaviour
{
    Queue<Chunk> ToGenerate = new Queue<Chunk>();

    public int WorldSize, GenBatchSize;
    public float ChunkSize, SurfaceLevel;
    public Vector3Int WorldPos;
    public Vector3 Offset;
    public bool ShowGrid;
    public static ChunkCreator Singleton;
    public List<Chunk> Generating = new List<Chunk>();
    public Queue<Chunk> ToDispose = new Queue<Chunk>();
    public Dictionary<Vector3Int, Chunk> Chunks = new Dictionary<Vector3Int, Chunk>();
    public GameObject Prefab;
    public string TerrainPath;
    private void Start()
    {
        Singleton = this;
        //Check to see if we have a saved world we could load.
        if (TerrainPath != "")
        {
            if (File.Exists(GetPath()))
            {
                //Looks like we can load a world.
                //Load the data and set all savedchunks to be that of the saved world.
                WorldData data = LoadData(GetPath());
                Debug.Log("Loading World: " + TerrainPath + " Created " + data.dateTime.ToString());
                foreach (KeyValuePair<int3, DataChunk> Chunk in data.ChunksData)
                {
                    SavedChunks.Add(new Vector3Int(Chunk.Key.x, Chunk.Key.y, Chunk.Key.z), Chunk.Value);
                }
            }
        }
        InvokeRepeating(nameof(Tick), 0f, .2f);
        CheckUnload = Time.time + 2;
    }
    string GetPath()
    {
        return Application.dataPath + "/Resources/" + TerrainPath + ".wrldat";
    }
    public void Save()
    {
        string filename = string.Format("{0}/Resources/{1}" + ".wrldat", Application.dataPath, TerrainPath);
        WorldData Data = new WorldData(this);
        BinaryFormatter formatter = new BinaryFormatter();
        FileStream stream;
        if (File.Exists(filename))
        {
            File.Delete(filename);
            stream = new FileStream(filename, FileMode.Create);
        }
        else
        {
            stream = new FileStream(filename, FileMode.Create);
        }
        formatter.Serialize(stream, Data); //UnityEngine.Vector3 In Assemly is not marked as serialiseable.
        Debug.Log("Saved world setup at " + filename + " With " + Data.ChunksData.Count + " Data Chunks.");
    }
    WorldData LoadData(string Path)
    {
        FileStream stream = new FileStream(Path, FileMode.Open);
        BinaryFormatter formatter = new BinaryFormatter();
        return formatter.Deserialize(stream) as WorldData;
    }
    public void RemoveFromQeue(Chunk chunk)
    {
        List<Chunk> chunks = ToGenerate.ToList();
        if (!chunks.Contains(chunk)) return;
        for (int i = chunks.Count - 1; i != -1; i--)
        {
            if (chunks[i] == chunk) chunks.RemoveAt(i);
        }
    }
    Chunk GetChunk(Vector3Int key)
    {
        Chunks.TryGetValue(key, out Chunk value);
        return value;
    }
    public void ModifyChunks(Bounds Bounds, bool subtract)
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
                        if (chunk.awaitingGen) continue;
                        Bounds bounds = new Bounds(chunk.transform.position + new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f, new Vector3(ChunkSize, ChunkSize, ChunkSize));
                        if (Bounds.Intersects(bounds))
                        {
                            //This chunk needs to be updated.
                            chunk.Modifcation(Bounds, subtract);
                        }
                    }

                }
            }
        }
    }
    float CheckUnload;
    void OnCompleteGen(Chunk Chunk)
    {
        //Chunk does not exist in qeue, remove from generating list.
        Generating.Remove(Chunk);
        Chunk.OnChunkCompleted.RemoveListener(OnCompleteGen);
    }
    //try checking out the new 'noise' library in unity.Mathematics
    public float CalculateDensity(int3 Coordinate, Vector3 NoiseOffset, int Size)
    {
        //0 = Ground.
        //1 = Air.
        //Keep this in mind.
        Vector3 WorldPosition = CalculateWorldPos(Coordinate, NoiseOffset, Size);
        //Do a check to see if this is out of bounds.
        if (WorldPosition.y <= -20) return 0f;
        Vector3 NoiseCave = CalculatePointNoise(Coordinate, 25f, NoiseOffset, Size);
        Vector3 NoiseSurface = CalculatePointNoise(Coordinate, 15f, NoiseOffset, Size);
        float Interp = (WorldPosition.y - SurfaceLevel) + Perlin2Noise(NoiseSurface); //This gives us a value for the surface
        //Modify Interp to have a low value at the surface, dependent on the noiseTransition.
        float InterpClamped = Mathf.Clamp(Interp, -3.5f, 1f);
        return Mathf.LerpUnclamped(Perlin3Noise(NoiseCave) / InterpClamped, 1f / InterpClamped, InterpClamped);
    }
    float Perlin2Noise(Vector3 Point) //This creates the surface terrain, opposite of caves.
    {
        //value - Point.y
        //1 - 0 - 0.25
        //.75
        return Mathf.PerlinNoise(Point.x, Point.z); //Creates a flat plane somehow.
    }
    Vector3 CalculatePointNoise(int3 pos, float Scale, Vector3 NoiseOffset, int size)
    {
        return Singleton.Offset + NoiseOffset + new Vector3((float)pos.x / (float)size + 1 * Scale, (float)pos.y / (float)size + 1 * Scale, (float)pos.z / (float)size + 1 * Scale);
    }
    Vector3 CalculateWorldPos(int3 position, Vector3 NoiseOffset, int size)
    {
        return Singleton.Offset + NoiseOffset + new Vector3((float)position.x / size + 1, (float)position.y / size + 1, (float)position.z / size + 1);
    }
    float Perlin3Noise(Vector3 Point) //For generating caves.
    {
        float AB = Mathf.PerlinNoise(Point.x, Point.y);
        float BC = Mathf.PerlinNoise(Point.y, Point.z);
        float AC = Mathf.PerlinNoise(Point.x, Point.z);
        float BA = Mathf.PerlinNoise(Point.y, Point.x);
        float CB = Mathf.PerlinNoise(Point.z, Point.y);
        float CA = Mathf.PerlinNoise(Point.z, Point.x);
        return (AB + BC + AC + BA + CB + CA) / 6f;
    }
    void Tick()
    {
        //Recalculate Centre.
        Vector3 Raw = FirstPersonBody.Singleton.Body.transform.position;
        Raw /= ChunkSize;
        Vector3Int WPosition = new Vector3Int(Mathf.RoundToInt(Raw.x), Mathf.RoundToInt(Raw.y), Mathf.RoundToInt(Raw.z));
        if (WorldPos != WPosition) //That means we're gonna have to do checks
        {
            WorldPos = WPosition;
            for (int x = 0; x < WorldSize; x++)
            {
                for (int y = 0; y < WorldSize; y++)
                {
                    for (int z = 0; z < WorldSize; z++)
                    {
                        //Don't Bother creating chunks that are above the threshold (0).
                        Vector3Int Position = new Vector3Int(x, y, z) + WorldPos;
                        if (Position.y >= 3) continue;
                        if (!Chunks.ContainsKey(Position))
                        {
                            Vector3 Pos = ((Vector3)WorldPos * ChunkSize) + (new Vector3(x, y, z) * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                            GameObject obj = Instantiate(Prefab, Pos, Quaternion.identity, transform);
                            Chunk chunkRef = obj.GetComponent<Chunk>();
                            Chunks.Add(Position, obj.GetComponent<Chunk>());
                            chunkRef.NoiseOffset = Position; //Null?
                            chunkRef.Key = Position;
                            //Check to see if we have any data chunks on this position.
                            if (SavedChunks.ContainsKey(Position))//Any previously saved chunks here?
                            {
                                SavedChunks.TryGetValue(Position, out DataChunk chunk);
                                Debug.Log("Enqeued Chunk " + Position + ", " + chunk.Items.Count + " Items, " + chunk.Modifcations.Count + " Node changes.");
                                obj.GetComponent<Chunk>().SavedData = chunk;
                                SavedChunks.Remove(Position);
                            }
                            ToGenerate.Enqueue(chunkRef);
                        }
                    }
                }
            } //Instantiate new chunks to be later generated.
        }
        //Check to see if we can dispatch any more chunks to be generated
        while ((Generating.Count < GenBatchSize) && ToGenerate.Count != 0)
        {
            //Dispatch the chunk.
            if (ToGenerate.Peek() != null)
            {
                ToGenerate.Peek().OnChunkCompleted.AddListener(OnCompleteGen);
                ToGenerate.Peek().StartCoroutine(ToGenerate.Peek().Generate(true, true));
                Generating.Add(ToGenerate.Peek());
            }
            ToGenerate.Dequeue();
        }
        if (CheckUnload <= Time.time) //We only check for chunks to unload every two seconds.
        {
            Vector3 Pos = ((Vector3)WorldPos * ChunkSize);
            Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize) * WorldSize;
            Bounds bounds = new Bounds(Pos, Size);
            foreach (KeyValuePair<Vector3Int, Chunk> pair in Chunks) //runs for all 3,375+ chunks in existance, and we're only checking for chunks that are out of range. 
            {
                //Check to see if the CENTRE of this chunk is within the boundary.
                if (!bounds.Contains(pair.Value.transform.position + new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f))
                {
                    //If this chunk is not contained within the world boundary, we need to delete it.
                    ToDispose.Enqueue(pair.Value); //Mark it for cleanup.
                }
            }
            CheckUnload = Time.time + 2f;
        }
        int MaxIterations = GenBatchSize * 5;
        while (ToDispose.Count != 0 && MaxIterations > 0) //We will only clean up 4 chunks every .2 secs.
        {
            RemoveFromQeue(ToDispose.Peek()); //Remove this from the generation qeue.
            //Quick check, see if we are trying to generate this chunk.
            if (Generating.Contains(ToDispose.Peek())) Generating.Remove(ToDispose.Peek()); //This still doesn't appear to work.

            while (ToDispose.Peek() == null && ToDispose.Count != 0) //Since we've been having this bug, look through the ToDispose qeue for a valid chunk.
            {
                ToDispose.Dequeue();
            }
            if (ToDispose.Count == 0) return; //for if we cleared out all of the chunks to be disposed.
            //TODO: Fix this so it creates a datachunk based on the chunk's current state, not it's original state + its current state.
            DataChunk chunk = new DataChunk(ToDispose.Peek().Key);
            Bounds ChunkBounds = new Bounds(ToDispose.Peek().transform.position + new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f, new Vector3(ChunkSize, ChunkSize, ChunkSize)); //This yells at me because the chunk has supposedly already been destroyed, which is impossible, for if it was, it would have been deqeued already.
            //No need to check for the chunk's original saved data, since that's outdated.

            //What we can do, is iterate through all the active items, and check to see which items are within the chunk's bounding box.

            //Then save any items that are within it, and also save any modifcations that the chunk has.
            for (int i = ActiveItems.Count - 1; i != -1; i--)
            {
                if (ChunkBounds.Contains(ActiveItems[i].transform.position))
                {
                    if (!chunk.Exists) chunk.Exists = true;
                    //Add this item.
                    chunk.Items.Add(new LODItem.LodItemData(ActiveItems[i]));
                    //Destroy this item, since it's chunk is about to be 'disposed'
                    Destroy(ActiveItems[i].gameObject);
                    ActiveItems.RemoveAt(i);
                }
            }

            if (ToDispose.Peek().Modified != null) //Has been modified, save it.
            {
                //Remember to initialise the modifcations array.
                chunk.Modifcations = new Dictionary<int3, float>();
                foreach (KeyValuePair<int3, float> val in ToDispose.Peek().Modified)
                {
                    chunk.Modifcations.Add(val.Key, val.Value); //NullReferenceException.
                }
                if (!chunk.Exists) chunk.Exists = true;
            }

            if (chunk.Exists)
            {
                //This chunk exists, save it.
                SavedChunks.Add(ToDispose.Peek().Key, chunk);
            }

            //We're having a bug where: 
            //A element will be destroyed, but not be deqeued.
            //I have literally no idea why this is happening.
            Chunks.Remove(ToDispose.Peek().Key); //Remove from chunks.
            Destroy(ToDispose.Peek().gameObject);
            ToDispose.Dequeue();
            MaxIterations--;
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
        if (Generating.Count != 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < Generating.Count; i++)
            {
                Vector3 Pos = (new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f) + ((Vector3)Generating[i].Key * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize);
                Gizmos.DrawWireCube(Pos, Size);
            }
        }
        if (ToDispose.Count != 0)
        {
            Gizmos.color = Color.red;
            foreach (Chunk chunk in ToDispose)
            {
                Vector3 Pos = (new Vector3(ChunkSize, ChunkSize, ChunkSize) * .5f) + ((Vector3)chunk.Key * ChunkSize) - (new Vector3(WorldSize * ChunkSize, WorldSize * ChunkSize, WorldSize * ChunkSize) * .5f);
                Vector3 Size = new Vector3(ChunkSize, ChunkSize, ChunkSize);
                Gizmos.DrawWireCube(Pos, Size);
            }
        }
        if (SavedChunks.Count != 0)
        {
            Gizmos.color = Color.green;
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
        public int3 ChunkCoord;
        public Dictionary<int3, float> Modifcations;
        public DataChunk(int3 coordinate)
        {
            Items = new List<LODItem.LodItemData>();
            Exists = false;
            ChunkCoord = coordinate;
            Modifcations = null;
        }
        public DataChunk(Vector3Int coordinate)
        {
            Items = new List<LODItem.LodItemData>();
            Exists = false;
            ChunkCoord = new int3(coordinate.x, coordinate.y, coordinate.z);
            Modifcations = null;
        }
        public void Clear()
        {
            Exists = false;
            Items.Clear();
            Items.TrimExcess();
            ChunkCoord = int3.zero;
        }
        //Also add a dictionary for all the nodes that were modified.
    }
    [System.Serializable]
    public class WorldData
    {
        //Chunk modifcations
        //LODItems
        //Out of range chunks.
        //Player Position + rotation
        //+ Player inventory
        public System.DateTime dateTime;
        public Dictionary<int3, DataChunk> ChunksData;
        public WorldData(ChunkCreator creator)
        {
            //Go through all chunks that WERE saved, and chunks that exist currently.
            dateTime = System.DateTime.Now;
            ChunksData = new Dictionary<int3, DataChunk>();
            //Iterate through all existing chunks.
            foreach (KeyValuePair<Vector3Int, Chunk> KeyValuePair in creator.Chunks)
            {
                DataChunk chunk = new DataChunk(KeyValuePair.Key);
                Bounds ChunkBounds = new Bounds(KeyValuePair.Value.transform.position + new Vector3(creator.ChunkSize, creator.ChunkSize, creator.ChunkSize) * .5f, new Vector3(creator.ChunkSize, creator.ChunkSize, creator.ChunkSize));
                for (int i = creator.ActiveItems.Count - 1; i != -1; i--)
                {
                    if (ChunkBounds.Contains(creator.ActiveItems[i].transform.position))
                    {
                        if (!chunk.Exists) chunk.Exists = true;
                        //Add this item.
                        chunk.Items.Add(new LODItem.LodItemData(creator.ActiveItems[i]));
                        //Destroy this item, since it's chunk is about to be 'disposed'
                    }
                }

                if (KeyValuePair.Value.Modified != null) //Has been modified, save it.
                {
                    //Remember to initialise the modifcations array.
                    chunk.Modifcations = new Dictionary<int3, float>();
                    foreach (KeyValuePair<int3, float> val in KeyValuePair.Value.Modified)
                    {
                        chunk.Modifcations.Add(val.Key, val.Value); //NullReferenceException.
                    }
                    if (!chunk.Exists) chunk.Exists = true;
                }

                if (chunk.Exists)
                {
                    //This chunk exists, save it.
                    int3 Coordinate = new int3(KeyValuePair.Key.x, KeyValuePair.Key.y, KeyValuePair.Key.z);
                    ChunksData.Add(Coordinate, chunk);
                }
            }
            //That should eliminate all active items, now we also need to include the chunks that are out of range.
            foreach (KeyValuePair<Vector3Int, DataChunk> data in creator.SavedChunks)
            {
                int3 coord = new int3(data.Key.x, data.Key.y, data.Key.z);
                ChunksData.Add(coord, data.Value);
            }
        }
    }
    #endregion
}