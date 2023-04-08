using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System.Diagnostics;
using System.Numerics;

namespace SimpleRaytracer.Gui
{
    public partial class Window : Form
    {
        private Size outputResolution = new(1800, 600);
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
            var camera = new Camera(new Vector3(0, -2, -4), 0.1f, 40, outputResolution.Width / (float)outputResolution.Height);

            camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -28 * (float)(Math.PI / 180));

            var sun = new GpuSphere(
                new Vector3(-10, -10, 0),
                new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 6),
                6f
            );

            var ground = new GpuSphere(
                new Vector3(0, 1000 + 0.75f, 0),
                new Material(new Vector3(0.9f, 0.9f, 0.9f)),
                1000
            );

            // Spheres

            var sphere1 = new GpuSphere(
                new Vector3(-3, 0, 0),
                new Material(new Vector3(0.95f, 0, 0), 0),
                0.75f
            );

            var sphere2 = new GpuSphere(
                new Vector3(-1, 0, 0),
                new Material(new Vector3(0, 0.95f, 0), 0.1f),
                 0.75f
            );

            var sphere3 = new GpuSphere(
                new Vector3(1, 0, 0),
                new Material(new Vector3(0, 0, 0.95f), 0.7f),
                 0.75f
            );

            var sphere4 = new GpuSphere(
                new Vector3(3, 0, 0),
                new Material(new Vector3(1, 1, 1), 1f),
                 0.75f
            );

            scene = new Scene()
            {
                Ambient = new Vector3(0.001f, 0.002f, 0.005f)
            };
            scene.Camera = camera;
            scene.Objects = new GpuSphere[]
            {
                sun,
                sphere1,
                sphere2,
                sphere3,
                sphere4,
                ground
            };

            Task.Run(() =>
            {

                raytracer = new Raytracer(outputResolution);

                Debug.WriteLine("Start render");

                var sw = Stopwatch.StartNew();
                raytracer.Render(scene, 10000, 10);

                var bmp = raytracer.WaitForResult();
                sw.Stop();

                Debug.WriteLine($"Wait for result: {sw.Elapsed.TotalMilliseconds} ms");

                Debug.WriteLine("Done");

                bmp.Save("render.png");

                Debug.WriteLine("Saved");

                Invoke(() =>
                {
                    pictureBox.Image = bmp;
                });

                raytracer.Dispose();
            });
        }
    }
}