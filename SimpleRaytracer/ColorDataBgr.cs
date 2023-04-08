using ILGPU.Algorithms;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleRaytracer
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ColorDataBgr
    {
        public byte b;
        public byte g;
        public byte r;

        public ColorDataBgr(byte r, byte g, byte b)
        {
            this.r = r;
            this.g = g;
            this.b = b;
        }

        public static ColorDataBgr GetGammaCorrected(Vector3 pixelColor, float scale)
        {
            return new ColorDataBgr(
                (byte)XMath.Clamp(XMath.Sqrt(pixelColor.X * scale) * 255, 0, 255),
                (byte)XMath.Clamp(XMath.Sqrt(pixelColor.Y * scale) * 255, 0, 255),
                (byte)XMath.Clamp(XMath.Sqrt(pixelColor.Z * scale) * 255, 0, 255)
            );
        }
    }
}
