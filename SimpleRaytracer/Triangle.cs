using System.Numerics;

namespace SimpleRaytracer
{
    public struct Triangle
    {
        const float Epsilon = 0.001f;
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }

        public bool IntersectRayTriangle(Ray ray, ref Hit hit)
        {
            var bidirectional = false;

            Vector3 ab = v1 - v0;
            Vector3 ac = v2 - v0;

            // Compute triangle normal. Can be precalculated or cached if
            // intersecting multiple segments against the same triangle
            Vector3 n = Vector3.Cross(ab, ac);

            // Compute denominator d. If d <= 0, segment is parallel to or points
            // away from triangle, so exit early
            float d = Vector3.Dot(-ray.Direction, n);
            if (d <= 0.0f) return false;

            // Compute intersection t value of pq with plane of triangle. A ray
            // intersects iff 0 <= t. Segment intersects iff 0 <= t <= 1. Delay
            // dividing by d until intersection has been found to pierce triangle
            Vector3 ap = ray.Origin - v0;
            float t = Vector3.Dot(ap, n);
            if ((t < 0.0f) && (!bidirectional)) return false;
            //if (t > d) return null; // For segment; exclude this code line for a ray test

            // Compute barycentric coordinate components and test if within bounds
            Vector3 e = Vector3.Cross(-ray.Direction, ap);
            float v = Vector3.Dot(ac, e);
            if (v < 0.0f || v > d) return false;

            float w = -Vector3.Dot(ab, e);
            if (w < 0.0f || v + w > d) return false;

            // Segment/ray intersects triangle. Perform delayed division and
            // compute the last barycentric coordinate component
            float ood = 1.0f / d;
            t *= ood;
            v *= ood;
            w *= ood;
           // float u = 1.0f - v - w;

            hit.position = ray.Origin + t * ray.Direction;
            hit.distance = t;
            //hit.barycentricCoordinate = new Vector3(u, v, w);
            hit.normal = Vector3.Normalize(n);

            return true;
        }
    }
}
