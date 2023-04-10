using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;

namespace SimpleRaytracer.Gui
{
    public partial class Window : Form
    {
        private Size outputResolution = new(600, 600);
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
            var camera = new Camera(new Vector3(0, -2, -4), 0.1f, 60, outputResolution.Width / (float)outputResolution.Height);

            camera.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -28 * (float)(Math.PI / 180));

            var sun = new GpuSphere(
                new Vector3(-10, -10, 0),
                new Material(new Vector3(0, 0, 0), new Vector3(1, 1, 0.9f) * 6),
                6f
            );

            var ground = new GpuSphere(
                new Vector3(0, 1000 + 0.75f, 0),
                new Material(new Vector3(1, 1, 1)),
                1000
            );

            // Spheres

            //var sphere1 = new GpuSphere(
            //    new Vector3(-3, 0, 0),
            //    new Material(new Vector3(0.95f, 0, 0), 0),
            //    0.75f
            //);

            //var sphere2 = new GpuSphere(
            //    new Vector3(-1, 0, 0),
            //    new Material(new Vector3(0, 0.95f, 0), 0.1f),
            //     0.75f
            //);

            //var sphere3 = new GpuSphere(
            //    new Vector3(1, 0, 0),
            //    new Material(new Vector3(0, 0, 0.95f), 0.7f),
            //     0.75f
            //);

            //var sphere4 = new GpuSphere(
            //    new Vector3(3, 0, 0),
            //    new Material(new Vector3(0.8f, 0.8f, 0.8f), 1f),
            //     0.75f
            //);

            var monkey = Mesh.LoadFromObj("models/monkey/monkey.obj");
            monkey.material = new Material(new Vector3(0, 1, 0));

            scene = new Scene()
            {
                Ambient = new Vector3(0.001f, 0.002f, 0.005f)
            };
            scene.Camera = camera;
            scene.Meshes = new Mesh[] { monkey };
            scene.Objects = new GpuSphere[]
            {
                sun,
                //sphere1,
                //sphere2,
                //sphere3,
                //sphere4,
                ground
            };

            float pitch = 0;
            float yaw = 0;
            float t = 0;

            DateTime lastTime = DateTime.Now;

            Task.Run(() =>
            {
                raytracer = new Raytracer(outputResolution);

                Debug.WriteLine($"Start render {raytracer.Accelerator.Name}");

                try
                {
                    while (true)
                    {
                        var sw = Stopwatch.StartNew();

                        var currentTime = DateTime.Now;
                        var deltaTime = (float)(currentTime - lastTime).TotalMilliseconds / 100f;
                        lastTime = currentTime;

                        if ((GetAsyncKeyState(0x57) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position + scene.Camera.Forward * 0.5f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x53) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position - scene.Camera.Forward * 0.5f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x44) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position + scene.Camera.Right * 0.5f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x41) & 0x8000) > 0)
                        {
                            scene.Camera.Position = scene.Camera.Position - scene.Camera.Right * 0.5f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x26) & 0x8000) > 0)
                        {
                            pitch += 0.2f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x28) & 0x8000) > 0)
                        {
                            pitch -= 0.2f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x25) & 0x8000) > 0)
                        {
                            yaw -= 0.2f * deltaTime;
                        }

                        if ((GetAsyncKeyState(0x27) & 0x8000) > 0)
                        {
                            yaw += 0.2f * deltaTime;
                        }

                        scene.Camera.Rotation = Quaternion.CreateFromYawPitchRoll(yaw, pitch, 0);

                        //scene.Objects[2].position.X = (float)Math.Sin(t);
                        //scene.Objects[2].position.Z = (float)Math.Cos(t);

                        t += deltaTime / 10;

                        raytracer.Render(scene, 100, 10);

                        var bmp = raytracer.WaitForResult();
                        sw.Stop();

                        Debug.WriteLine($"Wait for result: {sw.Elapsed.TotalMilliseconds} ms");

                        Debug.WriteLine("Done");

                        //bmp.Save("render.png");

                        Debug.WriteLine("Saved");


                        Invoke(() =>
                        {
                            var old = pictureBox.Image;
                            pictureBox.Image = bmp;
                            old?.Dispose();
                        });
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