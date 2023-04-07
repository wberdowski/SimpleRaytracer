using System.Numerics;

namespace SimpleRaytracer
{
    public struct Hit
    {
        public Material material;
        public Vector3 position;
        public Vector3 normal;
        public float distance;

        public Hit(Material material, Vector3 position, Vector3 normal, float distance)
        {
            this.material = material;
            this.position = position;
            this.normal = normal;
            this.distance = distance;
        }
    }
}
