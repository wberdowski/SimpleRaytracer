using ILGPU;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System.Numerics;

namespace SimpleRaytracer.Gui
{
    public partial class Window : Form
    {
        private Size outputResolution = new(800, 800);
        private Scene scene;
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

            var sun = new GpuSphere(
                new Vector3(-2, -2, -1),
                new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 5),
                1f
            );

            var sphere = new GpuSphere(
                new Vector3(0, 0, 0),
                new Material(new Vector3(1, 0, 0)),
                1f
            );

            var ground = new GpuSphere(
                new Vector3(0, 100 + 1, 0),
                new Material(new Vector3(1, 1, 1)),
                100
            );

            scene = new Scene();
            scene.Camera = camera;
            scene.Objects.Add(sun);
            scene.Objects.Add(sphere);
            scene.Objects.Add(ground);


            // Initialize ILGPU.
            using var context = Context.Create(x => x.Cuda().CPU()/*.EnableAlgorithms()*/);
            using var accelerator = context.GetPreferredDevice(false)
                .CreateAccelerator(context);

            Console.WriteLine($"Running on: {accelerator.Name}");

            raytracer = new Raytracer(scene, outputResolution, accelerator);
            Render();
        }

        private void Render()
        {
            var bmp = raytracer.Raytrace();

            pictureBox.Image = bmp;
        }
    }
}