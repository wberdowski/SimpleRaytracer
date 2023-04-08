using System.Numerics;

namespace SimpleRaytracer
{
    public struct Material
    {
        public Vector3 Albedo { get; set; } = Vector3.Zero;
        public Vector3 Emission { get; set; } = Vector3.Zero;
        public float Smoothness { get; set; } = 0;

        public Material(Vector3 albedo)
        {
            Albedo = albedo;
            Emission = Vector3.Zero;
        }

        public Material(Vector3 albedo, float smoothness)
        {
            Albedo = albedo;
            Smoothness = smoothness;
        }

        public Material(Vector3 albedo, Vector3 emission)
        {
            Albedo = albedo;
            Emission = emission;
        }
    }
}
