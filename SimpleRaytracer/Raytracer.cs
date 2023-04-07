using System.Drawing;
using System.Numerics;

namespace SimpleRaytracer
{
    public class Raytracer
    {
        static Random _random = new Random();

        public Bitmap Raytrace(Scene scene, Size resolution)
        {
            var sampleCount = 50;
            var bounces = 5;
            var cam = scene.Camera;

            var bmp = new Bitmap(resolution.Width, resolution.Height);

            var planeHeight = cam.NearClipPlane * (float)Math.Tan(cam.FieldOfView * 0.5f * (Math.PI / 180)) * 2;
            var planeWidth = planeHeight * cam.Aspect;

            var bottomLeft = new Vector3(-planeWidth / 2, -planeHeight / 2, cam.NearClipPlane);

            Parallel.For(0, resolution.Height, (y) =>
            {
                for (int x = 0; x < resolution.Width; x++)
                {
                    var normalizedX = x / (resolution.Width - 1f);
                    var normalizedY = y / (resolution.Height - 1f);

                    Vector3 pixelColor = Vector3.Zero;

                    for (int i = 0; i < sampleCount; i++)
                    {
                        var local = bottomLeft + new Vector3(
                            planeWidth * normalizedX + ((_random.NextSingle() - 0.5f) / (float)resolution.Width / 10),
                            planeHeight * normalizedY + ((_random.NextSingle() - 0.5f) / (float)resolution.Height / 10),
                            0);

                        var position = cam.Position + cam.Right * local.X + cam.Up * local.Y + cam.Forward * local.Z;
                        var dir = Vector3.Normalize(position - cam.Position);
                        var rayFromCamera = new Ray(cam.Position, dir);

                        pixelColor += TraceBounces(rayFromCamera, scene, bounces);
                    }

                    // Gamma correction

                    var scale = 1f / sampleCount;

                    pixelColor = new Vector3(
                        (float)Math.Sqrt(pixelColor.X * scale),
                        (float)Math.Sqrt(pixelColor.Y * scale),
                        (float)Math.Sqrt(pixelColor.Z * scale)
                    );

                    var outColor = Color.FromArgb(
                        (int)Math.Clamp(pixelColor.X * 255, 0, 255),
                        (int)Math.Clamp(pixelColor.Y * 255, 0, 255),
                        (int)Math.Clamp(pixelColor.Z * 255, 0, 255)
                    );

                    lock (bmp)
                    {
                        bmp.SetPixel(x, y, outColor);
                    }
                }
            });

            return bmp;
        }

        //private float RandomGaussian()
        //{
        //    var theta = 2 * Math.PI * (_random.Next() - int.MaxValue/2);
        //    var rho = Math.Sqrt(-2 * Math.Log(_random.Next() - int.MaxValue / 2));
        //    return (float)(rho * Math.Cos(theta));
        //}

        private Vector3 RandomNormalizedVector3()
        {
            return Vector3.Normalize(new Vector3(
                _random.Next() - int.MaxValue / 2,
                _random.Next() - int.MaxValue / 2,
                _random.Next() - int.MaxValue / 2
            ));
        }

        private Vector3 RandomVec3(float min, float max)
        {
            return new Vector3(
                _random.NextSingle() * (max - min) + min,
                _random.NextSingle() * (max - min) + min,
                _random.NextSingle() * (max - min) + min
                );
        }

        private Vector3 RandomInUnitSphere()
        {
            while (true)
            {
                var p = RandomVec3(-1, 1);
                if (p.LengthSquared() >= 1) continue;
                return p;
            }
        }

        private Vector3 TraceBounces(Ray ray, Scene scene, int bounces)
        {
            var rayColor = Vector3.One;
            var incomingLight = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                var closestHit = scene.GetClosestHit(ray);

                if (closestHit == null)
                {
                    break;
                }

                var randomVectorDir = RandomNormalizedVector3();

                if (Vector3.Dot(randomVectorDir, closestHit.Value.Normal) < 0f)
                {
                    randomVectorDir = -randomVectorDir;
                }

                ray = new Ray(closestHit.Value.Position, randomVectorDir);

                var emmitedLight = closestHit.Value.Target.Material.Emission;
                incomingLight += emmitedLight * rayColor;
                rayColor *= closestHit.Value.Target.Material.Albedo;
            }

            return incomingLight;

            //// Diffuse
            //var randomVectorDir = RandomNormalizedVector3();

            //// check if the random vector is in the same hemisphere as the normal
            //if (Vector3.Dot(randomVectorDir, closestHit.Value.Normal) < 0f)
            //{
            //    randomVectorDir = -randomVectorDir;
            //}

            //var diffuseRay = new Ray(closestHit.Value.Position, randomVectorDir);

            //Hit? closestHit2 = scene.GetClosestHit(diffuseRay);

            //if (closestHit2 == null)
            //{
            //    rayColor += GetAmbientColor();
            //    return;
            //}

            //if (closestHit2.Value.Target.Material.Emission != Vector3.Zero)
            //{
            //    rayColor += closestHit2.Value.Target.Material.Emission * closestHit.Value.Target.Material.Albedo;
            //}

            // Specular
            //var reflectionRay = new Ray(closestHit.Value.Position, Vector3.Reflect(ray.Direction, closestHit.Value.Normal));

            //foreach (var obj in world.Objects.Where(x => x.IsLightSource))
            //{
            //    if (obj.TryGetRayHit(reflectionRay, out var lightHit))
            //    {
            //        bmp.SetPixel(x, y, Color.White);
            //    } else
            //    {
            //        bmp.SetPixel(x, y, Color.Black);
            //    }
            //}
        }

        private Vector3 GetAmbientColor()
        {
            return new Vector3(0.5f, 0.6f, 0.7f);
        }
    }
}