using System.Numerics;

namespace SimpleRaytracer
{
    public static class MeshGenerator
    {
        public static Mesh LoadPlaneData(Vector3 position, Material material, float size, ref List<Triangle> triangles)
        {
            var bl = new Vector3(-0.5f, 0, -0.5f) * size;
            var br = new Vector3(0.5f, 0, -0.5f) * size;
            var tl = new Vector3(-0.5f, 0, 0.5f) * size;
            var tr = new Vector3(0.5f, 0, 0.5f) * size;

            var mesh = new Mesh(position, material)
            {
                SkipBoundingBoxTest = true
            };
            mesh.Aabb = new Aabb(bl, tr);

            mesh.arrayOffset = triangles.Count;
            mesh.triangleCount = 2;

            triangles.Add(new(bl, tr, br));
            triangles.Add(new(bl, tl, tr));

            return mesh;
        }
    }
}
