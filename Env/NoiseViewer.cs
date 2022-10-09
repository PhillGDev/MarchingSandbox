using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class NoiseViewer : MonoBehaviour
{
    public int Resolution; //16 to match the chunks.
    public MeshRenderer Renderer;
    public Vector3 NoiseOffset;
    private void Start()
    {
        Renderer = GetComponent<MeshRenderer>();
        Texture tex = new Texture2D(Resolution, Resolution);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Point;
        Renderer.material.mainTexture = new Texture2D(Resolution, Resolution);
    }
    private void Update()
    {
        Texture2D tex = Renderer.material.mainTexture as Texture2D;
        int Height = Mathf.RoundToInt(transform.position.y);
        for (int x = 0; x < Resolution; x++)
        {
            for (int y = 0; y < Resolution; y++)
            {
                //Scale for current = 25.
                int3 Coord = new int3(x, 0, y);
                float val = ChunkCreator.Singleton.CalculateDensity(Coord, NoiseOffset, Resolution);
                tex.SetPixel(x, y, new Color(val, val, val));
            }
        }
        tex.Apply();
        Renderer.material.mainTexture = tex;
    }
}
