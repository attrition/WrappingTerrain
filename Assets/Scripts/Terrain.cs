using UnityEngine;
using System.Collections.Generic;

public enum PlotType
{
    Water,
    Land,
    Coast,
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
    private Dictionary<PlotType, float> PlotHeights = new Dictionary<PlotType, float>
    {
        { PlotType.Water, 0f },
        { PlotType.Coast, 0.5f },
        { PlotType.Land, 1f },
    };

    private delegate void PlotMeshFunc(int x, int y, int absX, int absY, List<Vector3> verts, List<int> idxs, List<Vector2> uv1, List<Vector2> uv2, List<Vector3> normals, ref int vertCount);
    private Dictionary<PlotType, PlotMeshFunc> plotMeshFuncMap;

    // Use this for initialization
    void Start()
    {
        plotMeshFuncMap = new Dictionary<PlotType, PlotMeshFunc>
        {
            { PlotType.Land, AddLandPlotMesh },
            { PlotType.Water, AddWaterPlotMesh },
            { PlotType.Coast, AddCoastPlotMesh },
        };

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
                var idx = GetIndex(x, y);
                PlotType plot = PlotType.Land;
                if (HeightMap[idx] <= GetHeight(WaterLevel))
                    plot = PlotType.Water;

                PlotMap[idx] = plot;
            }
        }

        // third pass, add features such as coastline
        for (int y = 0; y < MapHeight; y++)
        {
            for (int x = 0; x < MapWidth; x++)
            {
                var idx = GetIndex(x, y);
                if (PlotMap[idx] == PlotType.Water && HasPlotTypeAsNeighbour(x, y, PlotType.Land))
                {
                    PlotMap[idx] = PlotType.Coast;
                }
            }
        }
    }

    bool HasPlotTypeAsNeighbour(int x, int y, PlotType type)
    {
        var xMin = Mathf.Max(x - 1, 0);
        var xMax = Mathf.Min(x + 1, MapWidth - 1);
        var yMin = Mathf.Max(y - 1, 0);
        var yMax = Mathf.Min(y + 1, MapHeight - 1);

        for (int yy = yMin; yy <= yMax; yy++)
        {
            for (int xx = xMin; xx <= xMax; xx++)
            {
                if (xx == x && yy == y)
                    continue;

                if (PlotMap[GetIndex(xx, yy)] == type)
                    return true;
            }
        }

        return false;
    }

    float[] GetDiamondSquareMap()
    {
        Random.seed = Seed;

        var heights = new float[MapWidth * MapHeight];
        var left = 0;
        var right = MapWidth - 1;
        var top = 0;
        var bottom = MapHeight - 1;

        var baseHeight = 100f;

        DiamondSquareR(ref heights, left, top, right, bottom, baseHeight);

        Random.seed = Seed + 1;
        DiamondSquareR(ref heights, left, top, right, bottom, baseHeight);
        Random.seed = Seed;

        return heights;
    }

    void DiamondSquareR(ref float[] map, int left, int top, int right, int bottom, float baseHeight)
    {
        int xc = (int)Mathf.Floor((left + right) / 2);
        int yc = (int)Mathf.Floor((top + bottom) / 2);

        // diamond step

        var cv = 0f;

        if (left == 0 && top == 0 && right == MapWidth - 1 && bottom == MapHeight - 1)
        {
            cv = baseHeight;
        }
        else
        {
            cv = Mathf.Floor(
                (
                    map[GetIndex(left, top)] +
                    map[GetIndex(right, top)] +
                    map[GetIndex(left, bottom)] +
                    map[GetIndex(right, bottom)]
                ) / 4
            ) - (Mathf.Floor(Random.Range(0f, 1f) - 0.5f) * baseHeight * 2);
        }
        map[GetIndex(xc, yc)] = cv;

        // square step

        // ensure top and bottom are seed to zero, so ground slopes into water near poles 
        if (top != 0)
            map[GetIndex(xc, top)] = Mathf.Floor(map[GetIndex(left, top)] + map[GetIndex(right, top)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        else
            map[GetIndex(xc, top)] = 0f;

        if (bottom != MapHeight - 1)
            map[GetIndex(xc, bottom)] = Mathf.Floor(map[GetIndex(left, bottom)] + map[GetIndex(right, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        else
            map[GetIndex(xc, bottom)] = 0f;

        map[GetIndex(left, yc)] = Mathf.Floor(map[GetIndex(left, top)] + map[GetIndex(left, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);
        map[GetIndex(right, yc)] = Mathf.Floor(map[GetIndex(right, top)] + map[GetIndex(right, bottom)]) / 2 + ((Random.Range(0f, 1f) - 0.5f) * baseHeight);

        if (right - left > 2)
        {
            baseHeight = Mathf.Floor(baseHeight * Mathf.Pow(2f, -0.90f));

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
                int offsetX = chunkX * ChunkSize;
                int offsetY = chunkY * ChunkSize;

                var verts = new List<Vector3>();
                var idxs = new List<int>();
                var uvs = new List<Vector2>();
                int vertCount = 0;

                for (int y = 0; y < ChunkSize; y++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        var absX = x + offsetX;
                        var absY = y + offsetY;

                        var plot = PlotMap[GetIndex(absX, absY)];
                        plotMeshFuncMap[plot](x, y, absX, absY, verts, idxs, uvs, null, null, ref vertCount);
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

    void AddLandPlotMesh(int x, int y, int absX, int absY, List<Vector3> verts, List<int> idxs, List<Vector2> uv1, List<Vector2> uv2, List<Vector3> normals, ref int vertCount)
    {
        var height = 1f;

        verts.Add(new Vector3(x, height, y));
        verts.Add(new Vector3(x, height, y + 1));
        verts.Add(new Vector3(x + 1, height, y + 1));
        verts.Add(new Vector3(x + 1, height, y));

        uv1.Add(new Vector2(absX, absY + 1));
        uv1.Add(new Vector2(absX, absY));
        uv1.Add(new Vector2(absX + 1, absY));
        uv1.Add(new Vector2(absX + 1, absY + 1));

        idxs.Add(vertCount);
        idxs.Add(vertCount + 1);
        idxs.Add(vertCount + 2);
        idxs.Add(vertCount + 2);
        idxs.Add(vertCount + 3);
        idxs.Add(vertCount);
        vertCount += 4;
    }

    void AddWaterPlotMesh(int x, int y, int absX, int absY, List<Vector3> verts, List<int> idxs, List<Vector2> uv1, List<Vector2> uv2, List<Vector3> normals, ref int vertCount)
    {
        var height = 0f;

        verts.Add(new Vector3(x, height, y));
        verts.Add(new Vector3(x, height, y + 1));
        verts.Add(new Vector3(x + 1, height, y + 1));
        verts.Add(new Vector3(x + 1, height, y));

        uv1.Add(new Vector2(x, y + 1));
        uv1.Add(new Vector2(x, y));
        uv1.Add(new Vector2(x + 1, y));
        uv1.Add(new Vector2(x + 1, y + 1));

        idxs.Add(vertCount);
        idxs.Add(vertCount + 1);
        idxs.Add(vertCount + 2);
        idxs.Add(vertCount + 2);
        idxs.Add(vertCount + 3);
        idxs.Add(vertCount);
        vertCount += 4;
    }

    void AddCoastPlotMesh(int x, int y, int absX, int absY, List<Vector3> verts, List<int> idxs, List<Vector2> uv1, List<Vector2> uv2, List<Vector3> normals, ref int vertCount)
    {
        var height = 0.5f;

        var vertsAcross = 15;
        var vertsMiddle = vertsAcross / 2;

        var localMap = new float[9];
        for (int localY = -1; localY <= 1; localY++)
        {
            for (int localX = -1; localX <= 1; localX++)
            {
                var relX = absX + localX;
                var relY = absY + localY;
                var idx = 4 + (localY * 3 + localX);

                if (relX < 0 || relX < 0 || relY > MapWidth || relY > MapHeight)
                    localMap[idx] = 0f;

                localMap[idx] = PlotHeights[PlotMap[GetIndex(relX, relY)]];
            }
        }

        var curr = localMap[4];
        var prevX = localMap[3];
        var nextX = localMap[5];
        var prevY = localMap[1];
        var nextY = localMap[7];

        for (int innerY = 0; innerY < vertsAcross; innerY++)
        {
            for (int innerX = 0; innerX < vertsAcross; innerX++)
            {
                var posX = (innerX / (vertsAcross - 1f));
                var posY = (innerY / (vertsAcross - 1f));
                
                height = Mathf.SmoothStep(prevX, nextX, posX);

                verts.Add(new Vector3(posX + x, height, posY + y));
                uv1.Add(new Vector2(posX + absX, posY + absY));

                if (innerX < vertsAcross - 1 && innerY < vertsAcross - 1)
                {
                    idxs.Add(vertCount);
                    idxs.Add(vertCount + vertsAcross);
                    idxs.Add(vertCount + vertsAcross + 1);

                    idxs.Add(vertCount + vertsAcross + 1);
                    idxs.Add(vertCount + 1);
                    idxs.Add(vertCount);
                }
                vertCount++;
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
