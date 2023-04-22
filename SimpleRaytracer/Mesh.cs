using ObjLoader.Loader.Loaders;
using System.Numerics;

namespace SimpleRaytracer
{
    public struct Mesh
    {
        public Vector3 Position;
        public Material Material;
        public Aabb Aabb = default;
        public GpuBool SkipBoundingBoxTest = false;
        public int triangleCount = default;
        public int arrayOffset = default;

        public Mesh(Vector3 position, Material material)
        {
            Position = position;
            Material = material;
        }

        public void LoadFromObj(string filepath, ref List<Triangle> triangles)
        {
            var objLoaderFactory = new ObjLoaderFactory();
            var objLoader = objLoaderFactory.Create();

            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;

            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            using (var fileStream = new FileStream(filepath, FileMode.Open))
            {
                var result = objLoader.Load(fileStream);
                var count = 0;

                foreach (var face in result.Groups[0].Faces)
                {
                    if (face.Count == 3)
                    {
                        var vectors = new Vector3[3];
                        var normals = new Vector3[3];

                        for (int i = 0; i < 3; i++)
                        {
                            var f = face[i];

                            var vertexPos = result.Vertices[f.VertexIndex - 1];
                            vectors[i] = new Vector3(vertexPos.X, vertexPos.Y, vertexPos.Z) + Position;

                            var normal = result.Vertices[f.VertexIndex - 1];
                            normals[i] = new Vector3(normal.X, normal.Y, normal.Z);

                            if (vectors[i].X < minX)
                            {
                                minX = vectors[i].X;
                            }

                            if (vectors[i].Y < minY)
                            {
                                minY = vectors[i].Y;
                            }

                            if (vectors[i].Z < minZ)
                            {
                                minZ = vectors[i].Z;
                            }

                            if (vectors[i].X > maxX)
                            {
                                maxX = vectors[i].X;
                            }

                            if (vectors[i].Y > maxY)
                            {
                                maxY = vectors[i].Y;
                            }

                            if (vectors[i].Z > maxZ)
                            {
                                maxZ = vectors[i].Z;
                            }

                            //result.Textures[v.TextureIndex - 1].X,
                            //result.Textures[v.TextureIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].X,
                            //result.Normals[v.NormalIndex - 1].Y,
                            //result.Normals[v.NormalIndex - 1].Z,
                        }

                        triangles.Add(new Triangle(
                            vectors[0], vectors[1], vectors[2],
                            normals[0], normals[1], normals[2]                            
                            ));
                        count++;
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

                Aabb = aabb;
                arrayOffset = triangles.Count - count;
                triangleCount = count;
            }
        }
    }
}
