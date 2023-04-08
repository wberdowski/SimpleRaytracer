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
    }
}
