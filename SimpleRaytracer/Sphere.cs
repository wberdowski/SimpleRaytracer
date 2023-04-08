using ILGPU.Algorithms;
using System.Numerics;

namespace SimpleRaytracer
{
    public class Sphere : Transform, IHittable
    {
        public Material Material { get; set; }
        public float Radius { get; set; }

        public Sphere()
        {
        }

        public Sphere(Vector3 position, float radius) : base(position)
        {
            Radius = radius;
        }

        public Sphere(Vector3 position, Quaternion rotation, float radius) : base(position, rotation)
        {
            Radius = radius;
        }

        public bool TryGetRayHit(Ray ray, out Hit? hit)
        {
            var offset = Position - ray.Origin;
            var projection = Vector3.Dot(offset, ray.Direction);
            var distanceToCenter = offset.Length();
            var distanceToIntersection = (float)XMath.Sqrt(distanceToCenter * distanceToCenter - projection * projection);

            if (distanceToIntersection > Radius)
            {
                hit = null;
                return false; // The ray doesn't intersect the sphere
            }

            var distanceToHit = (float)XMath.Sqrt(Radius * Radius - distanceToIntersection * distanceToIntersection);
            var hitDistance1 = projection - distanceToHit;
            var hitDistance2 = projection + distanceToHit;

            if (hitDistance1 <= 0 && hitDistance2 <= 0)
            {
                hit = null;
                return false; // The sphere is behind the ray's origin
            }

            // TODO: Subtract epsilon
            var dist = XMath.Min(hitDistance1, hitDistance2) - 0.001f;
            var hitPos = ray.Origin + ray.Direction * dist;
            var normal = Vector3.Normalize(hitPos - Position);

            hit = new Hit(Material, hitPos, normal, dist);

            return true; // The ray intersects the sphere
        }
    }
}
