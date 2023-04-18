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

        public Mesh(Vector3 position, Material material, Aabb aabb)
        {
            Position = position;
            Material = material;
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
    }
}
