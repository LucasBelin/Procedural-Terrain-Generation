using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkData
{
    public Vector3 origin;
    public int chunkSize;
    public Vector3[] heightMap;

    public ChunkData(Vector3 _origin, int _chunkSize, Vector3[] _heightMap)
    {
        this.origin = _origin;
        this.chunkSize = _chunkSize;
        this.heightMap = _heightMap;
    }
}
