using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerlinNoiseGenerator : MonoBehaviour
{
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

    public RawImage textureImage;
    private Texture2D noiseTex;
    private float[,] heightMap;

    void Start()
    {
        noiseTex = new Texture2D(width, height);
        textureImage.texture = noiseTex;

        GenerateHeightMap();
        GenerateTexture();
    }

    private void Update()
    {
        GenerateHeightMap();
        GenerateTexture();
    }

    void GenerateHeightMap()
    {
        heightMap = new float[width, height];

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
                float elevation = Mathf.Pow(e, exp);
                heightMap[y, x] = LinearConversion(elevation, 0f, 1f, minNoise, maxNoise);
            }
        }
    }

    void GenerateTexture()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sample = heightMap[x, y];
                noiseTex.SetPixel(x, y, new Color(sample, sample, sample));
            }
        }

        noiseTex.Apply();
    }

    float LinearConversion(float input, float input_start, float input_end, float output_start, float output_end)
    {
        return output_start + ((output_end - output_start) / (input_end - input_start)) * (input - input_start);
    }

    void SaveTextureToPNG(string path, Texture2D texture)
    {
        System.IO.File.WriteAllBytes(path, texture.EncodeToJPG());
    }
}
