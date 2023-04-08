using System.Numerics;

namespace SimpleRaytracer
{
    public struct RenderParams
    {
        public int ResolutionX { get; set; }
        public int ResolutionY { get; set; }
        public int Samples { get; set; }
        public int Bounces { get; set; }
        public Vector3 BottomLeft { get; }
        public float PlaneWidth { get; }
        public float PlaneHeight { get; }
        public Vector3 CameraPosition { get; }
        public Vector3 CameraRight { get; }
        public Vector3 CameraUp { get; }
        public Vector3 CameraForward { get; }
        public Vector3 Ambient { get; }

        public RenderParams(int resolutionX, int resolutionY, int samples, int bounces, Scene scene)
        {
            if (scene is null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            if (scene.Camera is null)
            {
                throw new ArgumentNullException(nameof(scene));
            }

            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            Samples = samples;
            Bounces = bounces;
            BottomLeft = scene.Camera.BottomLeft;
            PlaneWidth = scene.Camera.PlaneWidth;
            PlaneHeight = scene.Camera.PlaneHeight;
            CameraPosition = scene.Camera.Position;
            CameraRight = scene.Camera.Right;
            CameraUp = scene.Camera.Up;
            CameraForward = scene.Camera.Forward;
            Ambient = scene.Ambient;
        }
    }
}
