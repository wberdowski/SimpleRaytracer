using ILGPU;
using ILGPU.Algorithms;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimpleRaytracer
{
    public unsafe class Raytracer : IDisposable
    {
        public Accelerator Accelerator => _accelerator;

        private Context _context;
        private Accelerator _accelerator;
        private Action<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<Vector3, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, ArrayView1D<Mesh, Stride1D.Dense>, ArrayView1D<Triangle, Stride1D.Dense>, RenderParams, ulong> _loadedKernel;
        private MemoryBuffer1D<ColorDataBgr, Stride1D.Dense>? _frameBuffer;
        private MemoryBuffer1D<Vector3, Stride1D.Dense>? _accumulationBuffer;
        private Scene _scene;
        private MemoryBuffer1D<GpuSphere, Stride1D.Dense>? _sceneSphereBuffer;
        private MemoryBuffer1D<Mesh, Stride1D.Dense> _sceneMeshBuffer;
        private MemoryBuffer1D<Triangle, Stride1D.Dense> _sceneTriangleBuffer;

        private int count = 0;

        public Size Resolution { get; }

        public Raytracer(Size resolution)
        {
            Resolution = resolution;

            InitializeAccelerator();
        }

        private void InitializeAccelerator()
        {
            _context = Context.Create(b => b.CPU().Cuda().EnableAlgorithms());

            _accelerator = _context.GetPreferredDevice(false)
                .CreateAccelerator(_context);

            // Allocate frame buffer
            _frameBuffer = _accelerator.Allocate1D<ColorDataBgr>(Resolution.Width * Resolution.Height);
            _accumulationBuffer = _accelerator.Allocate1D<Vector3>(Resolution.Width * Resolution.Height);

            // load / precompile the kernel
            _loadedKernel = _accelerator.LoadAutoGroupedStreamKernel<Index1D, ArrayView1D<ColorDataBgr, Stride1D.Dense>, ArrayView1D<Vector3, Stride1D.Dense>, ArrayView1D<GpuSphere, Stride1D.Dense>, ArrayView1D<Mesh, Stride1D.Dense>, ArrayView1D<Triangle, Stride1D.Dense>, RenderParams, ulong>(Kernel);
        }

        public void Reset()
        {
            count = 0;
            _accumulationBuffer.MemSet(0);
        }

        public void SetScene(Scene scene)
        {
            _scene = scene;

            // Allocate buffers
            _sceneSphereBuffer = _accelerator.Allocate1D<GpuSphere>(scene.Objects.Length);
            _sceneMeshBuffer = _accelerator.Allocate1D<Mesh>(scene.Meshes.Length);
            _sceneTriangleBuffer = _accelerator.Allocate1D<Triangle>(scene.Triangles.Length);

            Debug.WriteLine($"Allocated ({nameof(_sceneSphereBuffer)}) = {_sceneSphereBuffer.LengthInBytes:n0} B");
            Debug.WriteLine($"Allocated ({nameof(_sceneMeshBuffer)}) = {_sceneMeshBuffer.LengthInBytes:n0} B");
            Debug.WriteLine($"Allocated ({nameof(_sceneTriangleBuffer)}) = {_sceneTriangleBuffer.LengthInBytes:n0} B");

            // Copy data
            _sceneSphereBuffer.CopyFromCPU(scene.Objects);
            _sceneMeshBuffer.CopyFromCPU(scene.Meshes);
            _sceneTriangleBuffer.CopyFromCPU(scene.Triangles);
        }

        public void Render(Vector3 sunDir, bool simplifiedEnabled, int sampleCount = 100, int bounceCount = 5)
        {
            count += sampleCount;

            // Start kernel execution
            var renderParams = new RenderParams(
                Resolution.Width,
                Resolution.Height,
                sampleCount,
                bounceCount,
                _scene,
                simplifiedEnabled,
                count
            );

            renderParams.SunDir = sunDir;

            _loadedKernel(Resolution.Width * Resolution.Height, _frameBuffer.View, _accumulationBuffer.View, _sceneSphereBuffer, _sceneMeshBuffer, _sceneTriangleBuffer, renderParams, (ulong)DateTime.Now.Ticks);
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

            //_sceneSphereBuffer?.Dispose();

            return bmp;
        }

        public void WaitForResult(ref Bitmap bmp)
        {
            _accelerator.Synchronize();

            var outputBuffer = _frameBuffer.GetAsArray1D();
            var bmpSizeBytes = Resolution.Width * Resolution.Height * sizeof(byte) * 3;

            var bmpData = bmp.LockBits(new Rectangle(0, 0, Resolution.Width, Resolution.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var ptrDst = bmpData.Scan0.ToPointer();

            fixed (void* ptrSrc = &outputBuffer[0])
            {
                Unsafe.CopyBlock(ptrDst, ptrSrc, (uint)bmpSizeBytes);
            }

            bmp.UnlockBits(bmpData);

            //_sceneSphereBuffer?.Dispose();
        }

        public static void Kernel(Index1D index, ArrayView1D<ColorDataBgr, Stride1D.Dense> output, ArrayView1D<Vector3, Stride1D.Dense> accumulator, ArrayView1D<GpuSphere, Stride1D.Dense> objects, ArrayView1D<Mesh, Stride1D.Dense> meshes, ArrayView1D<Triangle, Stride1D.Dense> triangles, RenderParams renderParams, ulong rngSeed)
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
                var offsetVec = Vector2.Zero;

                // Add small ray origin offset
                if (!renderParams.SimplifiedEnabled)
                {
                    offsetVec = GetRandomVector2InUnitSphere(ref rng) / 20000;
                }

                // Calculate ray with origin at camera position
                var local = renderParams.BottomLeft + new Vector3(
                    renderParams.PlaneWidth * normalizedX + offsetVec.X,
                    renderParams.PlaneHeight * (1 - normalizedY) + offsetVec.Y,
                    0
                );

                var rayTargetPos = renderParams.CameraPosition +
                    renderParams.CameraRight * local.X +
                    renderParams.CameraUp * local.Y +
                    renderParams.CameraForward * local.Z;

                var dir = Vector3.Normalize(rayTargetPos - renderParams.CameraPosition);
                var cameraRay = new Ray(renderParams.CameraPosition, dir);

                // Accumulate received light for a given pixel
                if (!renderParams.SimplifiedEnabled)
                {
                    pixelColor += TraceBounces(cameraRay, objects, meshes, triangles, renderParams.Bounces, ref rng, renderParams);
                }
                else
                {
                    pixelColor = TraceSimplified(cameraRay, objects, meshes, triangles, renderParams.Bounces, ref rng, renderParams);
                    renderParams.Samples = 1;
                    break;
                }
            }

            // Gamma correction
            var scale = 1f / renderParams.CurrentSampleCount;

            // Output calculated pixel color
            accumulator[index] += pixelColor;
            output[index] = ColorDataBgr.GetGammaCorrected(accumulator[index], scale);
        }

        public static bool TryGetClosestHit(ArrayView1D<GpuSphere, Stride1D.Dense> objects, ArrayView1D<Mesh, Stride1D.Dense> meshes, ArrayView1D<Triangle, Stride1D.Dense> triangles, Ray ray, out Hit closestHit)
        {
            Hit hit = default;
            closestHit = default;
            bool didHit = false;
            float minDist = float.MaxValue;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i].TryGetRayHit(ray, ref hit) && hit.distance < minDist)
                {
                    closestHit = hit;
                    minDist = hit.distance;
                    didHit = true;
                }
            }

            for (int i = 0; i < meshes.Length; i++)
            {
                if (meshes[i].SkipBoundingBoxTest || meshes[i].Aabb.TestAabb(ray, out var dist) && dist < minDist)
                {
                    for (int j = 0; j < meshes[i].triangleCount; j++)
                    {
                        if (triangles[j + meshes[i].arrayOffset].IntersectRayTriangle(ray, ref hit) && hit.distance < minDist)
                        {
                            hit.material = meshes[i].Material;
                            closestHit = hit;
                            minDist = hit.distance;
                            didHit = true;
                        }
                    }
                }
            }

            return didHit;
        }

        private static Vector3 TraceBounces(Ray ray, ArrayView1D<GpuSphere, Stride1D.Dense> objects, ArrayView1D<Mesh, Stride1D.Dense> meshes, ArrayView1D<Triangle, Stride1D.Dense> triangles, int bounces, ref XorShift128 random, RenderParams renderParams)
        {
            var rayColor = Vector3.One;
            var lightColor = Vector3.Zero;

            for (int i = 0; i <= bounces; i++)
            {
                if (!TryGetClosestHit(objects, meshes, triangles, ray, out var hit))
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

        private static Vector3 TraceSimplified(Ray ray, ArrayView1D<GpuSphere, Stride1D.Dense> objects, ArrayView1D<Mesh, Stride1D.Dense> meshes, ArrayView1D<Triangle, Stride1D.Dense> triangles, int bounces, ref XorShift128 random, RenderParams renderParams)
        {
            if (TryGetClosestHit(objects, meshes, triangles, ray, out var hit))
            {
                return Clamp(hit.material.Albedo * Vector3.Dot(hit.normal, renderParams.SunDir) + hit.material.Emission);
            }

            return renderParams.Ambient;
        }

        private static Vector3 Clamp(Vector3 value)
        {
            value.X = XMath.Clamp(value.X, 0, 1);
            value.Y = XMath.Clamp(value.Y, 0, 1);
            value.Z = XMath.Clamp(value.Z, 0, 1);

            return value;
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
            _frameBuffer?.Dispose();
            _accumulationBuffer?.Dispose();
            _accelerator.Dispose();
            _context.Dispose();
        }
    }
}