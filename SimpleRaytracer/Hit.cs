using System.Numerics;

namespace SimpleRaytracer
{
    public struct Hit
    {
        public IHittable Target { get; }
        public Vector3 Position { get; }
        public Vector3 Normal { get; }
        public float Distance { get; }
        public bool IsFrontFace { get; }

        public Hit(Ray ray, IHittable target, Vector3 position, Vector3 normal, float distance)
        {
            Target = target;
            Position = position;
            Normal = normal;
            Distance = distance;
            IsFrontFace = Vector3.Dot(Normal, ray.Direction) < 0;
        }
    }
}
