using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
// لن نستخدم MeshCollider أو Rigidbody أو تصادم جاهز
// [RequireComponent(typeof(MeshCollider), typeof(Rigidbody))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralBowlingPin : MonoBehaviour
{
    public enum PhysicsType { Solid, Rubber, Glass }

    [Header("Pin Profile")]
    public int radialSegments = 24;
    public int heightSegments = 32;
    public float pinHeight = 1.2f;
    public float maxRadius = 0.3f;
    public float bottomRadius = 0.15f;
    public float neckRadius = 0.05f;

    [Header("Mass-Spring Physics")]
    public float pointMass = 0.2f;
    public float springStiffness = 200f;
    public float springDamping = 2f;
    public Tetrahedralizer tetrahedralizer; // اربطه في الـ Inspector أو أنشئه أوتوماتيكياً

    [HideInInspector]
    public MassSpring massSpring;


    [Header("Rendering Materials")]
    public PhysicsType physicsType = PhysicsType.Solid;
    public Material solidVisual;
    public Material rubberVisual;
    public Material glassVisual;

    [Header("Glass Break Effects")]
    public AudioClip glassBreakClip;
    public float breakSoundThreshold = 5f;
    public GameObject glassShatterPrefab; // تأكد من تعيينه في الـInspector

    [HideInInspector]
    public Mesh pinMesh; // سيتم بناءه ديناميكياً ويُستخدم لاحقًا في تصادم Narrow Phase

    void Start()
    {
        BuildMesh();
        SetupVisual();

        if (tetrahedralizer == null)
            tetrahedralizer = gameObject.AddComponent<Tetrahedralizer>();

        // لا حاجة لهذه بعد الآن:
        // tetrahedralizer.radius = maxRadius;

        tetrahedralizer.internalResolution = 10;
        tetrahedralizer.Initialize(pinMesh);

        massSpring = new MassSpring();
        foreach (var (p0, p1, p2, p3) in tetrahedralizer.tetrahedra)
        {
            Vector3 wp0 = transform.TransformPoint(p0);
            Vector3 wp1 = transform.TransformPoint(p1);
            Vector3 wp2 = transform.TransformPoint(p2);
            Vector3 wp3 = transform.TransformPoint(p3);

            massSpring.AddTetrahedron(wp0, wp1, wp2, wp3, pointMass, springStiffness, springDamping);
        }
    }


    void Update()
    {
        if (!Application.isPlaying)
        {
            BuildMesh();
            SetupVisual();
        }
    }

    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            BuildMesh();
            SetupVisual();
        }
    }
    void FixedUpdate()
    {
        if (Application.isPlaying && massSpring != null)
        {
            massSpring.Simulate(Time.fixedDeltaTime);
            UpdateMeshVertices();
        }
    }

    void UpdateMeshVertices()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        var verts = mesh.vertices;
        var updatedPositions = massSpring.GetPositions();

        if (updatedPositions.Count != verts.Length) return; // تأكد أن الأعداد تطابق

        for (int i = 0; i < verts.Length; i++)
        {
            // تحويل من إحداثيات عالمية إلى محلية قبل التحديث
            verts[i] = transform.InverseTransformPoint(updatedPositions[i]);
        }

        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }


    public void ApplyImpact(Vector3 position, Vector3 direction, float magnitude)
    {
        massSpring.ApplyImpulse(position, direction, magnitude, influenceRadius: 0.3f);

        if (physicsType == PhysicsType.Glass && magnitude > breakSoundThreshold)
        {
            if (glassBreakClip)
                AudioSource.PlayClipAtPoint(glassBreakClip, transform.position);
            if (glassShatterPrefab)
                Instantiate(glassShatterPrefab, transform.position, transform.rotation);

            Destroy(gameObject); // أو افصل الـ Mesh وأضف جسيمات
        }
    }

    void BuildMesh()
    {
        pinMesh = new Mesh { name = "ProceduralBowlingPin" };
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector2[] profile = new Vector2[heightSegments + 1];
        for (int i = 0; i <= heightSegments; i++)
        {
            float t = (float)i / heightSegments;
            float y = t * pinHeight;
            float r = Mathf.Lerp(bottomRadius, maxRadius, Mathf.Sin(t * Mathf.PI));
            r = Mathf.Lerp(r, neckRadius, t * t);
            profile[i] = new Vector2(y, r);
        }

        int rings = radialSegments + 1;
        for (int i = 0; i <= heightSegments; i++)
        {
            Vector2 p = profile[i];
            for (int j = 0; j < rings; j++)
            {
                float theta = (float)j / radialSegments * Mathf.PI * 2f;
                verts.Add(new Vector3(Mathf.Cos(theta) * p.y, p.x, Mathf.Sin(theta) * p.y));
                uvs.Add(new Vector2((float)j / radialSegments, p.x / pinHeight));
            }
        }

        for (int i = 0; i < heightSegments; i++)
        {
            int row = i * rings;
            int next = (i + 1) * rings;
            for (int j = 0; j < radialSegments; j++)
            {
                int a = row + j;
                int b = next + j;
                int c = a + 1;
                int d = b + 1;
                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(c); tris.Add(b); tris.Add(d);
            }
        }

        // غلق الأسفل
        int bottomCenter = verts.Count;
        verts.Add(new Vector3(0, 0, 0));
        uvs.Add(new Vector2(0.5f, 0f));
        for (int j = 0; j < radialSegments; j++)
        {
            tris.Add(bottomCenter);
            tris.Add(j + 1);
            tris.Add(j);
        }

        // غلق الأعلى
        int topCenter = verts.Count;
        verts.Add(new Vector3(0, pinHeight, 0));
        uvs.Add(new Vector2(0.5f, 1f));
        int baseRing = heightSegments * rings;
        for (int j = 0; j < radialSegments; j++)
        {
            tris.Add(topCenter);
            tris.Add(baseRing + j);
            tris.Add(baseRing + j + 1);
        }

        pinMesh.SetVertices(verts);
        pinMesh.SetTriangles(tris, 0);
        pinMesh.SetUVs(0, uvs);
        pinMesh.RecalculateNormals();
        pinMesh.RecalculateBounds();

        GetComponent<MeshFilter>().sharedMesh = pinMesh;
    }

    void SetupVisual()
    {
        var rend = GetComponent<MeshRenderer>();
        switch (physicsType)
        {
            case PhysicsType.Rubber: rend.sharedMaterial = rubberVisual; break;
            case PhysicsType.Glass: rend.sharedMaterial = glassVisual; break;
            default: rend.sharedMaterial = solidVisual; break;
        }
    }

}
