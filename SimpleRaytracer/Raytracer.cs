using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimpleRaytracer
{
    public unsafe class Raytracer
    {
        public Accelerator Accelerator { get; }
        public Scene Scene { get; }
        public Size Resolution { get; }

        private readonly Action<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams, uint> _loadedKernel;
        private readonly MemoryBuffer1D<ColorDataBgr, Stride1D.Dense> _frameBuffer;
        private readonly MemoryBuffer1D<GpuSphere, Stride1D.Dense> _sceneBuffer;

        public Raytracer(Scene scene, Size resolution, Accelerator accelerator)
        {
            Scene = scene;
            Resolution = resolution;
            Accelerator = accelerator;

            // load / precompile the kernel
            _loadedKernel =
                accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams, uint>(Kernel);

            _frameBuffer = accelerator.Allocate1D<ColorDataBgr>(resolution.Width * resolution.Height);
            _sceneBuffer = accelerator.Allocate1D<GpuSphere>(scene.Objects.Count);
        }

        public Bitmap Raytrace(int sampleCount = 1000, int bounceCount = 5)
        {
            var cam = Scene.Camera;

            var planeHeight = cam.NearClipPlane * (float)Math.Tan(cam.FieldOfView * 0.5f * (Math.PI / 180)) * 2;
            var planeWidth = planeHeight * cam.Aspect;

            var bottomLeft = new Vector3(-planeWidth / 2, -planeHeight / 2, cam.NearClipPlane);

            var renderParams = new RenderParams(
                Resolution.Width,
                Resolution.Height,
                sampleCount,
                bounceCount,
                bottomLeft,
                planeWidth,
                planeHeight,
                cam.Position,
                cam.Right,
                cam.Up,
                cam.Forward
            );

            var random = new Random();

            _sceneBuffer.CopyFromCPU(Scene.Objects.ToArray());

            _loadedKernel(Resolution.Width * Resolution.Height, _frameBuffer.View, _sceneBuffer, renderParams, (uint)random.Next());

            var outputBuffer = _frameBuffer.GetAsArray1D();

            _frameBuffer.Dispose();

            var bmp = new Bitmap(Resolution.Width, Resolution.Height);
            var bmpSizeBytes = Resolution.Width * Resolution.Height * sizeof(byte) * 3;

            var bmpData = bmp.LockBits(new Rectangle(0, 0, Resolution.Width, Resolution.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var ptrDst = bmpData.Scan0.ToPointer();

            fixed (void* ptrSrc = &outputBuffer[0])
            {
                Unsafe.CopyBlock(ptrDst, ptrSrc, (uint)bmpSizeBytes);
            }

            bmp.UnlockBits(bmpData);

            return bmp;
        }

        public static void Kernel(Index1D index, ArrayView1D<ColorDataBgr, Stride1D.Dense> output, ArrayView1D<GpuSphere, Stride1D.Dense> objects, RenderParams renderParams, uint rngSeed)
        {
            var indexX = index % renderParams.resolutionX;
            var indexY = index / renderParams.resolutionX;

            var normalizedX = indexX / (renderParams.resolutionX - 1f);
            var normalizedY = indexY / (renderParams.resolutionY - 1f);

            Vector3 pixelColor = Vector3.Zero;

            var rng1 = new XorShift64Star((uint)indexX * (uint)indexY + (uint)indexX + rngSeed);
            var rng = new XorShift128(rng1.NextUInt(), rng1.NextUInt(), rng1.NextUInt(), rng1.NextUInt());

            for (int i = 0; i < renderParams.samples; i++)
            {
                var local = renderParams.bottomLeft + new Vector3(
                    renderParams.planeWidth * normalizedX + (NormalizedRandomFloat(ref rng) / 10000),
                    renderParams.planeHeight * normalizedY + (NormalizedRandomFloat(ref rng) / 10000),
                    0);

                var position = renderParams.cameraPosition +
                    renderParams.cameraRight * local.X +
                    renderParams.cameraUp * local.Y +
                    renderParams.cameraForward * local.Z;

                var dir = Vector3.Normalize(position - renderParams.cameraPosition);
                var rayFromCamera = new Ray(renderParams.cameraPosition, dir);

                pixelColor += TraceBounces(rayFromCamera, objects, renderParams.bounces, ref rng);
            }

            //pixelColor = new Vector3(rng.NextFloat(), rng.NextFloat(), rng.NextFloat()) * renderParams.samples;

            var scale = 1f / renderParams.samples;

            var c = new Vector3(
                (float)XMath.Sqrt(pixelColor.X * scale),
                (float)XMath.Sqrt(pixelColor.Y * scale),
                (float)XMath.Sqrt(pixelColor.Z * scale)
            );

            var outColor = new ColorDataBgr(
                (byte)XMath.Clamp(c.X * 255, 0, 255),
                (byte)XMath.Clamp(c.Y * 255, 0, 255),
                (byte)XMath.Clamp(c.Z * 255, 0, 255)
            );

            output[index] = outColor;
        }

        public static bool TryGetClosestHit(ArrayView1D<GpuSphere, Stride1D.Dense> objects, Ray ray, out Hit closestHit)
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

        private static Vector3 TraceBounces(Ray ray, ArrayView1D<GpuSphere, Stride1D.Dense> objects, int bounces, ref XorShift128 rngView)
        {
            var rayColor = Vector3.One;
            var incomingLight = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                if (!TryGetClosestHit(objects, ray, out var closestHit))
                {
                    break;
                }

                var diffuseVecDir = RandomNormalizedVector3(ref rngView);

                if (Vector3.Dot(diffuseVecDir, closestHit.normal) < 0f)
                {
                    diffuseVecDir = -diffuseVecDir;
                }

                ray = new Ray(closestHit.position, diffuseVecDir);

                var emmitedLight = closestHit.material.Emission;
                incomingLight += emmitedLight * rayColor;
                rayColor *= closestHit.material.Albedo;
            }

            return incomingLight;
        }

        private static Vector3 RandomNormalizedVector3(ref XorShift128 rngView)
        {
            var halfIntMax = int.MaxValue / 2;

            return Vector3.Normalize(new(
                rngView.Next() - halfIntMax,
                rngView.Next() - halfIntMax,
                rngView.Next() - halfIntMax
            ));
        }

        private static float NormalizedRandomFloat(ref XorShift128 rngView)
        {
            return rngView.NextFloat() * 2 - 1f;
        }
    }
}