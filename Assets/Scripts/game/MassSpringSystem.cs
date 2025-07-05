using UnityEngine;
using System.Collections.Generic;

public class MassSpring
{
    public class MassPoint
    {
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 Force;
        public float Mass;
        public bool IsFixed;

        public MassPoint(Vector3 position, float mass, bool isFixed = false)
        {
            Position = position;
            Velocity = Vector3.zero;
            Force = Vector3.zero;
            Mass = mass;
            IsFixed = isFixed;
        }
    }

    public class Spring
    {
        public int PointA;
        public int PointB;
        public float RestLength;
        public float Stiffness;
        public float Damping;

        public Spring(int a, int b, float restLength, float stiffness, float damping)
        {
            PointA = a;
            PointB = b;
            RestLength = restLength;
            Stiffness = stiffness;
            Damping = damping;
        }
    }

    public List<MassPoint> Points = new();
    public List<Spring> Springs = new();

    private Dictionary<Vector3, int> pointIndexMap = new(); // لتفادي تكرار النقاط

    public int AddOrGetPoint(Vector3 position, float mass, bool isFixed = false)
    {
        if (pointIndexMap.TryGetValue(position, out int index))
        {
            return index;
        }

        index = Points.Count;
        Points.Add(new MassPoint(position, mass, isFixed));
        pointIndexMap[position] = index;
        return index;
    }

    public void AddMassPoint(Vector3 position, float mass, bool isFixed = false)
    {
        AddOrGetPoint(position, mass, isFixed); // استدعاء الطريقة الذكية
    }

    public void AddSpring(int indexA, int indexB, float stiffness, float damping)
    {
        float restLength = Vector3.Distance(Points[indexA].Position, Points[indexB].Position);
        Springs.Add(new Spring(indexA, indexB, restLength, stiffness, damping));
    }

    public void AddTetrahedron(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float mass, float stiffness, float damping)
    {
        int i0 = AddOrGetPoint(p0, mass);
        int i1 = AddOrGetPoint(p1, mass);
        int i2 = AddOrGetPoint(p2, mass);
        int i3 = AddOrGetPoint(p3, mass);

        AddSpring(i0, i1, stiffness, damping);
        AddSpring(i0, i2, stiffness, damping);
        AddSpring(i0, i3, stiffness, damping);
        AddSpring(i1, i2, stiffness, damping);
        AddSpring(i2, i3, stiffness, damping);
        AddSpring(i3, i1, stiffness, damping);
    }

    public void Simulate(float deltaTime)
    {
        foreach (var point in Points)
            point.Force = Vector3.zero;

        foreach (var spring in Springs)
        {
            MassPoint pA = Points[spring.PointA];
            MassPoint pB = Points[spring.PointB];

            Vector3 dir = pB.Position - pA.Position;
            float currentLength = dir.magnitude;
            if (currentLength == 0) continue;
            Vector3 normalized = dir / currentLength;

            float displacement = currentLength - spring.RestLength;
            Vector3 springForce = spring.Stiffness * displacement * normalized;

            Vector3 relativeVelocity = pB.Velocity - pA.Velocity;
            Vector3 dampingForce = spring.Damping * Vector3.Dot(relativeVelocity, normalized) * normalized;

            Vector3 totalForce = springForce + dampingForce;

            if (!pA.IsFixed) pA.Force += totalForce;
            if (!pB.IsFixed) pB.Force -= totalForce;
        }

        foreach (var point in Points)
        {
            if (point.IsFixed) continue;
            Vector3 acceleration = point.Force / point.Mass;
            point.Velocity += acceleration * deltaTime;
            point.Position += point.Velocity * deltaTime;
        }
    }

    public List<Vector3> GetPositions()
    {
        List<Vector3> positions = new();
        foreach (var point in Points)
            positions.Add(point.Position);
        return positions;
    }

    public void ApplyImpulse(Vector3 worldPosition, Vector3 forceDirection, float forceMagnitude, float influenceRadius)
    {
        for (int i = 0; i < Points.Count; i++)
        {
            float dist = Vector3.Distance(Points[i].Position, worldPosition);
            if (dist < influenceRadius && !Points[i].IsFixed)
            {
                float factor = 1f - (dist / influenceRadius);
                Vector3 impulse = forceDirection.normalized * forceMagnitude * factor;
                Points[i].Velocity += impulse / Points[i].Mass;
            }
        }
    }
}
