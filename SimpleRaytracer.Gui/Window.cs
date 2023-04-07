using System.Numerics;

namespace SimpleRaytracer.Gui
{
    public partial class Window : Form
    {
        private Size outputResolution = new(600, 600);
        private Scene world;
        private Raytracer raytracer;

        public Window()
        {
            InitializeComponent();
            ClientSize = outputResolution;

            Location = new Point(
                Screen.PrimaryScreen.WorkingArea.Width / 2 - outputResolution.Width / 2,
                Screen.PrimaryScreen.WorkingArea.Height / 2 - outputResolution.Height / 2
            );
        }

        private void Window_Load(object sender, EventArgs e)
        {
            var camera = new Camera(new Vector3(0, -2, -4), 0.1f, 60, outputResolution.Width / (float)outputResolution.Height);

            camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -30 * (float)(Math.PI / 180));

            var sun = new Sphere(new Vector3(-2, -2, -1), 1f)
            {
                IsLightSource = true,
                Material = new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 5)
            };

            var sphere = new Sphere(new Vector3(0, 0, 0), 1f)
            {
                Material = new Material(new Vector3(1, 0, 0))
            };

            var ground = new Sphere(new Vector3(0, 100 + 1, 0), 100)
            {
                Material = new Material(new Vector3(1,1,1))
            };

            world = new Scene();
            world.Camera = camera;
            world.Objects.Add(sun);
            world.Objects.Add(sphere);
            world.Objects.Add(ground);

            raytracer = new Raytracer();

            Render();
        }

        private void Render()
        {
            var bmp = raytracer.Raytrace(world, outputResolution);

            pictureBox.Image = bmp;
        }
    }
}