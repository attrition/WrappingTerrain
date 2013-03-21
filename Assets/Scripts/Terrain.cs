using UnityEngine;
using System.Collections.Generic;

public class Terrain : MonoBehaviour
{
    public float MoveSpeed = 32f;

    public int Seed = 1337;
    public double Frequency = 1f;
    public double Lacunarity = 2f;
    public double Persistence = 0.5f;
    public int Octaves = 6;

    public int MapWidth = 16;
    public int MapHeight = 16;
    public int ChunkSize = 16;

    public Texture2D HeightMapDebug = null;
    public float[] HeightMap = null;

    private List<TerrainChunk> chunks = null;

    // Use this for initialization
    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        GenerateHeightMap();
        GenerateMeshes();
    }

    void GenerateHeightMap()
    {
        if (MapHeight > MapWidth)
            MapHeight = MapWidth; // height must be at most equal to width, noise uses width as its maximum to allow wrapping
        var perlin = new LibNoise.Unity.Generator.Perlin(Frequency, Lacunarity, Persistence, Octaves, Seed, LibNoise.Unity.QualityMode.High);
        var noise = new LibNoise.Unity.Noise2D(MapWidth, perlin);
        noise.GeneratePlanar(0f, 1f, 0f, 1f, true);

        if (HeightMapDebug != null)
            Destroy(HeightMapDebug);

        HeightMapDebug = noise.GetTexture(LibNoise.Unity.Gradient.Grayscale);
        HeightMapDebug.Apply();

        float lo = float.PositiveInfinity;
        float hi = float.NegativeInfinity;
        float mid = 0f;
        float avg = 0f;

        HeightMap = new float[MapWidth * MapHeight];

        // first pass, get lo/hi/mid/avg
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var val = noise[x, y];
                if (val < lo)
                    lo = val;

                if (val > hi)
                    hi = val;

                avg += val;
            }
        }
        avg /= HeightMap.Length;
        mid = (hi + lo) / 2;

        Debug.Log("avg: " + avg + " | mid: " + mid);

        // second pass, assign terraced heightmap
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var val = noise[x, y];
                if (val < mid)
                    val = 0f;
                else
                    val = 1f;

                HeightMap[y * MapWidth + x] = val;
            }
        }
    }

    void GenerateMeshes()
    {
        if (HeightMapDebug == null)
        {
            Debug.Log("Tried to generate terrain mesh, but heightmap is null");
            return;
        }

        if (chunks != null)
        {
            foreach (var chunk in chunks)
                Destroy(chunk.gameObject);
        }

        chunks = new List<TerrainChunk>();

        int chunksX = MapWidth / ChunkSize;
        int chunksY = MapHeight / ChunkSize;

        for (int chunkY = 0; chunkY < chunksY; chunkY++)
        {
            for (int chunkX = 0; chunkX < chunksX; chunkX++)
            {
                int i = 0;
                int offsetX = chunkX * ChunkSize;
                int offsetY = chunkY * ChunkSize;

                var verts = new List<Vector3>();
                var idxs = new List<int>();
                var uvs = new List<Vector2>();

                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        var absX = x + offsetX;
                        var absY = y + offsetY;

                        var height = GetHeightAt(absX, absY);

                        if (height == 0f)
                            continue;
                        
                        verts.Add(new Vector3(x, height, y));
                        verts.Add(new Vector3(x, height, y + 1));
                        verts.Add(new Vector3(x + 1, height, y + 1));
                        verts.Add(new Vector3(x + 1, height, y));

                        uvs.Add(new Vector2(x, y + 1));
                        uvs.Add(new Vector2(x, y));
                        uvs.Add(new Vector2(x + 1, y));
                        uvs.Add(new Vector2(x + 1, y + 1));

                        idxs.Add(i);
                        idxs.Add(i + 1);
                        idxs.Add(i + 2);
                        idxs.Add(i + 2);
                        idxs.Add(i + 3);
                        idxs.Add(i);
                        i += 4;
                    }
                }

                var meshId = "[" + chunkX + "," + chunkY + "]";
                var chunkObj = new GameObject("Terrain Chunk " + meshId);
                chunkObj.transform.position = new Vector3(offsetX, 0f, offsetY);
                chunkObj.transform.parent = this.transform;

                var chunk = chunkObj.AddComponent<TerrainChunk>();

                var mesh = new Mesh();
                mesh.name = "Terrain Mesh " + meshId;
                mesh.vertices = verts.ToArray();
                mesh.triangles = idxs.ToArray();
                mesh.uv = uvs.ToArray();
                mesh.RecalculateNormals();

                chunk.Mesh = mesh;
                chunk.Material = Resources.Load("Materials/Terrain") as Material;
                chunk.Material.color = new Color(chunkX / (float)chunksX, chunkY / (float)chunksY, 0f, 1f);

                chunks.Add(chunk);
            }
        }
    }

    float GetHeightAt(int x, int y)
    {
        return HeightMap[y * MapWidth + x];
    }

    /* Old style
                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        verts.Add(new Vector3(x, GetHeightAt(offsetX + x, offsetY + y), y));
                        uvs.Add(new Vector2(x, y));

                        if (x < ChunkSize - 1 && y < ChunkSize - 1)
                        {
                            idxs.Add(i);
                            idxs.Add(i + ChunkSize);
                            idxs.Add(i + ChunkSize + 1);

                            idxs.Add(i + ChunkSize + 1);
                            idxs.Add(i + 1);
                            idxs.Add(i);
                        }
                        i++;
                    }
                }
    */

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            GenerateMap();

        var moveX = 0f;
        var moveY = 0f;

        if (Input.GetKey(KeyCode.A))
            moveX += MoveSpeed;
        if (Input.GetKey(KeyCode.D))
            moveX -= MoveSpeed;
        if (Input.GetKey(KeyCode.W))
            moveY -= MoveSpeed;
        if (Input.GetKey(KeyCode.S))
            moveY += MoveSpeed;

        moveX *= Time.deltaTime;
        moveY *= Time.deltaTime;

        if (moveX != 0f || moveY != 0f)
        {
            foreach (var chunk in chunks)
            {
                var pos = chunk.gameObject.transform.position;
                pos.x += moveX;
                pos.z += moveY;

                if (pos.x < 0f)
                    pos.x += MapWidth;
                else if (pos.x > MapWidth)
                    pos.x -= MapWidth;

                chunk.gameObject.transform.position = pos;
            }
        }
    }
}
