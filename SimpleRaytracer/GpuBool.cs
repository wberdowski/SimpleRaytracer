namespace SimpleRaytracer
{
    [Serializable]
    public struct GpuBool
    {
        public byte value;

        public GpuBool(bool value)
        {
            this.value = (byte)(value ? 1 : 0);
        }

        public static implicit operator bool(GpuBool value)
        {
            return value.value == 1;
        }

        public static implicit operator GpuBool(bool value)
        {
            return new GpuBool(value);
        }

        public override string ToString()
        {
            return ((bool)this).ToString();
        }
    }
}
