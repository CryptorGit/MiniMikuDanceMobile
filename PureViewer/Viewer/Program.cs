using System;
using System.IO;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
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
            _shaderProgram = CreateBasicShader();
            _viewMatrix = Matrix4.LookAt(new Vector3(0, 0, 3), Vector3.Zero, Vector3.UnitY);
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

        private int CreateBasicShader()
        {
            string vertexShaderSource = "#version 330 core\nlayout(location=0) in vec3 aPos;\nlayout(location=1) in vec3 aNormal;\nuniform mat4 model;\nuniform mat4 view;\nuniform mat4 projection;\nvoid main(){gl_Position = projection * view * model * vec4(aPos,1.0);}";
            string fragmentShaderSource = "#version 330 core\nout vec4 FragColor;\nvoid main(){FragColor = vec4(1.0,1.0,1.0,1.0);}";
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
