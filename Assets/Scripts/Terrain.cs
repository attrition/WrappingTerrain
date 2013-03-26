using UnityEngine;
using System.Collections.Generic;

public enum PlotType
{
    Water,
    Land,
    Hill,
    Mountain,
    Forest,
    Desert,
    Tundra,
    Swamp,
    River,
}

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

    public float[] HeightMap = null;
    public PlotType[] PlotMap = null;

    private List<TerrainChunk> chunks = null;

    private float HighestPoint = 0f;
    private float LowestPoint = 0f;

    public float WaterLevel = 68f;

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
        LowestPoint = float.PositiveInfinity;
        HighestPoint = float.NegativeInfinity;

        //HeightMap = GetHeightMap();
        HeightMap = GetDiamondSquareMap();
        PlotMap = new PlotType[MapWidth * MapHeight];

        // first pass, get lo/hi/mid/avg
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var val = HeightMap[GetIndex(x, y)];
                if (val < LowestPoint)
                    LowestPoint = val;

                if (val > HighestPoint)
                    HighestPoint = val;
            }
        }

        // second pass, assign terraced heightmap
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                PlotType plot = PlotType.Land;
                if (HeightMap[GetIndex(x, y)] <= GetHeight(WaterLevel))
                    plot = PlotType.Water;

                PlotMap[GetIndex(x, y)] = plot;
            }
        }
    }

    float[] GetDiamondSquareMap()
    {
        Random.seed = Seed;
        if (MapHeight > MapWidth)
            MapHeight = MapWidth; // height must be at most equal to width, noise uses width as its maximum to allow wrapping

        var heights = new float[MapWidth * MapHeight];
        var left = 0;
        var right = MapWidth - 1;
        var top = 0;
        var bottom = MapHeight - 1;

        var baseHeight = 1000f;

        DiamondSquareR(ref heights, left, top, right, bottom, baseHeight);

        return heights;
    }

    void DiamondSquareR(ref float[] map, int left, int top, int right, int bottom, float baseHeight)
    {
        int xc = (int)Mathf.Floor((left + right) / 2);
        int yc = (int)Mathf.Floor((top + bottom) / 2);

        // diamond step
        
        var cv = Mathf.Floor(
            (
                map[GetIndex(left, top)] +
                map[GetIndex(right, top)] +
                map[GetIndex(left, bottom)] +
                map[GetIndex(right, bottom)]
            ) / 4
        ) - (Mathf.Floor(Random.Range(0f, 1f) - 0.5f) * baseHeight * 2);
        
        map[GetIndex(xc, yc)] = cv;

        // square step

        map[GetIndex(xc, top)] = Mathf.Floor(map[GetIndex(left, top)] + map[GetIndex(right, top)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        map[GetIndex(xc, bottom)] = Mathf.Floor(map[GetIndex(left, bottom)] + map[GetIndex(right, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        map[GetIndex(left, yc)] = Mathf.Floor(map[GetIndex(left, top)] + map[GetIndex(left, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        map[GetIndex(right, yc)] = Mathf.Floor(map[GetIndex(right, top)] + map[GetIndex(right, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);

        if (right - left > 2)
        {
            baseHeight = Mathf.Floor(baseHeight * Mathf.Pow(2f, -0.75f));

            DiamondSquareR(ref map, left, top, xc, yc, baseHeight);
            DiamondSquareR(ref map, xc, top, right, yc, baseHeight);
            DiamondSquareR(ref map, left, yc, xc, bottom, baseHeight);
            DiamondSquareR(ref map, xc, yc, right, bottom, baseHeight);
        }
    }

    int GetIndex(int x, int y)
    {
        return y * MapWidth + x;
    }

    float[] GetHeightMap()
    {
        if (MapHeight > MapWidth)
            MapHeight = MapWidth; // height must be at most equal to width, noise uses width as its maximum to allow wrapping

        var perlin = new LibNoise.Unity.Generator.Perlin(Frequency, Lacunarity, Persistence, Octaves, Seed, LibNoise.Unity.QualityMode.High);
        var noise = new LibNoise.Unity.Noise2D(MapWidth, perlin);
        noise.GeneratePlanar(0f, 1f, 0f, 1f, true);

        var heights = new float[MapWidth * MapHeight];

        for (int y = 0; y < MapHeight; y++)
            for (int x = 0; x < MapWidth; x++)
                heights[GetIndex(x, y)] = noise[x, y];

        return heights;
    }

    void GenerateMeshes()
    {
        if (HeightMap == null)
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

                        // most everything is at 1f, excepting water and possibly coasts
                        var height = 1f;// GetHeightAt(x, y);
                        if (PlotMap[GetIndex(absX, absY)] == PlotType.Water)
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

    float GetHeight(float percent)
    {
        return (percent / 100f) * (HighestPoint - LowestPoint) + LowestPoint;
    }

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
