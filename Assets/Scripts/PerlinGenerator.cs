using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PerlinGenerator : MonoBehaviour
{
    public static PerlinGenerator instance = null;

    public int perlinTextureSizeX = 256;
    public int perlinTextureSizeY = 256;
    public bool randomizeNoiseOffset = true;
    public Vector2 perlinOffset = new Vector2(0, 0);
    public float noiseScale = 1f;
    public int perlinGridStepSizeX = 40;
    public int perlinGridStepSizeY = 40;

    public bool visualizeGrid = false;
    public GameObject visualizationCube;
    public float visualizationHeightScale = 5f;
    public RawImage visualizationUI;

    private Texture2D perlinTexture;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else if(instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Generate();
    }

    public void Generate()
    {
        if(gameObject.transform.childCount > 0)
            Destroy(gameObject.transform.GetChild(0).gameObject);
        GenerateNoise();
        if(visualizeGrid)
        {
            VisualizeGrid();
        }
    }

    private void GenerateNoise()
    {
        if(randomizeNoiseOffset)
        {
            perlinOffset = new Vector2(Random.Range(-99999, 99999), Random.Range(-99999, 99999));
        }

        perlinTexture = new Texture2D(perlinTextureSizeX, perlinTextureSizeY);

        for (int x = 0; x < perlinTextureSizeX; x++)
        {
            for (int y = 0; y < perlinTextureSizeY; y++)
            {
                perlinTexture.SetPixel(x, y, SampleNoise(x, y));
            }
        }

        perlinTexture.Apply();
        visualizationUI.texture = perlinTexture;
    }

    private Color SampleNoise(int x, int y)
    {
        float xCoord = (float)x / perlinTextureSizeX * noiseScale + perlinOffset.x;
        float yCoord = (float)y / perlinTextureSizeY * noiseScale + perlinOffset.y;

        float sample = Mathf.PerlinNoise(xCoord, yCoord);
        return new Color(sample, sample, sample);
    }
    public float SampleStepped(int x, int y)
    {
        int gridStepSizeX = perlinTextureSizeX / perlinGridStepSizeX;
        int gridStepSizeY = perlinTextureSizeY / perlinGridStepSizeY;

        return perlinTexture.GetPixel(Mathf.FloorToInt(x * gridStepSizeX), Mathf.FloorToInt(y * gridStepSizeY)).grayscale;
    }

    private void VisualizeGrid()
    {
        GameObject visuParent = new GameObject("VisualizationParent");
        visuParent.transform.SetParent(this.transform);

        for (int x = 0; x < perlinGridStepSizeX; x++)
        {
            for (int y = 0; y < perlinGridStepSizeY; y++)
            {
                GameObject clone = Instantiate(visualizationCube, new Vector3(x, SampleStepped(x, y) * visualizationHeightScale, y) + transform.position, transform.rotation);
                clone.transform.SetParent(visuParent.transform);           
            }
        }
        visuParent.transform.position = new Vector3(-perlinGridStepSizeX * .5f, -visualizationHeightScale * .5f, -perlinGridStepSizeY * .5f);
    }
}
