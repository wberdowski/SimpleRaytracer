using System.Numerics;

namespace SimpleRaytracer
{
    public struct Triangle
    {
        private const float Epsilon = float.Epsilon;
        public Vector3 v0;
        public Vector3 v1;
        public Vector3 v2;
        public Vector3 n0 = Vector3.Zero;
        public Vector3 n1 = Vector3.Zero;
        public Vector3 n2 = Vector3.Zero;

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            this.v0 = v0;
            this.v1 = v1;
            this.v2 = v2;
        }

        public Triangle(Vector3 v0, Vector3 v1, Vector3 v2, Vector3 n0, Vector3 n1, Vector3 n2) : this(v0, v1, v2)
        {
            this.n0 = n0;
            this.n1 = n1;
            this.n2 = n2;
        }

        public bool IntersectRayTriangle(Ray ray, ref Hit hit)
        {
            var ab = v1 - v0;
            var ac = v2 - v0;

            var n = Vector3.Cross(ab, ac);

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

            var x = v / det;
            var y = w / det;

            var l1 = Vector3.Lerp(n0, n2, y * 2f);
            var l2 = Vector3.Lerp(n0, n1, x * 2f);
            var norm = Vector3.Normalize(Vector3.Lerp(l1, l2, 0.5f));

            //hit.material = new Material(norm, 0);
            //hit.material = new Material(new Vector3(v / det, w / det, 0), 0.5f);
            hit.position = ray.Origin + t * ray.Direction;
            hit.distance = t;
            hit.normal = norm;

            return true;
        }
    }
}
