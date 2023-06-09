﻿using ILGPU.Algorithms;
using System.Numerics;

namespace SimpleRaytracer
{
    public struct GpuSphere
    {
        public Vector3 position;
        public Material material;
        public float radius;

        public GpuSphere(Vector3 position, Material material, float radius)
        {
            this.position = position;
            this.material = material;
            this.radius = radius;
        }

        public bool TryGetRayHit(Ray ray, ref Hit hit)
        {
            var offset = position - ray.Origin;
            var projection = Vector3.Dot(offset, ray.Direction);
            var distanceToCenter = offset.Length();
            var distanceToIntersection = (float)XMath.Sqrt(distanceToCenter * distanceToCenter - projection * projection);

            if (distanceToIntersection > radius)
            {
                return false; // The ray doesn't intersect the sphere
            }

            var distanceToHit = (float)XMath.Sqrt(radius * radius - distanceToIntersection * distanceToIntersection);
            var hitDistance1 = projection - distanceToHit;
            var hitDistance2 = projection + distanceToHit;

            if (hitDistance1 <= 0 && hitDistance2 <= 0)
            {
                return false; // The sphere is behind the ray's origin
            }

            // TODO: Subtract epsilon
            var dist = XMath.Min(hitDistance1, hitDistance2) - 0.001f;
            var hitPos = ray.Origin + ray.Direction * dist;
            var normal = Vector3.Normalize(hitPos - position);

            hit = new Hit(material, hitPos, normal, dist);

            return true; // The ray intersects the sphere
        }
    }
}
