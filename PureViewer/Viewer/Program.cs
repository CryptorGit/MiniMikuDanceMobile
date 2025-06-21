using System;
using System.IO;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Assimp;

namespace ViewerApp
{
    public class Viewer : GameWindow
    {
        private readonly string _modelPath;
        private int _vao;
        private int _vbo;
        private int _ebo;
        private int _shaderProgram;
        private int _vertexCount;
        private Matrix4 _modelMatrix = Matrix4.Identity;
        private Matrix4 _viewMatrix;
        private Matrix4 _projectionMatrix;
        private int _modelLocation;
        private int _viewLocation;
        private int _projectionLocation;
        private float _rotation;
        private Vector3 _cameraPos = new(0, 0, 3);
        private float _yaw = -90f;
        private float _pitch;
        private Vector2 _lastMouse;
        private bool _firstMove = true;

        public Viewer(string modelPath) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
        {
            _modelPath = modelPath;
            Title = "MiniMikuDance Viewer";
            Size = new Vector2i(800, 600);
        }

        protected override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(Color4.CornflowerBlue);
            GL.Enable(EnableCap.DepthTest);
            LoadModel(_modelPath);
            string shaderDir = Path.Combine(AppContext.BaseDirectory, "Shaders");
            string vert = Path.Combine(shaderDir, "basic.vert");
            string frag = Path.Combine(shaderDir, "basic.frag");
            _shaderProgram = CreateShaderProgram(vert, frag);
            _viewMatrix = Matrix4.LookAt(_cameraPos, Vector3.Zero, Vector3.UnitY);
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100f);
            _modelLocation = GL.GetUniformLocation(_shaderProgram, "model");
            _viewLocation = GL.GetUniformLocation(_shaderProgram, "view");
            _projectionLocation = GL.GetUniformLocation(_shaderProgram, "projection");
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);
            GL.Viewport(0, 0, Size.X, Size.Y);
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(45f), Size.X / (float)Size.Y, 0.1f, 100f);
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            _rotation += (float)args.Time;
            _modelMatrix = Matrix4.CreateRotationY(_rotation);

            float speed = 2.5f * (float)args.Time;
            var input = KeyboardState;
            Vector3 forward = GetForwardVector();
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            if (input.IsKeyDown(Keys.W)) _cameraPos += forward * speed;
            if (input.IsKeyDown(Keys.S)) _cameraPos -= forward * speed;
            if (input.IsKeyDown(Keys.A)) _cameraPos -= right * speed;
            if (input.IsKeyDown(Keys.D)) _cameraPos += right * speed;

            var mouse = MouseState;
            if (_firstMove)
            {
                _lastMouse = new Vector2(mouse.X, mouse.Y);
                _firstMove = false;
            }
            else
            {
                var deltaX = mouse.X - _lastMouse.X;
                var deltaY = mouse.Y - _lastMouse.Y;
                _lastMouse = new Vector2(mouse.X, mouse.Y);
                const float sensitivity = 0.2f;
                _yaw += deltaX * sensitivity;
                _pitch -= deltaY * sensitivity;
                _pitch = Math.Clamp(_pitch, -89f, 89f);
            }

            _viewMatrix = Matrix4.LookAt(_cameraPos, _cameraPos + GetForwardVector(), Vector3.UnitY);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.UseProgram(_shaderProgram);
            GL.UniformMatrix4(_modelLocation, false, ref _modelMatrix);
            GL.UniformMatrix4(_viewLocation, false, ref _viewMatrix);
            GL.UniformMatrix4(_projectionLocation, false, ref _projectionMatrix);
            GL.BindVertexArray(_vao);
            GL.DrawElements(OpenTK.Graphics.OpenGL4.PrimitiveType.Triangles, _vertexCount, DrawElementsType.UnsignedInt, 0);
            SwapBuffers();
        }

        protected override void OnUnload()
        {
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
            GL.DeleteProgram(_shaderProgram);
            base.OnUnload();
        }

        private void LoadModel(string path)
        {
            var importer = new AssimpContext();
            var scene = importer.ImportFile(path, PostProcessSteps.Triangulate | PostProcessSteps.GenerateNormals);
            var mesh = scene.Meshes[0];
            float[] vertices = new float[mesh.VertexCount * 6];
            for (int i = 0; i < mesh.VertexCount; i++)
            {
                vertices[i * 6 + 0] = mesh.Vertices[i].X;
                vertices[i * 6 + 1] = mesh.Vertices[i].Y;
                vertices[i * 6 + 2] = mesh.Vertices[i].Z;
                vertices[i * 6 + 3] = mesh.Normals[i].X;
                vertices[i * 6 + 4] = mesh.Normals[i].Y;
                vertices[i * 6 + 5] = mesh.Normals[i].Z;
            }
            uint[] indices = mesh.GetUnsignedIndices();
            _vertexCount = indices.Length;
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();
            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), 3 * sizeof(float));
            GL.BindVertexArray(0);
        }

        private int CreateShaderProgram(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);
            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            int program = GL.CreateProgram();
            GL.AttachShader(program, vertexShader);
            GL.AttachShader(program, fragmentShader);
            GL.LinkProgram(program);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            return program;
        }

        private Vector3 GetForwardVector()
        {
            Vector3 forward;
            forward.X = MathF.Cos(MathHelper.DegreesToRadians(_pitch)) * MathF.Cos(MathHelper.DegreesToRadians(_yaw));
            forward.Y = MathF.Sin(MathHelper.DegreesToRadians(_pitch));
            forward.Z = MathF.Cos(MathHelper.DegreesToRadians(_pitch)) * MathF.Sin(MathHelper.DegreesToRadians(_yaw));
            return Vector3.Normalize(forward);
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            string modelPath = args.Length > 0 ? args[0] : Path.Combine("..", "Assets", "Models", "sample.obj");
            using var viewer = new Viewer(modelPath);
            viewer.Run();
        }
    }
}
