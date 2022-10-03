using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LODItemDatabase : MonoBehaviour
{
    public static LODItemDatabase Singleton;
    public Dictionary<string, GameObject> PrefabDatabase;
    public void Start()
    {
        Singleton = this;
        GameObject[] prefabs = Resources.LoadAll<GameObject>("Items");
        GameObject[] env = Resources.LoadAll<GameObject>("EnvContent");
        PrefabDatabase = new Dictionary<string, GameObject>();
        for (int i = 0; i < prefabs.Length; i++)
        {
            PrefabDatabase.Add(prefabs[i].name, prefabs[i]);
        }
        for (int i = 0; i < env.Length; i++)
        {
            PrefabDatabase.Add(env[i].name, env[i]);
        }
    }
    public GameObject GetPrefabFromID(string id)
    {
        //Debug.Log(id);
        if (id == null) return null;
        PrefabDatabase.TryGetValue(id, out GameObject value);
        return value;
    }
    public string GetIDFromObj(GameObject obj)
    {
        Debug.Log(obj.name);
        foreach(KeyValuePair<string, GameObject> pair in PrefabDatabase)
        {
            if(pair.Value.gameObject.name + "(Clone)" == obj.name)
            {
                return pair.Key;
            }
        }
        return null;
    }
}
