using System.Numerics;

namespace SimpleRaytracer
{
    public struct RenderParams
    {
        public int resolutionX;
        public int resolutionY;
        public int samples;
        public int bounces;
        public Vector3 bottomLeft;
        public float planeWidth;
        public float planeHeight;
        public Vector3 cameraPosition;
        public Vector3 cameraRight;
        public Vector3 cameraUp;
        public Vector3 cameraForward;

        public RenderParams(int resolutionX, int resolutionY, int samples, int bounces, Vector3 bottomLeft, float planeWidth, float planeHeight, Vector3 cameraPosition, Vector3 cameraRight, Vector3 cameraUp, Vector3 cameraForward)
        {
            this.resolutionX = resolutionX;
            this.resolutionY = resolutionY;
            this.samples = samples;
            this.bounces = bounces;
            this.bottomLeft = bottomLeft;
            this.planeWidth = planeWidth;
            this.planeHeight = planeHeight;
            this.cameraPosition = cameraPosition;
            this.cameraRight = cameraRight;
            this.cameraUp = cameraUp;
            this.cameraForward = cameraForward;
        }
    }
}
