using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteInEditMode]
public class Tetrahedralizer : MonoBehaviour
{
    [Header("Settings")]
    public int internalResolution = 10;

    [HideInInspector]
    public List<Vector3> internalPoints = new();
    [HideInInspector]
    public List<(Vector3, Vector3, Vector3, Vector3)> tetrahedra = new();

    private Mesh mesh;
    private MeshCollider meshCollider;

    void OnValidate()
    {
        TryInitialize();
        GenerateInternalPoints();
        GenerateTetrahedra();
    }

    public void Initialize(Mesh targetMesh)
    {
        mesh = targetMesh;
        TryInitialize();
        GenerateInternalPoints();
        GenerateTetrahedra();
    }

    private void TryInitialize()
    {
        if (mesh == null)
        {
            var mf = GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
        }

        if (meshCollider == null)
        {
            meshCollider = GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = gameObject.AddComponent<MeshCollider>();
        }

        meshCollider.sharedMesh = mesh;
        meshCollider.convex = true;
    }

    void GenerateInternalPoints()
    {
        internalPoints.Clear();
        tetrahedra.Clear();

        if (mesh == null || meshCollider == null) return;

        var bounds = mesh.bounds;
        Vector3 min = bounds.min;
        Vector3 max = bounds.max;

        Vector3 size = max - min;
        float step = Mathf.Max(size.x, size.y, size.z) / internalResolution;

        for (float x = min.x; x <= max.x; x += step)
        {
            for (float y = min.y; y <= max.y; y += step)
            {
                for (float z = min.z; z <= max.z; z += step)
                {
                    Vector3 localPoint = new Vector3(x, y, z);
                    Vector3 worldPoint = transform.TransformPoint(localPoint);

                    if (PointInsideMesh(worldPoint))
                    {
                        internalPoints.Add(transform.InverseTransformPoint(worldPoint));
                    }
                }
            }
        }
    }

    bool PointInsideMesh(Vector3 worldPoint)
    {
        return Physics.OverlapSphere(worldPoint, 0.01f)
            .Any(c => c == meshCollider);
    }

    void GenerateTetrahedra()
    {
        if (mesh == null) return;

        Vector3[] vertices = mesh.vertices;
        foreach (var internalPt in internalPoints)
        {
            var nearestSurfacePoints = vertices
                .OrderBy(v => (v - internalPt).sqrMagnitude)
                .Take(3)
                .ToList();

            if (nearestSurfacePoints.Count == 3)
            {
                tetrahedra.Add((internalPt, nearestSurfacePoints[0], nearestSurfacePoints[1], nearestSurfacePoints[2]));
            }
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        foreach (var pt in internalPoints)
            Gizmos.DrawSphere(transform.TransformPoint(pt), 0.015f);

        Gizmos.color = Color.cyan;
        foreach (var (p0, p1, p2, p3) in tetrahedra)
        {
            Vector3 wp0 = transform.TransformPoint(p0);
            Vector3 wp1 = transform.TransformPoint(p1);
            Vector3 wp2 = transform.TransformPoint(p2);
            Vector3 wp3 = transform.TransformPoint(p3);

            Gizmos.DrawLine(wp0, wp1);
            Gizmos.DrawLine(wp0, wp2);
            Gizmos.DrawLine(wp0, wp3);
            Gizmos.DrawLine(wp1, wp2);
            Gizmos.DrawLine(wp2, wp3);
            Gizmos.DrawLine(wp3, wp1);
        }
    }
}
