using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using System;
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
            _sceneBuffer = accelerator.Allocate1D<GpuSphere>(scene.Objects.Length);
        }

        public void Render(int sampleCount = 100, int bounceCount = 5)
        {
            var cam = Scene.Camera;

            var renderParams = new RenderParams(
                Resolution.Width,
                Resolution.Height,
                sampleCount,
                bounceCount,
                Scene
            );

            var random = new Random();

            _sceneBuffer.CopyFromCPU(Scene.Objects);

            _loadedKernel(Resolution.Width * Resolution.Height, _frameBuffer.View, _sceneBuffer, renderParams, (uint)random.Next());
        }

        public Bitmap WaitForResult()
        {
            Accelerator.Synchronize();

            var outputBuffer = _frameBuffer.GetAsArray1D();

            //_frameBuffer.Dispose();

            var bmpSizeBytes = Resolution.Width * Resolution.Height * sizeof(byte) * 3;

            var bmp = new Bitmap(Resolution.Width, Resolution.Height);
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
            var indexX = index % renderParams.ResolutionX;
            var indexY = index / renderParams.ResolutionX;

            var normalizedX = indexX / (renderParams.ResolutionX - 1f);
            var normalizedY = indexY / (renderParams.ResolutionY - 1f);

            Vector3 pixelColor = Vector3.Zero;

            var rng1 = new XorShift64Star((uint)indexX * (uint)indexY + (uint)indexX + rngSeed);
            var rng = new XorShift128(rng1.NextUInt(), rng1.NextUInt(), rng1.NextUInt(), rng1.NextUInt());

            for (int i = 0; i < renderParams.Samples; i++)
            {
                var offsetVec = GetRandomVector2InUnitSphere(ref rng) / 10000;

                var local = renderParams.BottomLeft + new Vector3(
                    renderParams.PlaneWidth * normalizedX + offsetVec.X,
                    renderParams.PlaneHeight * normalizedY + offsetVec.Y,
                    0);

                var position = renderParams.CameraPosition +
                    renderParams.CameraRight * local.X +
                    renderParams.CameraUp * local.Y +
                    renderParams.CameraForward * local.Z;

                var dir = Vector3.Normalize(position - renderParams.CameraPosition);
                var rayFromCamera = new Ray(renderParams.CameraPosition, dir);

                pixelColor += TraceBounces(rayFromCamera, objects, renderParams.Bounces, ref rng);
            }

            //pixelColor = new Vector3(rng.NextFloat(), rng.NextFloat(), rng.NextFloat()) * renderParams.samples;

            var scale = 1f / renderParams.Samples;

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

        private static Vector3 TraceBounces(Ray ray, ArrayView1D<GpuSphere, Stride1D.Dense> objects, int bounces, ref XorShift128 random)
        {
            var rayColor = Vector3.One;
            var incomingLight = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                if (!TryGetClosestHit(objects, ray, out var closestHit))
                {
                    break;
                }

                var diffuseDir = Vector3.Normalize(closestHit.normal + GetRandomVector3InUnitSphere(ref random));
                var specularVecDir = Vector3.Reflect(ray.Direction, closestHit.normal);

                var reflectionDir = Vector3.Lerp(diffuseDir, specularVecDir, closestHit.material.Smoothness);

                ray = new Ray(closestHit.position, reflectionDir);

                var emmitedLight = closestHit.material.Emission;

                incomingLight += emmitedLight * rayColor;
                rayColor *= closestHit.material.Albedo;
            }

            return incomingLight;
        }

        private static float GetRandomFloatNormalized(ref XorShift128 random)
        {
            return random.NextFloat() * 2 - 1f;
        }

        private static Vector3 GetRandomVector3(ref XorShift128 random)
        {
            return new(
                GetRandomFloatNormalized(ref random),
                GetRandomFloatNormalized(ref random),
                GetRandomFloatNormalized(ref random)
            );
        }

        public static Vector3 GetRandomVector3InUnitSphere(ref XorShift128 random)
        {
            var u = random.NextFloat() * 2 - 1;
            var theta = random.NextFloat() * 2 * XMath.PI;
            var r = XMath.Sqrt(1 - u * u);
            var x = r * XMath.Cos(theta);
            var y = r * XMath.Sin(theta);

            return new Vector3(x, y, u);
        }

        public static Vector2 GetRandomVector2InUnitSphere(ref XorShift128 random)
        {
            var u = random.NextFloat() * 2 - 1;
            var theta = random.NextFloat() * 2 * XMath.PI;
            var r = XMath.Sqrt(1 - u * u);
            var x = r * XMath.Cos(theta);

            return new Vector2(x, u);
        }
    }
}