using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    public GameObject blockPrefab;
    public Mesh blockMesh;
    public GameObject quadPrefab;
    public Transform player;

    public int chunkSize = 16;
    [Min(1)]
    public int maxRenderDistance = 8;
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

    private List<GameObject> chunks;
    private List<GameObject> chunksActive;

    //face id, face position offset, face rotation
    private Dictionary<int, KeyValuePair<Vector3, Vector3>> quadFaces;

    private void Start()
    {
        chunks = new List<GameObject>();
        chunksActive = new List<GameObject>();
        seed = Random.Range(-99999, 99999);

        //Top:0, Bottom:1, Front:2, Back:3, Left:4, Right:5
        quadFaces = new Dictionary<int, KeyValuePair<Vector3, Vector3>>
        {
            { 0, new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0.5f, 0.5f), new Vector3(90, 0, 0)) },
            { 1, new KeyValuePair<Vector3, Vector3>(new Vector3(0, -0.5f, 0.5f), new Vector3(-90, 0, 0)) },
            { 2, new KeyValuePair<Vector3, Vector3>(Vector3.zero, Vector3.zero) },
            { 3, new KeyValuePair<Vector3, Vector3>(new Vector3(0, 0, 1f), new Vector3(-180, 0, 0)) },
            { 4, new KeyValuePair<Vector3, Vector3>(new Vector3(-0.5f, 0, 0.5f), new Vector3(0, 90, 0)) },
            { 5, new KeyValuePair<Vector3, Vector3>(new Vector3(0.5f, 0, 0.5f), new Vector3(0, -90, 0)) }
        };

        //Update chunks active every 0.1 seconds
        InvokeRepeating(nameof(UpdateChunksActive), 0.1f, 0.1f);
    }

    GameObject GenerateChunk(ChunkData data)
    {
        GameObject chunk = CreateChunkMesh(data);
        chunks.Add(chunk);
        chunksActive.Add(chunk);
        return chunk;
    }

    ChunkData GenerateChunkData(Vector3 chunkPosition)
    {
        List<float> heights = new List<float>();

        for (int x = 0, i = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++, i++)
            {
                int posX = (int)(chunkPosition.x + x);
                int posZ = (int)(chunkPosition.z + z);
                int height = Round(Map(GetPerlinNoise(posX, posZ), 0, 1, 0, heightMultiplier), 1);
                heights.Add(height);
            }
        }

        ChunkData data = new ChunkData(chunkPosition, chunkSize, heights);
        return data;
    }

    GameObject CreateChunkMesh(ChunkData data)
    {
        CombineInstance[] combine = new CombineInstance[chunkSize * chunkSize];
        for (int x = 0, i = 0; x < chunkSize; x++)
        {
            for (int z = 0; z < chunkSize; z++, i++)
            {
                combine[i].mesh = blockMesh;
                combine[i].transform = Matrix4x4.Translate(new Vector3(x, data.heights[i], z));
            }
        }

        string chunkName = "Chunk " + data.chunkPosition.x + "," + data.chunkPosition.z;
        GameObject chunk = new GameObject(chunkName);
        chunk.AddComponent<MeshFilter>();
        chunk.AddComponent<MeshRenderer>().material.color = Random.ColorHSV();
        chunk.transform.SetParent(transform);
        chunk.transform.position = data.chunkPosition;

        chunk.transform.GetComponent<MeshFilter>().mesh = new Mesh();
        chunk.transform.GetComponent<MeshFilter>().mesh.CombineMeshes(combine);
        chunk.transform.GetComponent<MeshFilter>().mesh.RecalculateBounds();
        chunk.transform.GetComponent<MeshFilter>().mesh.RecalculateNormals();
        chunk.transform.GetComponent<MeshFilter>().mesh.Optimize();

        return chunk;
    }

    List<Vector3> GenerateChunkPositionsAroundPlayer()
    {
        List<Vector3> positions = new List<Vector3>();
        Vector3 playerPosRounded = new Vector3(
            Round(player.transform.position.x, chunkSize),
            0,
            Round(player.transform.position.z, chunkSize));

        int viewRadius = player.GetComponent<FieldOfView>().viewRadius;
        int viewAngle = player.GetComponent<FieldOfView>().viewAngle;
        int renderDistance = Mathf.Clamp(Mathf.FloorToInt(viewRadius / chunkSize), 2, maxRenderDistance);
        for (int z = -renderDistance; z < renderDistance; z++)
        {
            for (int x = -renderDistance; x < renderDistance; x++)
            {
                int posX = (int)(playerPosRounded.x + x * chunkSize);
                int posZ = (int)(playerPosRounded.z + z * chunkSize);
                Vector3 chunkPos = new Vector3(posX, 0, posZ);
                Vector2 chunkPos2D = new Vector2(posX, posZ);
                Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.z);
                Vector2 dirToChunk2D = (chunkPos2D - playerPos2D).normalized;

                //Outside of the render distance
                if (Vector2.Distance(chunkPos2D, playerPos2D) > viewRadius) continue;

                //Chunks around the player are always active regardless of the fov
                if (Vector2.Distance(playerPos2D, chunkPos2D) < chunkSize * 2)
                {
                    positions.Add(chunkPos);
                }
                //Chunks in fov
                else
                {
                    Vector2 forward2D = new Vector2(player.transform.forward.x, player.transform.forward.z);
                    if (Vector2.Angle(forward2D, dirToChunk2D) < viewAngle / 2)
                    {
                        positions.Add(chunkPos);
                    }
                }
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

    void DestroyFarAwayChunks()
    {
        for (int i = chunks.Count - 1; i >= 0; i--)
        {
            GameObject chunk = chunks[i];
            Vector2 playerPos2D = new Vector2(player.transform.position.x, player.transform.position.z);
            Vector2 chunkPos2D = new Vector2(chunk.transform.position.x, chunk.transform.position.z);
            if (Vector2.Distance(playerPos2D, chunkPos2D) > chunkSize * 30)
            {
                chunks.Remove(chunk);
                Destroy(chunk);
            }
        }
    }

    void UpdateChunksActive()
    {
        DestroyFarAwayChunks();
        List<Vector3> shouldBeActive = GenerateChunkPositionsAroundPlayer();

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

    float GetPerlinNoise(float x, float z)
    {
        float nx = (seed + (float)x / chunkSize * frequency);
        float nz = (seed + (float)z / chunkSize * frequency);
        float e = 0f;
        for (float i = 1; i <= octaves; i++)
        {
            float iPow = Mathf.Pow(i, pow);
            e += 1f / iPow * Mathf.PerlinNoise(iPow * nx, iPow * nz);
        }
        float elevation = Mathf.Pow(e, exp);
        return Map(elevation, 0f, 1f, minNoise, maxNoise);
    }

    float Map(float input, float inputStart, float inputEnd, float outputStart, float outputEnd)
    {
        return outputStart + (outputEnd - outputStart) / (inputEnd - inputStart) * (input - inputStart);
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
    public List<float> heights;

    public ChunkData(Vector3 _chunkPosition, int _chunkSize, List<float> _heights)
    {
        chunkPosition = _chunkPosition;
        chunkSize = _chunkSize;
        heights = _heights;
    }
}
