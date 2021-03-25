using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class testt : MonoBehaviour
{
    public GameObject cube;
    public GameObject quad;

    public List<Vector3> faces;
    Dictionary<Vector3, Vector3> offsetsAndRotations;

    Vector3 zFace = new Vector3(0, 0, 0);
    Vector3 nzFace = new Vector3(-180, 0, 0);
    private void Start()
    {
        offsetsAndRotations = new Dictionary<Vector3, Vector3>();
        offsetsAndRotations.Add(new Vector3(0, 0, 0), new Vector3(0, 0, 0));
        offsetsAndRotations.Add(new Vector3(0, 0.5f, 0.5f), new Vector3(90, 0, 0));
        offsetsAndRotations.Add(new Vector3(0, 0, 1f), new Vector3(180, 0, 0));
        offsetsAndRotations.Add(new Vector3(0, -0.5f, 0.5f), new Vector3(270, 0, 0));
        offsetsAndRotations.Add(new Vector3(-0.5f, 0, 0.5f), new Vector3(0, 90, 0));
        offsetsAndRotations.Add(new Vector3(0.5f, 0, 0.5f), new Vector3(0, 270, 0));
        CreateQuadCube(new Vector3(0, 0, 0));
    }

    void CreateQuadCube(Vector3 position)
    {
        foreach(KeyValuePair<Vector3, Vector3> entry in offsetsAndRotations)
        {
            Instantiate(quad, position + entry.Key, Quaternion.Euler(entry.Value), transform);
        }
    }
}
