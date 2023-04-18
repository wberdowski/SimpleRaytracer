using ILGPU.Algorithms;
using System.Numerics;

namespace SimpleRaytracer
{
    public struct Aabb
    {
        public Vector3 Min { get; set; }
        public Vector3 Max { get; set; }

        public Aabb(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public bool TestAabb(Ray ray, out float dist)
        {
            Vector3 dirfrac = Vector3.One / ray.Direction;

            float t1 = (Min.X - ray.Origin.X) * dirfrac.X;
            float t2 = (Max.X - ray.Origin.X) * dirfrac.X;
            float t3 = (Min.Y - ray.Origin.Y) * dirfrac.Y;
            float t4 = (Max.Y - ray.Origin.Y) * dirfrac.Y;
            float t5 = (Min.Z - ray.Origin.Z) * dirfrac.Z;
            float t6 = (Max.Z - ray.Origin.Z) * dirfrac.Z;

            float tmin = XMath.Max(XMath.Max(XMath.Min(t1, t2), XMath.Min(t3, t4)), XMath.Min(t5, t6));
            float tmax = XMath.Min(XMath.Min(XMath.Max(t1, t2), XMath.Max(t3, t4)), XMath.Max(t5, t6));

            if (tmin > tmax || tmax < 0)
            {
                dist = float.PositiveInfinity;
                return false;
            }

            dist = tmin;

            return true;
        }
    }
}