using System.Numerics;

namespace SimpleRaytracer
{
    public struct Triangle
    {
        private const float Epsilon = float.Epsilon;
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 n;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;

            var ab = v1 - v0;
            var ac = v2 - v0;

            n = Vector3.Cross(ab, ac);
        }

        public bool IntersectRayTriangle(Ray ray, ref Hit hit)
        {
            var det = Vector3.Dot(-ray.Direction, n);

            if (det <= 0.0f)
            {
                return false;
            }

            var ap = ray.Origin - v0;
            var t = Vector3.Dot(ap, n);

            if (t < Epsilon)
            {
                return false;
            }

            var e = Vector3.Cross(-ray.Direction, ap);
            var v = Vector3.Dot(v2 - v0, e);

            if (v < 0.0f || v > det)
            {
                return false;
            }

            var w = -Vector3.Dot(v1 - v0, e);

            if (w < 0.0f || v + w > det)
            {
                return false;
            }

            t /= det;

            hit.position = ray.Origin + t * ray.Direction;
            hit.distance = t;
            hit.normal = Vector3.Normalize(n);

            return true;
        }
    }
}
