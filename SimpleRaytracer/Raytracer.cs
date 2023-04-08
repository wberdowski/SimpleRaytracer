using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimpleRaytracer
{
    public unsafe class Raytracer : IDisposable
    {
        private Context _context;
        private Accelerator _accelerator;
        private Action<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams, ulong> _loadedKernel;
        private MemoryBuffer1D<ColorDataBgr, Stride1D.Dense>? _frameBuffer;
        private MemoryBuffer1D<GpuSphere, Stride1D.Dense>? _sceneBuffer;

        public Size Resolution { get; }

        public Raytracer(Size resolution)
        {
            Resolution = resolution;

            InitializeAccelerator();
        }

        private void InitializeAccelerator()
        {
            _context = Context.Create(x => x.Cuda().EnableAlgorithms());
            _accelerator = _context.GetPreferredDevice(false)
                .CreateAccelerator(_context);

            // Allocate frame buffer
            _frameBuffer = _accelerator.Allocate1D<ColorDataBgr>(Resolution.Width * Resolution.Height);

            // load / precompile the kernel
            _loadedKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, RenderParams, ulong>(Kernel);
        }

        public void Render(Scene scene, int sampleCount = 100, int bounceCount = 5)
        {
            // Allocate buffers
            _sceneBuffer = _accelerator.Allocate1D<GpuSphere>(scene.Objects.Length);

            // Copy data
            _sceneBuffer.CopyFromCPU(scene.Objects);

            // Start kernel execution
            var renderParams = new RenderParams(
                Resolution.Width,
                Resolution.Height,
                sampleCount,
                bounceCount,
                scene
            );

            _loadedKernel(Resolution.Width * Resolution.Height, _frameBuffer.View, _sceneBuffer, renderParams, (ulong)DateTime.Now.Ticks);
        }

        public Bitmap WaitForResult()
        {
            _accelerator.Synchronize();

            var outputBuffer = _frameBuffer.GetAsArray1D();
            var bmpSizeBytes = Resolution.Width * Resolution.Height * sizeof(byte) * 3;
            var bmp = new Bitmap(Resolution.Width, Resolution.Height);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, Resolution.Width, Resolution.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var ptrDst = bmpData.Scan0.ToPointer();

            fixed (void* ptrSrc = &outputBuffer[0])
            {
                Unsafe.CopyBlock(ptrDst, ptrSrc, (uint)bmpSizeBytes);
            }

            bmp.UnlockBits(bmpData);

            _frameBuffer?.Dispose();
            _sceneBuffer?.Dispose();

            return bmp;
        }

        public static void Kernel(Index1D index, ArrayView1D<ColorDataBgr, Stride1D.Dense> output, ArrayView1D<GpuSphere, Stride1D.Dense> objects, RenderParams renderParams, ulong rngSeed)
        {
            // Get pixel position
            var x = index % renderParams.ResolutionX;
            var y = index / renderParams.ResolutionX;

            // Normalize pixel position [0...1]
            var normalizedX = x / (renderParams.ResolutionX - 1f);
            var normalizedY = y / (renderParams.ResolutionY - 1f);

            // Initialize pseudo-random functions
            var rngInit = new XorShift64Star((ulong)index + rngSeed);
            var rng = new XorShift128(rngInit.NextUInt(), rngInit.NextUInt(), rngInit.NextUInt(), rngInit.NextUInt());

            var pixelColor = Vector3.Zero;

            // Run raytracing algorithm n-times for every pixel
            for (int i = 0; i < renderParams.Samples; i++)
            {
                // Add small ray origin offset
                var offsetVec = GetRandomVector2InUnitSphere(ref rng) / 10000;

                // Calculate ray with origin at camera position
                var local = renderParams.BottomLeft + new Vector3(
                    renderParams.PlaneWidth * normalizedX + offsetVec.X,
                    renderParams.PlaneHeight * normalizedY + offsetVec.Y,
                    0
                );

                var rayTargetPos = renderParams.CameraPosition +
                    renderParams.CameraRight * local.X +
                    renderParams.CameraUp * local.Y +
                    renderParams.CameraForward * local.Z;

                var dir = Vector3.Normalize(rayTargetPos - renderParams.CameraPosition);
                var cameraRay = new Ray(renderParams.CameraPosition, dir);

                // Accumulate received light for a given pixel
                pixelColor += TraceBounces(cameraRay, objects, renderParams.Bounces, ref rng, renderParams);
            }

            // Gamma correction
            var scale = 1f / renderParams.Samples;

            // Output calculated pixel color
            output[index] = ColorDataBgr.GetGammaCorrected(pixelColor, scale);
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

        private static Vector3 TraceBounces(Ray ray, ArrayView1D<GpuSphere, Stride1D.Dense> objects, int bounces, ref XorShift128 random, RenderParams renderParams)
        {
            var rayColor = Vector3.One;
            var lightColor = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                if (!TryGetClosestHit(objects, ray, out var hit))
                {
                    lightColor += renderParams.Ambient * rayColor;
                    break;
                }

                var diffuseDir = Vector3.Normalize(hit.normal + GetRandomVector3InUnitSphere(ref random));
                var specularVecDir = Vector3.Reflect(ray.Direction, hit.normal);

                var reflectionDir = Vector3.Lerp(diffuseDir, specularVecDir, hit.material.Smoothness);

                ray = new Ray(hit.position, reflectionDir);

                lightColor += hit.material.Emission * rayColor;
                rayColor *= hit.material.Albedo;
            }

            return lightColor;
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

        public void Dispose()
        {
            _accelerator.Dispose();
            _context.Dispose();
        }
    }
}