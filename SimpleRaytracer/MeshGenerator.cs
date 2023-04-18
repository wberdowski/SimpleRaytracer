using System.Numerics;

namespace SimpleRaytracer
{
    public static class MeshGenerator
    {
        public static (Mesh, Triangle[]) GetPlaneData(Vector3 position, Material material, float size = 1)
        {
            var bl = new Vector3(-0.5f, 0, -0.5f) * size;
            var br = new Vector3(0.5f, 0, -0.5f) * size;
            var tl = new Vector3(-0.5f, 0, 0.5f) * size;
            var tr = new Vector3(0.5f, 0, 0.5f) * size;

            var mesh = new Mesh(position, material, new Aabb(bl, tr));

            return (mesh, new Triangle[]
            {
                new Triangle(
                    bl,tr,br
                ),
                 new Triangle(
                    bl,tl,tr
                )
            });
        }
    }
}
