using ILGPU.Algorithms;
using ObjLoader.Loader.Data;
using ObjLoader.Loader.Loaders;
using System.Numerics;

namespace SimpleRaytracer
{
    public struct Mesh
    {
        public Vector3 position;
        public Material material;
        //public Vector3[] Vertices { get; set; }
        public Aabb Aabb { get; private set; }

        public static Mesh LoadFromObj(string filepath)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            var mesh = new Mesh();

            using (var fileStream = new FileStream(filepath, FileMode.Open))
            {
                var result = objLoader.Load(fileStream);
                var vertices = new List<Vector3>();

                foreach (var face in result.Groups[0].Faces)
                {
                    if (face.Count == 3)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var f = face[i];

                            var vertexPos = result.Vertices[f.VertexIndex - 1];
                            vertices.Add(new Vector3(vertexPos.X, vertexPos.Y, vertexPos.Z));

                            //result.Textures[v.TextureIndex - 1].X,
                            //result.Textures[v.TextureIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].X,
                            //result.Normals[v.NormalIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].Z,
                        }
                    }
                    else
                    {
                        throw new Exception("Unsupported face vertex count.");
                    }
                }


                var aabb = new Aabb()
                {
                    Min = new Vector3(vertices.MinBy(v => v.X).X, vertices.MinBy(v => v.Y).Y, vertices.MinBy(v => v.Z).Z),
                    Max = new Vector3(vertices.MaxBy(v => v.X).X, vertices.MaxBy(v => v.Y).Y, vertices.MaxBy(v => v.Z).Z)
                };

                //mesh.Vertices = vertices.ToArray();
                mesh.Aabb = aabb;
            }

            return mesh;
        }

        public bool RayAABBIntersect_OLD(Ray ray, out Hit hit)
        {
            Vector3 invRayDir = Vector3.One / ray.Direction;

            float t1 = (Aabb.Min.X - ray.Origin.X) * invRayDir.X;
            float t2 = (Aabb.Max.X - ray.Origin.X) * invRayDir.X;
            float t3 = (Aabb.Min.Y - ray.Origin.Y) * invRayDir.Y;
            float t4 = (Aabb.Max.Y - ray.Origin.Y) * invRayDir.Y;
            float t5 = (Aabb.Min.Z - ray.Origin.Z) * invRayDir.Z;
            float t6 = (Aabb.Max.Z - ray.Origin.Z) * invRayDir.Z;

            float tMin = XMath.Max(XMath.Max(XMath.Min(t1, t2), XMath.Min(t3, t4)), XMath.Min(t5, t6));
            float tMax = XMath.Min(XMath.Min(XMath.Max(t1, t2), XMath.Max(t3, t4)), XMath.Max(t5, t6));

            // Use an early exit condition
            if (tMin > tMax)
            {
                hit = default;
                return false;
            }

            // TODO: Subtract epsilon
            var dist = XMath.Min(tMin, tMax) - 0.001f;
            var hitPos = ray.Origin + ray.Direction * dist;
            var normal = Vector3.Normalize(hitPos - position); // TODO

            hit = new Hit(material, hitPos, normal, dist);

            return true;
        }

        public bool RayAABBIntersect(Ray ray, out Hit hit)
        {
            var invRayDir = Vector3.One / ray.Direction;

            var t0 = (Aabb.Min - ray.Origin) * invRayDir;
            var t1 = (Aabb.Max - ray.Origin) * invRayDir;
            var tmin = Vector3.Min(t0, t1);
            var tmax = Vector3.Max(t0, t1);

            var isHit = MaxComponent(tmin) <= MaxComponent(tmax);

            if (!isHit)
            {
                hit = default;
                return false;
            }

            // TODO: Subtract epsilon
            var dist = Vector3.Distance(ray.Origin, tmin) - 0.001f;
            var hitPos = ray.Origin + ray.Direction * dist;
            var normal = Vector3.Normalize(hitPos - position); // TODO

            hit = new Hit(material, hitPos, normal, dist);

            return true;
        }

        private float MaxComponent(Vector3 v)
        {
            return XMath.Max(XMath.Max(v.X, v.Y), v.Z);
        }
    }
}
