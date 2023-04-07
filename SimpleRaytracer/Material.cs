using System.Numerics;

namespace SimpleRaytracer
{
    public class Material
    {
        public Vector3 Albedo { get; set; }
        public Vector3 Emission { get; set; }
        public bool IsLightSource => Emission != Vector3.Zero;

        public Material(Vector3 albedo)
        {
            Albedo = albedo;
        }

        public Material(Vector3 albedo, Vector3 emission)
        {
            Albedo = albedo;
            Emission = emission;
        }
    }
}
