using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    [Min(1)]
    public float maxHeight = 5f;
    public bool useTerraces = false;
    public bool useCeiling = false;
    [Min(1)]
    public int terraces = 12;
    [Min(5)]
    public int width = 20;
    [Min(5)]
    public int height = 20;
    public float xOrg = 0f;
    public float yOrg = 0f;
    [Range(0, 1)]
    public float minNoise = 0f;
    [Range(0, 1)]
    public float maxNoise = 1f;
    [Range(1, 20)]
    public float frequency = 3f;
    [Range(1, 8)]
    public int octaves = 3;
    [Range(0.01f, 20f)]
    public float exp = 1;
    [Range(1, 10)]
    public int pow = 2;

    Mesh mesh;
    Vector3[] vertices;
    int[] triangles;
    Vector2[] uvs;

    private float[,] heightMap;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
        GenerateHeightMap();
        CreateShape();
        UpdateMesh();
    }

    private void Update()
    {
        GenerateHeightMap();
        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        vertices = new Vector3[(width + 1) * (height + 1)];

        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                float y = heightMap[x, z];
                vertices[i] = new Vector3(x, y, z);
            }
        }

        triangles = new int[width * height * 6];

        for (int z = 0, vert = 0, tris = 0; z < height; z++, vert++)
        {
            for (int x = 0; x < width; x++, vert++, tris += 6)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + width + 1;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + width + 1;
                triangles[tris + 5] = vert + width + 2;
            }
        }

        uvs = new Vector2[vertices.Length];

        for (int z = 0, i = 0; z <= height; z++)
        {
            for (int x = 0; x <= width; x++, i++)
            {
                uvs[i] = new Vector2((float)x / width, (float)z / height);
            }
        }
    }
    void UpdateMesh()
    {
        mesh.Clear();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();
    }

    void GenerateHeightMap()
    {
        heightMap = new float[width + 1, height + 1];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = xOrg + (float)x / width * frequency;
                float ny = yOrg + (float)y / height * frequency;
                float e = 0f;
                for (float i = 1; i <= octaves; i++)
                {
                    float iPow2 = Mathf.Pow(i, pow);
                    e += 1f / iPow2 * Mathf.PerlinNoise(iPow2 * nx, iPow2 * ny);
                }
                float elevation = useTerraces ? Mathf.Round(e * terraces) / terraces * maxHeight : Mathf.Pow(e, exp);
                heightMap[y, x] = useCeiling
                    ? Mathf.Clamp(1f / LinearConversion(elevation, 0f, 1f, minNoise, maxNoise), 0, maxHeight)
                    : LinearConversion(elevation, 0f, 1f, minNoise, maxNoise) * maxHeight;
            }
        }
    }

    float LinearConversion(float input, float input_start, float input_end, float output_start, float output_end)
    {
        return output_start + ((output_end - output_start) / (input_end - input_start)) * (input - input_start);
    }

    float Scale01(float value, float valueMin, float valueMax)
    {
        return (value - valueMin) / (valueMax - valueMin);
    }

    void SaveTextureToPNG(string path, Texture2D texture)
    {
        System.IO.File.WriteAllBytes(path, texture.EncodeToJPG());
    }
}
