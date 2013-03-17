using UnityEngine;
using System.Collections.Generic;

public class Terrain : MonoBehaviour
{
    public float SideWidth = 64f;
    public float MoveSpeed = 32f;

    private const float CHUNK_WIDTH = 16f;
    private List<TerrainChunk> chunks;

	// Use this for initialization
	void Start()
	{
        chunks = new List<TerrainChunk>();

        var chunksX = SideWidth / CHUNK_WIDTH;
        for (int x = 0; x < chunksX; x++)
        {
            float offsetX = x * CHUNK_WIDTH;

            var chunkObj = new GameObject("Terrain Chunk " + x);
            chunkObj.transform.position = new Vector3(offsetX, 0f, 0f);
            chunkObj.transform.parent = this.transform;

            var chunk = chunkObj.AddComponent<TerrainChunk>();
    		var mesh = new Mesh();

    		mesh.vertices = new [] {
                new Vector3(0f, 0f, 0f),
                new Vector3(0f, 0f, SideWidth),
                new Vector3(CHUNK_WIDTH, 0f, SideWidth),
                new Vector3(CHUNK_WIDTH, 0f, 0f)
            };

    		mesh.triangles = new [] {
                0, 1, 2, 2, 3, 0
            };

    		mesh.uv = new [] {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

    		mesh.RecalculateNormals();

            chunk.Mesh = mesh;
            chunk.Material = Resources.Load("Materials/Terrain") as Material;
            chunk.Material.color = new Color(0f, offsetX / SideWidth, 0f, 1f);

            chunks.Add(chunk);
        }
    }
	
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
                    pos.x += SideWidth;
                else if (pos.x > SideWidth)
                    pos.x -= SideWidth;

                chunk.gameObject.transform.position = pos;
            }
        }
	}
}
