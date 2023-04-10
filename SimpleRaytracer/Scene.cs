using System.Numerics;

namespace SimpleRaytracer
{
    public class Scene
    {
        public Camera? Camera { get; set; }
        public GpuSphere[] Objects { get; set; } = new GpuSphere[0];
        public Mesh[] Meshes { get; set; } = new Mesh[0];
        public Vector3 Ambient { get; set; }
        public Triangle[] Triangles { get; set; } = new Triangle[0];
    }
}
