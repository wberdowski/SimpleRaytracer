using ILGPU.Algorithms;
using ObjLoader.Loader.Data;
using ObjLoader.Loader.Loaders;
using System.Numerics;

namespace SimpleRaytracer
{
    public struct Mesh
    {
        public Vector3 Position;
        public Material Material;
        public Aabb Aabb;
        public int TriangleCount = 0;

        public Mesh(Vector3 position, Material material, Vector3[] vertices, Aabb aabb)
        {
            Position = position;
            Material = material;
            //Vertices = vertices;
            Aabb = aabb;
        }

        public static (Mesh, Triangle[]) LoadFromObj(string filepath)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();
            var mesh = new Mesh();

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            using (var fileStream = new FileStream(filepath, FileMode.Open))
            {
                var result = objLoader.Load(fileStream);
                var triangles = new List<Triangle>();

                foreach (var face in result.Groups[0].Faces)
                {
                    if (face.Count == 3)
                    {
                        var vectors = new Vector3[3];

                        for (int i = 0; i < 3; i++)
                        {
                            var f = face[i];

                            var vertexPos = result.Vertices[f.VertexIndex - 1];
                            vectors[i] = new Vector3(vertexPos.X, vertexPos.Y, vertexPos.Z);

                            if (vertexPos.X < minX)
                            {
                                minX = vertexPos.X;
                            }

                            if (vertexPos.Y < minY)
                            {
                                minY = vertexPos.Y;
                            }

                            if (vertexPos.Z < minZ)
                            {
                                minZ = vertexPos.Z;
                            }

                            if (vertexPos.X > maxX)
                            {
                                maxX = vertexPos.X;
                            }

                            if (vertexPos.Y > maxY)
                            {
                                maxY = vertexPos.Y;
                            }

                            if (vertexPos.Z > maxZ)
                            {
                                maxZ = vertexPos.Z;
                            }

                            //result.Textures[v.TextureIndex - 1].X,
                            //result.Textures[v.TextureIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].X,
                            //result.Normals[v.NormalIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].Z,
                        }

                        triangles.Add(new Triangle(vectors[0], vectors[1], vectors[2]));
                    }
                    else
                    {
                        throw new Exception("Unsupported face vertex count.");
                    }
                }

                var aabb = new Aabb()
                {
                    Min = new Vector3(minX, minY, minZ),
                    Max = new Vector3(maxX, maxY, maxZ)
                };

                mesh.Aabb = aabb;
                mesh.TriangleCount = triangles.Count;

                return (mesh, triangles.ToArray());
            }
        }

        public bool GetRayAabbIntersection(Ray ray, ref Hit hit)
        {
            Vector3 dirfrac = Vector3.One / ray.Direction;

            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            float t1 = (Aabb.Min.X - ray.Origin.X) * dirfrac.X;
            float t2 = (Aabb.Max.X - ray.Origin.X) * dirfrac.X;
            float t3 = (Aabb.Min.Y - ray.Origin.Y) * dirfrac.Y;
            float t4 = (Aabb.Max.Y - ray.Origin.Y) * dirfrac.Y;
            float t5 = (Aabb.Min.Z - ray.Origin.Z) * dirfrac.Z;
            float t6 = (Aabb.Max.Z - ray.Origin.Z) * dirfrac.Z;

            float tmin = XMath.Max(XMath.Max(XMath.Min(t1, t2), XMath.Min(t3, t4)), XMath.Min(t5, t6));
            float tmax = XMath.Min(XMath.Min(XMath.Max(t1, t2), XMath.Max(t3, t4)), XMath.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
            if (tmin > tmax || tmax < 0)
            {
                return false;
            }

            // TODO: Subtract epsilon
            var dist = tmin - 0.001f;
            var hitPos = ray.Origin + ray.Direction * dist;
            var normal = Vector3.Normalize(hitPos - Position); // TODO

            hit = new Hit(Material, hitPos, normal, dist);

            return true;
        }

        public bool TestAabb(Ray ray, out float dist)
        {
            Vector3 dirfrac = Vector3.One / ray.Direction;

            // lb is the corner of AABB with minimal coordinates - left bottom, rt is maximal corner
            // r.org is origin of ray
            float t1 = (Aabb.Min.X - ray.Origin.X) * dirfrac.X;
            float t2 = (Aabb.Max.X - ray.Origin.X) * dirfrac.X;
            float t3 = (Aabb.Min.Y - ray.Origin.Y) * dirfrac.Y;
            float t4 = (Aabb.Max.Y - ray.Origin.Y) * dirfrac.Y;
            float t5 = (Aabb.Min.Z - ray.Origin.Z) * dirfrac.Z;
            float t6 = (Aabb.Max.Z - ray.Origin.Z) * dirfrac.Z;

            float tmin = XMath.Max(XMath.Max(XMath.Min(t1, t2), XMath.Min(t3, t4)), XMath.Min(t5, t6));
            float tmax = XMath.Min(XMath.Min(XMath.Max(t1, t2), XMath.Max(t3, t4)), XMath.Max(t5, t6));

            // if tmax < 0, ray (line) is intersecting AABB, but the whole AABB is behind us
            if (tmin > tmax || tmax < 0)
            {
                dist = float.PositiveInfinity;
                return false;
            }

            dist = tmin;

            return true;
        }
    }
}
