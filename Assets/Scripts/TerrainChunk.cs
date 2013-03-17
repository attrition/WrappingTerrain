using UnityEngine;
using System.Collections.Generic;

public class TerrainChunk : MonoBehaviour
{
    public Mesh Mesh { get; set; }

    public Material Material
    {
        get { return this.gameObject.renderer.material; }
        set { this.gameObject.renderer.material = value; }
    }

    void Awake()
    {
        Mesh = null;
        this.gameObject.AddComponent<MeshRenderer>();
        this.gameObject.AddComponent<MeshFilter>();
        this.gameObject.AddComponent<MeshCollider>();
    }

    void FixedUpdate()
    {
        if (Mesh != null)
        {
            var filt = this.gameObject.GetComponent<MeshFilter>();
            var coll = this.gameObject.GetComponent<MeshCollider>();
            filt.sharedMesh = Mesh;
            coll.sharedMesh = Mesh;
        }
    }

    void OnMouseOver()
    {
    }

    void OnMouseDown()
    {
    }

    void OnMouseUp()
    {
    }
}
