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
    public float[] heightMap = null;

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
        var perlin = new LibNoise.Unity.Generator.Perlin(Frequency, Lacunarity, Persistence, Octaves, Seed, LibNoise.Unity.QualityMode.High);
        var noise = new LibNoise.Unity.Noise2D(Mathf.Max(MapWidth, MapHeight), perlin);
        noise.GeneratePlanar(0f, 1f, 0f, 1f);

        if (HeightMapDebug != null)
            Destroy(HeightMapDebug);

        HeightMapDebug = noise.GetTexture(LibNoise.Unity.Gradient.Grayscale);
        HeightMapDebug.Apply();
        
        heightMap = new float[MapWidth * MapHeight];
        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                heightMap[y * MapWidth + x] = noise[x, y];
    }

    void GenerateMeshes()
    {
        if (HeightMapDebug == null)
        {
            Debug.Log("Tried to generate terrain mesh, but heightmap is null");
            return;
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
                chunk.Material.color = new Color(0f, 0.8f, 0f, 1f);

                chunks.Add(chunk);
            }
        }
    }

    float GetHeightAt(int x, int y)
    {
        return heightMap[y * MapWidth + x];
    }

    /* Old style
    void GenerateOldStyle()
    {
        chunks = new List<TerrainChunk>();

        var chunksX = MapWidth / CHUNK_SIZE;
        for (int x = 0; x < chunksX; x++)
        {
            float offsetX = x * CHUNK_SIZE;

            var chunkObj = new GameObject("Terrain Chunk " + x);
            chunkObj.transform.position = new Vector3(offsetX, 0f, 0f);
            chunkObj.transform.parent = this.transform;

            var chunk = chunkObj.AddComponent<TerrainChunk>();
            var mesh = new Mesh();

            mesh.vertices = new[] {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, MapHeight),
                new Vector3(CHUNK_SIZE, 0f, MapHeight),
                new Vector3(CHUNK_SIZE, 0f, 0f)
            };

            mesh.triangles = new[] {
                0, 1, 2, 2, 3, 0
            };

            mesh.uv = new[] {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

            mesh.RecalculateNormals();

            chunk.Mesh = mesh;
            chunk.Material = Resources.Load("Materials/Terrain") as Material;
            chunk.Material.color = new Color(0f, offsetX / MapWidth, 0f, 1f);

            chunks.Add(chunk);
        }
    }
    */

    // Update is called once per frame
    void Update()
    {
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
