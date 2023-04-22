using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleRaytracer.Gui
{
    public partial class Window : Form
    {
        private Size outputResolution = new(1280, 720);
        private Scene scene;
        private Raytracer raytracer;

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int key);

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
            var camera = new Camera(new Vector3(0, 2.3f, -4), 0.1f, 60, outputResolution.Width / (float)outputResolution.Height);

            var sun = new GpuSphere(
                new Vector3(-10, 10, 0),
                new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 6),
                6f
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
                new Material(new Vector3(0.8f, 0.8f, 0.8f), 1f),
                 0.75f
            );

            var triangles = new List<Triangle>();

            var ground = MeshGenerator.LoadPlaneData(
                new Vector3(0, 0, 0),
                new Material(new Vector3(0.2f, 0.2f, 0.2f), 0.9f),
                10,
                ref triangles
            );

            var monkey = new Mesh(
                new Vector3(0, 1.5f, 0),
                new Material(new Vector3(1, 0.7f, 0), 0f)
            );

            monkey.LoadFromObj("models/suzanne/suzanne.obj", ref triangles);

            scene = new Scene()
            {
                Ambient = new Vector3(0.2f, 0.3f, 0.5f)
            };
            scene.Camera = camera;
            scene.Meshes = new Mesh[] { ground, monkey };
            scene.Triangles = triangles.ToArray();
            //scene.Triangles = monkey.Item2;
            scene.Objects = new GpuSphere[]
            {
                sun,
                //sphere1,
                //sphere2,
                //sphere3,
                //sphere4,
            };

            float pitch = 26 * (float)(Math.PI / 180);
            float yaw = 0 * (float)(Math.PI / 180);
            float t = 0;

            DateTime lastTime = DateTime.Now;

            Task.Run(() =>
            {
                raytracer = new Raytracer(outputResolution);

                Debug.WriteLine($"Start render {raytracer.Accelerator.Name}");

                var bmp = new Bitmap(outputResolution.Width, outputResolution.Height);

                try
                {
                    scene.Camera.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);

                    raytracer.SetScene(scene);

                    while (true)
                    {
                        var sw = Stopwatch.StartNew();

                        var currentTime = DateTime.Now;
                        var deltaTime = (float)(currentTime - lastTime).TotalMilliseconds;
                        lastTime = currentTime;

                        if ((GetAsyncKeyState(0x57) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position + scene.Camera.Forward * 0.5f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x53) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position - scene.Camera.Forward * 0.5f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x44) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position + scene.Camera.Right * 0.5f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x41) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position - scene.Camera.Right * 0.5f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x26) & 0x8000) > 0)
                        {
                            pitch += 0.2f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x28) & 0x8000) > 0)
                        {
                            pitch -= 0.2f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x25) & 0x8000) > 0)
                        {
                            yaw -= 0.2f * deltaTime / 100f;
                        }

                        if ((GetAsyncKeyState(0x27) & 0x8000) > 0)
                        {
                            yaw += 0.2f * deltaTime / 100f;
                        }

                        scene.Camera.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);

                        t += deltaTime / 1000f;

                        raytracer.Render(Vector3.Normalize(new Vector3((float)Math.Sin(t), 1, (float)Math.Cos(t))), 1000, 10);

                        raytracer.WaitForResult(ref bmp);
                        sw.Stop();

                        Debug.WriteLine($"Wait for result: {sw.Elapsed.TotalMilliseconds} ms");

                        Debug.WriteLine("Done");

                        //Directory.CreateDirectory("renders");
                        //bmp.Save($"renders/render_{DateTime.Now.Ticks}.png");

                        Debug.WriteLine("Saved");

                        Invoke(() =>
                        {
                            pictureBox.Image = bmp;
                        });
                        Thread.Sleep(10);
                    }

                }
                finally
                {
                    raytracer.Dispose();
                }
            });
        }
    }
}