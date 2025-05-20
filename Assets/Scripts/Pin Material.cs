using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RealisticPinWithTopBall : MonoBehaviour
{
    public int segments = 24;
    public int verticalSegments = 8;
    public float height = 1.2f;
    public float topRadius = 0.2f;
    public float middleRadius = 0.25f;
    public float bottomRadius = 0.15f;
    public float topBallRadius = 0.1f;
    public float ballYOffset = 0.2f;

    private Mesh mesh;

    void Start()
    {
        GeneratePin();
    }

#if UNITY_EDITOR
    void Update()
    {
        if (!Application.isPlaying)
        {
            GeneratePin();
        }
    }
#endif

    void GeneratePin()
    {
        mesh = new Mesh();
        mesh.name = "Procedural Bowling Pin";
        GetComponent<MeshFilter>().sharedMesh = mesh;

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        for (int y = 0; y <= verticalSegments; y++)
        {
            float t = (float)y / verticalSegments;
            float radius = Mathf.Lerp(bottomRadius, middleRadius, Mathf.Sin(t * Mathf.PI));
            radius = Mathf.Lerp(radius, topRadius, t); // تدريج من المنتصف للأعلى

            float yPos = t * height;

            for (int i = 0; i <= segments; i++)
            {
                float angle = 2f * Mathf.PI * i / segments;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                verts.Add(new Vector3(x, yPos, z));
            }
        }

        int ringVerts = segments + 1;
        for (int y = 0; y < verticalSegments; y++)
        {
            for (int i = 0; i < segments; i++)
            {
                int current = y * ringVerts + i;
                int next = current + ringVerts;

                tris.Add(current);
                tris.Add(next);
                tris.Add(current + 1);

                tris.Add(next);
                tris.Add(next + 1);
                tris.Add(current + 1);
            }
        }

        Vector3 topBallCenter = new Vector3(0, height, 0);
        int baseIndex = verts.Count;
        for (int lat = 0; lat <= verticalSegments; lat++)
        {
            float theta = Mathf.PI * lat / verticalSegments;
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);

            for (int lon = 0; lon <= segments; lon++)
            {
                float phi = 2f * Mathf.PI * lon / segments;
                float sinPhi = Mathf.Sin(phi);
                float cosPhi = Mathf.Cos(phi);

                float x = sinTheta * cosPhi;
                float y = cosTheta;
                float z = sinTheta * sinPhi;

                Vector3 point = new Vector3(x, y, z) * topBallRadius + new Vector3(0, ballYOffset, 0);
                verts.Add(point + topBallCenter);
            }
        }

        for (int lat = 0; lat < verticalSegments; lat++)
        {
            for (int lon = 0; lon < segments; lon++)
            {
                int first = baseIndex + lat * (segments + 1) + lon;
                int second = first + segments + 1;

                tris.Add(first);
                tris.Add(second);
                tris.Add(first + 1);

                tris.Add(second);
                tris.Add(second + 1);
                tris.Add(first + 1);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
    }
}
