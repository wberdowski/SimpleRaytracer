using ILGPU;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using System.Diagnostics;
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
                new Vector3(-4, -4, -1),
                new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 10),
                2f
            );

            var sphere = new GpuSphere(
                new Vector3(0, 0, 0),
                new Material(new Vector3(1, 0, 0)),
                1f
            );

            var ground = new GpuSphere(
                new Vector3(0, 1000 + 1, 0),
                new Material(new Vector3(1, 1, 1)),
                1000
            );

            scene = new Scene();
            scene.Camera = camera;
            scene.Objects = new GpuSphere[]
            {
                sun,
                sphere,
                ground
            };

            Task.Run(() =>
            {
                using var context = Context.Create(x => x.Cuda().CPU().EnableAlgorithms());

                using var accelerator = context.GetPreferredDevice(false)
                    .CreateAccelerator(context);

                Debug.WriteLine($"Running on: {accelerator.Name}");

                raytracer = new Raytracer(scene, outputResolution, accelerator);

                Debug.WriteLine("Start render");

                var sw = Stopwatch.StartNew();
                raytracer.Render(1000, 10);

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
            });
        }
    }
}