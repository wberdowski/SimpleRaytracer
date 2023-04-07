using FastBitmapLib;
using ILGPU;
using ILGPU.Runtime;
using System.Drawing;
using System.Numerics;

namespace SimpleRaytracer
{
    public class Raytracer
    {
        public Accelerator Accelerator { get; }
        public Scene Scene { get; }
        public Size Resolution { get; }

        private Action<Index2D, ArrayView2D<Vector3, Stride2D.DenseX>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams> _loadedKernel;
        private MemoryBuffer2D<Vector3, Stride2D.DenseX> _frameBuffer;
        private MemoryBuffer1D<GpuSphere, Stride1D.Dense> _sceneBuffer;

        public Raytracer(Scene scene, Size resolution, Accelerator accelerator)
        {
            Scene = scene;
            Resolution = resolution;
            Accelerator = accelerator;

            // load / precompile the kernel
            _loadedKernel =
                accelerator.LoadAutoGroupedStreamKernel<Index2D, ArrayView2D<Vector3, Stride2D.DenseX>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams>(Kernel);

            _frameBuffer = accelerator.Allocate2DDenseX<Vector3>(new LongIndex2D(resolution.Width, resolution.Height));
            _sceneBuffer = accelerator.Allocate1D<GpuSphere>(scene.Objects.Count);
        }

        public Bitmap Raytrace()
        {
            var sampleCount = 50;
            var bounces = 5;
            var cam = Scene.Camera;

            var planeHeight = cam.NearClipPlane * (float)Math.Tan(cam.FieldOfView * 0.5f * (Math.PI / 180)) * 2;
            var planeWidth = planeHeight * cam.Aspect;

            var bottomLeft = new Vector3(-planeWidth / 2, -planeHeight / 2, cam.NearClipPlane);

            var renderParams = new RenderParams(
                Resolution.Width,
                Resolution.Height,
                sampleCount,
                bounces,
                bottomLeft,
                planeWidth,
                planeHeight,
                cam.Position,
                cam.Right,
                cam.Up,
                cam.Forward
            );

            _sceneBuffer.CopyFromCPU(Scene.Objects.ToArray());

            _loadedKernel(new Index2D(Resolution.Width, Resolution.Height), _frameBuffer.View, _sceneBuffer, renderParams);

            Accelerator.Synchronize();

            var outputBuffer = _frameBuffer.GetAsArray2D();

            _frameBuffer.Dispose();

            var bmp = new Bitmap(Resolution.Width, Resolution.Height);

            var scale = 1f / sampleCount;

            using (var fastBitmap = bmp.FastLock())
            {
                for (int by = 0; by < Resolution.Height; by++)
                {
                    for (int bx = 0; bx < Resolution.Width; bx++)
                    {
                        outputBuffer[bx, by] = new Vector3(
                            (float)Math.Sqrt(outputBuffer[bx, by].X * scale),
                            (float)Math.Sqrt(outputBuffer[bx, by].Y * scale),
                            (float)Math.Sqrt(outputBuffer[bx, by].Z * scale)
                        );

                        var outColor = Color.FromArgb(
                            (int)Math.Clamp(outputBuffer[bx, by].X * 255, 0, 255),
                            (int)Math.Clamp(outputBuffer[bx, by].Y * 255, 0, 255),
                            (int)Math.Clamp(outputBuffer[bx, by].Z * 255, 0, 255)
                        );

                        fastBitmap.SetPixel(bx, by, outColor);
                    }
                }
            }

            return bmp;
        }

        public static void Kernel(Index2D idx, ArrayView2D<Vector3, Stride2D.DenseX> output, ArrayView1D<GpuSphere, Stride1D.Dense> objects, RenderParams renderParams)
        {
            var normalizedX = idx.X / (renderParams.resolutionX - 1f);
            var normalizedY = idx.Y / (renderParams.resolutionY - 1f);

            Vector3 pixelColor = Vector3.Zero;

            for (int i = 0; i < renderParams.samples; i++)
            {
                var local = renderParams.bottomLeft + new Vector3(
                    //renderParams.planeWidth * normalizedX + ((GenerateRandomNumber(idx.X * idx.Y + idx.Y, -50, 50) / 100f) / renderParams.resolutionX / 10),  // TODO: / 10 ?
                    //renderParams.planeWidth * normalizedX + ((GenerateRandomNumber(idx.X * idx.Y + idx.Y, -50, 50) / 100f) / renderParams.resolutionX / 10),  // TODO: / 10 ?
                    renderParams.planeWidth * normalizedX,
                    renderParams.planeHeight * normalizedY,
                    0);

                var position = renderParams.cameraPosition +
                    renderParams.cameraRight * local.X +
                    renderParams.cameraUp * local.Y +
                    renderParams.cameraForward * local.Z;

                var dir = Vector3.Normalize(position - renderParams.cameraPosition);
                var rayFromCamera = new Ray(renderParams.cameraPosition, dir);

                pixelColor += TraceBounces(rayFromCamera, objects, renderParams.bounces, idx.X * idx.Y + idx.Y);
            }

            output[idx] = pixelColor;
        }

        public static bool TryGetClosestHit(ArrayView<GpuSphere> objects, Ray ray, out Hit closestHit)
        {
            closestHit = default;
            bool didHit = false;
            float minDist = float.MaxValue;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].TryGetRayHit(ray, out var hit) && hit.distance < minDist)
                {
                    closestHit = hit;
                    minDist = hit.distance;
                    didHit = true;
                }
            }
            return didHit;
        }

        private static Vector3 TraceBounces(Ray ray, ArrayView<GpuSphere> objects, int bounces, int seed)
        {
            var rayColor = Vector3.One;
            var incomingLight = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                if (!TryGetClosestHit(objects, ray, out var closestHit))
                {
                    break;
                }

                var randomVectorDir = RandomNormalizedVector3(seed);

                if (Vector3.Dot(randomVectorDir, closestHit.normal) < 0f)
                {
                    randomVectorDir = -randomVectorDir;
                }

                ray = new Ray(closestHit.position, randomVectorDir);

                var emmitedLight = closestHit.material.Emission;
                incomingLight += emmitedLight * rayColor;
                rayColor *= closestHit.material.Albedo;
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

        //private float RandomGaussian()
        //{
        //    var theta = 2 * Math.PI * (_random.Next() - int.MaxValue/2);
        //    var rho = Math.Sqrt(-2 * Math.Log(_random.Next() - int.MaxValue / 2));
        //    return (float)(rho * Math.Cos(theta));
        //}

        public static int GenerateRandomNumber(int seed, int minValue, int maxValue)
        {
            uint x = (uint)seed;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            int randNum = (int)(x % (uint)(maxValue - minValue + 1) + (uint)minValue);
            return randNum;
        }

        private static Vector3 RandomNormalizedVector3(int seed)
        {
            return Vector3.Normalize(new Vector3(
                GenerateRandomNumber(seed + 1, -1000, 1000),
                GenerateRandomNumber(seed + 2, -1000, 1000),
                GenerateRandomNumber(seed + 3, -1000, 1000)
            ));
        }

        //private Vector3 RandomVec3(float min, float max)
        //{
        //    return new Vector3(
        //        _random.NextSingle() * (max - min) + min,
        //        _random.NextSingle() * (max - min) + min,
        //        _random.NextSingle() * (max - min) + min
        //        );
        //}

        //private Vector3 RandomInUnitSphere()
        //{
        //    while (true)
        //    {
        //        var p = RandomVec3(-1, 1);
        //        if (p.LengthSquared() >= 1) continue;
        //        return p;
        //    }
        //}
    }
}