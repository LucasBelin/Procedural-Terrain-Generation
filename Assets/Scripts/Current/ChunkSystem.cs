using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChunkSystem : MonoBehaviour
{
    public GameObject cube;
    public GameObject quad;
    public Transform player;

    public int chunkSize = 16;
    [Min(1)]
    public int renderDistance = 8;
    public int baseHeight = 32;
    public int heightMultiplier = 32;
    public enum GenerationMode { Cube, Quad };
    public GenerationMode mode;
    private Vector3 scale;

    private int seed = 0;
    [Range(0, 1)]
    public float minNoise = 0f;
    [Range(0, 1)]
    public float maxNoise = 1f;
    public float frequency = 3f;
    [Range(1, 8)]
    public int octaves = 1;
    [Range(0.01f, 20f)]
    public float exp = 1;
    [Range(1, 10)]
    public int pow = 2;

    private List<ChunkData> chunksData;
    private List<GameObject> chunks;
    private List<GameObject> chunksActive;

    public Color dirt;
    public Color grass;
    public Color stone;
    public Color snow;

    private void Start()
    {
        scale = mode == GenerationMode.Cube ? cube.transform.localScale : quad.transform.localScale;
        chunksData = new List<ChunkData>();
        chunks = new List<GameObject>();
        chunksActive = new List<GameObject>();
        seed = Random.Range(-99999, 99999);
    }

    private void Update()
    {
        UpdateChunksActive();
    }

    public GameObject CreateBlock(Transform chunk, Vector3 position, Color color, List<int> faceIDs)
    {
        GameObject block = new GameObject("Block");
        block.transform.SetParent(chunk);
        block.transform.position = position;

        foreach (int id in faceIDs)
        {
            KeyValuePair<Vector3, Vector3> faceOffsetAndRotation = GetFaceValuesFromID(id);
            Vector3 faceOffset = faceOffsetAndRotation.Key;
            Vector3 faceRotation = faceOffsetAndRotation.Value;

            Vector3 halfScale = scale * 0.5f;
            Vector3 facePosition = position + faceOffset * scale.x + halfScale;
            GameObject face = Instantiate(quad, facePosition, Quaternion.Euler(faceRotation), block.transform);
            face.GetComponent<MeshRenderer>().material.color = color;
        }

        return block;
    }

    public KeyValuePair<Vector3, Vector3> GetFaceValuesFromID(int id)
    {
        switch(id)
        {
            //Top:0, Bottom:1, Front:2, Back:3, Left:4, Right:5
            case 0:
                return new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.5f, 0.5f), new Vector3(90, 0, 0));
            case 1:
                return new KeyValuePair<Vector3, Vector3>(new Vector3(0, -0.5f, 0.5f), new Vector3(-90, 0, 0));
            case 2:
                return new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);
            case 3:
                return new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0, 1f), new Vector3(-180, 0, 0));
            case 4:
                return new KeyValuePair<Vector3, Vector3>(new Vector3(-0.5f, 0, 0.5f), new Vector3(0, 90, 0));
            case 5:
                return new KeyValuePair<Vector3, Vector3>(new Vector3(0.5f, 0, 0.5f), new Vector3(0, -90, 0));
            default: 
                return new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero);
        }
    }

    GameObject GenerateChunk(ChunkData data)
    {
        string chunkName = "Chunk " + data.origin.x + "," + data.origin.z;
        GameObject chunk = new GameObject(chunkName);
        chunk.transform.SetParent(transform);
        chunk.transform.position = data.origin;

        foreach(Vector3 pos in data.heightMap)
        {
            if(mode == GenerationMode.Cube)
            {
                //Only top most layer
                GameObject block = Instantiate(cube, pos, Quaternion.identity, chunk.transform);
                block.GetComponent<MeshRenderer>().material.color = GetColor((int)pos.y);
                //Fill down to 0
                /*for (int i = 0; i <= pos.y; i++)
                {
                    Vector3 blockPos = new Vector3(pos.x, pos.y - i * cube.transform.localScale.y, pos.z);
                    if (blockPos.y < 0) break;
                    GameObject block = Instantiate(cube, blockPos, Quaternion.identity, chunk.transform);
                    block.GetComponent<MeshRenderer>().material.color = GetColor((int)blockPos.y);
                }*/
            }
            else if(mode == GenerationMode.Quad)
            {
                //TODO determine which faces to render
                CreateBlock(chunk.transform, pos, GetColor((int)pos.y), new List<int> { 0 });
            }
        }
        chunks.Add(chunk);
        chunksActive.Add(chunk);
        return chunk;
    }

    ChunkData GenerateChunkData(Vector3 chunkPosition)
    {
        Vector3[] heights = new Vector3[chunkSize * chunkSize];

        for (int x = 0, i = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++, i++)
            {
                int posX = (int)(chunkPosition.x + x * scale.x);
                int posZ = (int)(chunkPosition.z + z * scale.z);
                int height = Round(LinearConversion(GetPerlinNoise(posX, posZ), 0, 1, 0, heightMultiplier), (int)scale.y);
                heights[i] = new Vector3(posX, height + baseHeight, posZ);
            }
        }
        ChunkData data = new ChunkData(chunkPosition, chunkSize, heights);
        chunksData.Add(data);
        return data;
    }

    List<Vector3> GenerateChunkPositionsAroundPlayer()
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 playerPos = new Vector3(
            Round(player.transform.position.x, (int)(chunkSize * scale.x)),
            0,
            Round(player.transform.position.z, (int)(chunkSize * scale.z)));

        for (int z = -renderDistance; z < renderDistance; z++)
        {
            for (int x = -renderDistance; x < renderDistance; x++)
            {
                int posX = (int)(playerPos.x + x * chunkSize * scale.x);
                int posZ = (int)(playerPos.z + z * chunkSize * scale.z);
                positions.Add(new Vector3(posX, 0, posZ));
            }
        }
        return positions;
    }

    GameObject FindOrGenerateChunk(Vector3 chunkPosition)
    {
        foreach (GameObject chunk in chunks)
        {
            if (chunk.transform.position == chunkPosition)
                return chunk;
        }
        return GenerateChunk(GenerateChunkData(chunkPosition));
    }

    //TODO Only show chunks in the fov
    void UpdateChunksActive()
    {
        scale = mode == GenerationMode.Cube ? cube.transform.localScale : quad.transform.localScale;
        List<Vector3> shouldBeActive = GenerateChunkPositionsAroundPlayer();
        List<GameObject> toRemove = new List<GameObject>();

        //Remove chunks outside of the render distance from the list and set inactive
        for(int i = chunksActive.Count - 1; i >= 0; i--)
        {
            if(!shouldBeActive.Contains(chunksActive[i].transform.position))
            {
                chunksActive[i].SetActive(false);
                chunksActive.RemoveAt(i);
            }
        }

        foreach (Vector3 chunkPosition in shouldBeActive)
        {
            ActivateChunk(chunkPosition);
        }
    }

    void ActivateChunk(Vector3 chunkPosition)
    {
        //Gets the chunk at chunkPosition, return if already active
        GameObject chunk = FindOrGenerateChunk(chunkPosition);
        if (chunk.activeSelf) return;

        chunksActive.Add(chunk);
        chunk.SetActive(true);
    }

    float GetPerlinNoise(float x, float z)
    {
        float nx = (seed + (float)x / chunkSize * frequency) / 5;
        float nz = (seed + (float)z / chunkSize * frequency) / 5;
        float e = 0f;
        for (float i = 1; i <= octaves; i++)
        {
            float iPow = Mathf.Pow(i, pow);
            e += 1f / iPow * Mathf.PerlinNoise(iPow * nx, iPow * nz);
        }
        float elevation = Mathf.Pow(e, exp);
        float v =  LinearConversion(elevation, 0f, 1f, minNoise, maxNoise);
        return v;
    }

    float LinearConversion(float input, float input_start, float input_end, float output_start, float output_end)
    {
        return output_start + ((output_end - output_start) / (input_end - input_start)) * (input - input_start);
    }


    Color GetColor(int _height)
    {
        if (_height == 2) return dirt;
        else if (_height > 2 && _height < 20) return grass;
        else if (_height > 19 && _height < 35) return stone;
        else return snow;
    }

    private int Round(float value, int roundTo)
    {
        if (value > 0)
            return Mathf.CeilToInt(value / roundTo) * roundTo;
        else if (value < 0)
            return Mathf.FloorToInt(value / roundTo) * roundTo;
        else
            return (int)value;
    }
}
