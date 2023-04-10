using System.Numerics;

namespace SimpleRaytracer
{
    public class Scene
    {
        public Camera? Camera { get; set; }
        public GpuSphere[] Objects { get; set; }
        public Mesh[] Meshes { get; set; }
        public Vector3 Ambient { get; set; }
    }
}
