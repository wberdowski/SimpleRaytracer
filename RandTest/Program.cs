// ---------------------------------------------------------------------------------------
//                                    ILGPU Samples
//                           Copyright (c) 2021 ILGPU Project
//                                    www.ilgpu.net
//
// File: Program.cs
//
// This file is part of ILGPU and is distributed under the University of Illinois Open
// Source License. See LICENSE.txt for details.
// ---------------------------------------------------------------------------------------

using ILGPU;
using ILGPU.Algorithms.Random;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using System.Globalization;

namespace RandTest
{
    internal class Program
    {
        /// <summary>
        /// Generate random numbers within a kernel.
        /// </summary>
        public static void MyRandomKernel(
            Index1D index,
            RNGView<XorShift64Star> rng,
            ArrayView1D<float, Stride1D.Dense> view)
        {
            view[index] = rng.NextFloat() * 2 - 1;
        }

        /// <summary>
        /// Make use of random number generation within a kernel.
        /// </summary>
        private static void KernelRandom(Accelerator accelerator)
        {
            Console.WriteLine("Kernel Random");

            // Generate random numbers using the XorShift64Star algorithm.
            // NB: Use the standard .NET random number generator to initialize each GPU
            // kernel with a different starting seed. Otherwise, if the kernels have the
            // same starting seed, they will generate the same "random number".
            var random = new Random();
            using var rng = RNG.Create<XorShift64Star>(accelerator, random);

            // Use the RNG implementation to get a view that is compatible with the given
            // max number of parallel warps. This value is particularly important since
            // this implementation shares a single RNG state across all threads in a warp.
            var rngView = rng.GetView(accelerator.WarpSize);

            using var buffer = accelerator.Allocate1D<float>(1024);
            var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1D, RNGView<XorShift64Star>, ArrayView1D<float, Stride1D.Dense>>(MyRandomKernel);
            kernel((int)buffer.Length, rngView, buffer.View);

            // Reads data from the GPU buffer into a new CPU array.
            // Implicitly calls accelerator.DefaultStream.Synchronize() to ensure
            // that the kernel and memory copy are completed first.
            var randomValues = buffer.GetAsArray1D();
            for (int i = 0; i < randomValues.Length; ++i)
            {
                Console.Write($"({randomValues[i].ToString(CultureInfo.InvariantCulture)}, 0),");
            }

            Console.WriteLine();
        }


        /// <summary>
        /// Examples of generating random values.
        /// </summary>
        private static void Main()
        {
            // Create default context and enable algorithms library
            using var context = Context.Create(builder => builder.CPU().EnableAlgorithms());
            var device = context.Devices.OrderByDescending(x => x.MaxNumThreads).First();

            using var accelerator = device.CreateAccelerator(context);
            Console.WriteLine($"Performing operations on {accelerator}");

            KernelRandom(accelerator);
        }
    }
}