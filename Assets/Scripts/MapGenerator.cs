using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject blockPrefab;
    public GameObject quadPrefab;
    public Transform player;

    public int chunkSize = 16;
    [Min(1)]
    public int renderDistance = 8;
    public int baseHeight = 32;
    public int heightMultiplier = 32;
    public enum GenerationMode { Cube, Quad };
    public GenerationMode mode;

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

    //face id, face position offset, face rotation
    private Dictionary<int, KeyValuePair<Vector3, Vector3>> quadFaces;

    public Color dirt;
    public Color grass;
    public Color stone;
    public Color snow;

    private void Start()
    {
        chunksData = new List<ChunkData>();
        chunks = new List<GameObject>();
        chunksActive = new List<GameObject>();
        seed = Random.Range(-99999, 99999);

        //Top:0, Bottom:1, Front:2, Back:3, Left:4, Right:5
        quadFaces = new Dictionary<int, KeyValuePair<Vector3, Vector3>>();
        quadFaces.Add(0, new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.5f, 0.5f), new Vector3(90, 0, 0)));
        quadFaces.Add(1, new KeyValuePair<Vector3, Vector3>(new Vector3(0, -0.5f, 0.5f), new Vector3(-90, 0, 0)));
        quadFaces.Add(2, new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero));
        quadFaces.Add(3, new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0, 1f), new Vector3(-180, 0, 0)));
        quadFaces.Add(4, new KeyValuePair<Vector3, Vector3>(new Vector3(-0.5f, 0, 0.5f), new Vector3(0, 90, 0)));
        quadFaces.Add(5, new KeyValuePair<Vector3, Vector3>(new Vector3(0.5f, 0, 0.5f), new Vector3(0, -90, 0)));
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
            KeyValuePair<Vector3, Vector3> faceOffsetAndRotation = quadFaces[id];
            Vector3 faceOffset = faceOffsetAndRotation.Key;
            Vector3 faceRotation = faceOffsetAndRotation.Value;

            Vector3 facePosition = position + faceOffset;
            GameObject face = Instantiate(quadPrefab, facePosition, Quaternion.Euler(faceRotation), block.transform);
            face.GetComponent<MeshRenderer>().material.color = color;
        }

        return block;
    }

    GameObject GenerateChunk(ChunkData data)
    {
        string chunkName = "Chunk " + data.chunkPosition.x + "," + data.chunkPosition.z;
        GameObject chunk = new GameObject(chunkName);
        chunk.AddComponent<MeshFilter>().mesh = quadPrefab.GetComponent<MeshFilter>().sharedMesh;
        chunk.AddComponent<MeshRenderer>().material.color = Random.ColorHSV();
        chunk.transform.SetParent(transform);
        chunk.transform.position = data.chunkPosition;

        foreach(Vector3 pos in data.blockPositions)
        {
            if(mode == GenerationMode.Cube)
            {
                //Only top most layer
                GameObject block = Instantiate(blockPrefab, pos, Quaternion.identity, chunk.transform);
                block.GetComponent<MeshRenderer>().material.color = GetColor((int)pos.y);
            }
            else if(mode == GenerationMode.Quad)
            {
                //TODO determine which faces to render
                CreateBlock(chunk.transform, pos, GetColor((int)pos.y), new List<int> { 0 });
            }
        }
        if(mode == GenerationMode.Cube)
            CreateChunkMesh(chunk);

        chunks.Add(chunk);
        chunksActive.Add(chunk);
        return chunk;
    }

    void CreateChunkMesh(GameObject chunk)
    {
        MeshFilter[] meshFilters = chunk.GetComponentsInChildren<MeshFilter>();
        CombineInstance[] combine = new CombineInstance[meshFilters.Length];

        int i = 1;
        while (i < meshFilters.Length)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = Matrix4x4.Translate(meshFilters[i].transform.localPosition);
            meshFilters[i].gameObject.SetActive(false);

            i++;
        }
        chunk.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        chunk.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        chunk.transform.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        chunk.transform.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        chunk.transform.GetComponent<MeshFilter>().mesh.Optimize();
    }

    ChunkData GenerateChunkData(Vector3 chunkPosition)
    {
        Vector3[] heights = new Vector3[chunkSize * chunkSize];

        for (int x = 0, i = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++, i++)
            {
                int posX = (int)(chunkPosition.x + x);
                int posZ = (int)(chunkPosition.z + z);
                int height = Round(LinearConversion(GetPerlinNoise(posX, posZ), 0, 1, 0, heightMultiplier), 1);
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
            Round(player.transform.position.x, (chunkSize)),
            0,
            Round(player.transform.position.z, (chunkSize)));

        for (int z = -renderDistance; z < renderDistance; z++)
        {
            for (int x = -renderDistance; x < renderDistance; x++)
            {
                int posX = (int)(playerPos.x + x * chunkSize);
                int posZ = (int)(playerPos.z + z * chunkSize);
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

    //Temporary
    Color GetColor(int height)
    {
        if (height <= 15) return stone;
        else if (height > 15 && height <= 30) return dirt;
        else if (height > 30 && height < 50) return grass;
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

struct ChunkData
{
    public Vector3 chunkPosition;
    public int chunkSize;
    public Vector3[] blockPositions;

    public ChunkData(Vector3 _chunkPosition, int _chunkSize, Vector3[] _blockPositions)
    {
        chunkPosition = _chunkPosition;
        chunkSize = _chunkSize;
        blockPositions = _blockPositions;
    }
}
