using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralSphere : MonoBehaviour
{
    public int longitudeSegments = 24;
    public int latitudeSegments = 16;
    public float radius = 1f;

    private Mesh mesh;
    private Vector3[] vertices;

    void Start()
    {
        GenerateSphere();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            GenerateSphere();
        }
    }
#endif

    void GenerateSphere()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Sphere";
        GetComponent<MeshFilter>().sharedMesh = mesh;

        List<Vector3> verts = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int lat = 0; lat <= latitudeSegments; lat++)
        {
            float theta = Mathf.PI * lat / latitudeSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= longitudeSegments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / longitudeSegments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float x = sinTheta * cosPhi;
                float y = cosTheta;
                float z = sinTheta * sinPhi;

                verts.Add(new Vector3(x, y, z) * radius);
            }
        }

        for (int lat = 0; lat < latitudeSegments; lat++)
        {
            for (int lon = 0; lon < longitudeSegments; lon++)
            {
                int first = lat * (longitudeSegments + 1) + lon;
                int second = first + longitudeSegments + 1;

                triangles.Add(first);
                triangles.Add(second);
                triangles.Add(first + 1);

                triangles.Add(second);
                triangles.Add(second + 1);
                triangles.Add(first + 1);
            }
        }

        vertices = verts.ToArray();
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();
    }

    public void DeformVertex(int index, Vector3 offset)
    {
        if (index >= 0 && index < vertices.Length)
        {
            vertices[index] += offset;
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }
    }
}
