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
    }
}