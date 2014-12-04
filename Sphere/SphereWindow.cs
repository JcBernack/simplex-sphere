using System;
using DerpGL;
using DerpGL.Buffers;
using DerpGL.Cameras;
using DerpGL.Shaders;
using DerpGL.Utilities;
using log4net.Config;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Sphere.Shaders;

namespace Sphere
{
    public class SphereWindow
        : DerpWindow
    {
        private GeodesicProgram _program;
        private VertexArray _vao;

        private float _tessellationScale;
        private Icosahedron _icosahedron;

        private readonly CameraBase _camera;
        private Matrix4 _modelMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelViewMatrix;
        private Matrix4 _projectionMatrix;

        private const float ClipNear = 0.1f;
        private const float ClipFar = 100;

        public SphereWindow()
            : base(800, 600, GraphicsMode.Default, "Sphere")
        {
            // disable vsync
            VSync = VSyncMode.Off;
            // set up camera
            _camera = new FirstPersonCamera();
            _camera.Enable(this);
            _camera.DefaultPosition.Z = 20;
            _camera.ResetToDefault();
            // set default tesselation levels
            _tessellationScale = 0.15f;
            // hook up events
            RenderFrame += OnRender;
            Load += OnLoad;
            Unload += OnUnload;
            KeyDown += OnKeyDown;
            Resize += OnResize;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // maximize window
            WindowState = WindowState.Maximized;
            // load program
            _program = ProgramFactory.Create<GeodesicProgram>();
            _program.Use();
            _program.AmbientMaterial.Set(new Vector3(0.04f, 0.04f, 0.04f));
            _program.DiffuseMaterial.Set(new Vector3(0, 0.75f, 0.75f));
            _program.ClipNear.Set(ClipNear);
            _program.ClipFar.Set(ClipFar);
            // create icosahedron and set model matrix
            //TODO: translation is kaputt
            _modelMatrix = Matrix4.CreateScale(1) * Matrix4.CreateTranslation(0, 0, 0);
            _icosahedron = new Icosahedron();
            _icosahedron.UpdateBuffers();
            // bind it to an vao
            _vao = new VertexArray();
            _vao.Bind();
            _vao.BindElementBuffer(_icosahedron.IndexBuffer);
            _vao.BindAttribute(_program.Position, _icosahedron.VertexBuffer);
            // set some reasonable default state
            GL.ClearColor(Color4.Black);
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
        }

        private void OnUnload(object sender, EventArgs e)
        {
            _icosahedron.Dispose();
            GLResource.DisposeAll(this);
        }

        private void OnResize(object sender, EventArgs e)
        {
            // adjust the viewport
            GL.Viewport(ClientSize);
            // adjust the projection matrix
            var aspectRatio = Width / (float)Height;
            _projectionMatrix = Matrix4.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, ClipNear, ClipFar);
        }

        private void OnRender(object sender, FrameEventArgs e)
        {
            var eye = _camera.GetEyePosition();
            Title = string.Format("Icosahedron tesselation level - tessellation scale: {0} - FPS: {1} - eye: {2}", _tessellationScale, FrameTimer.FpsBasedOnFramesRendered, eye);
            _viewMatrix = Matrix4.Identity;
            _camera.ApplyCamera(ref _viewMatrix);
            Matrix4.Mult(ref _modelMatrix, ref _viewMatrix, out _modelViewMatrix);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
            _program.ModelMatrix.Set(_modelMatrix);
            _program.ViewMatrix.Set(_viewMatrix);
            _program.ProjectionMatrix.Set(_projectionMatrix);
            _program.ModelViewMatrix.Set(_modelViewMatrix);
            _program.ModelViewProjectionMatrix.Set(_modelViewMatrix*_projectionMatrix);
            _program.NormalMatrix.Set(new Matrix3(_modelViewMatrix));
            _program.CameraPosition.Set(_camera.GetEyePosition());
            _program.LightPosition.Set(new Vector3(0.25f, 0.25f, 1));
            _program.TessellationScale.Set(_tessellationScale);
            _vao.DrawElements(PrimitiveType.Patches, _icosahedron.IndexBuffer.ElementCount);
            SwapBuffers();
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.R) _camera.ResetToDefault();
            const float inc = 1 + 0.1f;
            const float dec = 1 - 0.1f;
            if (e.Key == Key.Up) _tessellationScale *= inc;
            if (e.Key == Key.Down) _tessellationScale *= dec;
        }

        public static void Main(string[] args)
        {
            // initialize log4net via app.config
            XmlConfigurator.Configure();
            // run game window
            using (var window = new SphereWindow())
            {
                window.Run();
            }
        }
    }
}
