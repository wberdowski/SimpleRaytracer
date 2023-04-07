namespace SimpleRaytracer
{
    public class Scene
    {
        public Camera? Camera { get; set; }
        public List<IHittable> Objects { get; set; } = new();

        public Hit? GetClosestHit(Ray ray)
        {
            Hit? closestHit = null;

            foreach (var obj in Objects)
            {
                if (obj.TryGetRayHit(ray, out var hit) && (closestHit == null || hit.Value.Distance < closestHit.Value.Distance))
                {
                    closestHit = hit;
                }
            }

            return closestHit;
        }
    }
}
