namespace SimpleRaytracer
{
    public interface IHittable
    {
        Material Material { get; set; }
        bool TryGetRayHit(Ray ray, out Hit? hit);
    }
}