namespace SimpleRaytracer
{
    public class Scene
    {
        public Camera? Camera { get; set; }
        public List<GpuSphere> Objects { get; set; } = new();
    }
}
