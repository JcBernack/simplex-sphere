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
using Sphere.Variables;

namespace Sphere
{
    public class SphereWindow
        : DerpWindow
    {
        [Variable(Key.Right, Key.Left, VariableScaling.Linear, 10)]
        public float PixelsPerEdge = 30;

        [Variable(Key.Up, Key.Down, VariableScaling.Linear, 100)]
        public float Radius;

        [Variable(Key.PageUp, Key.PageDown, VariableScaling.Linear, 50)]
        public float HeightScale;

        [Variable(Key.Home, Key.End, VariableScaling.Linear, 1)]
        public float TerrainScale;

        private readonly VariableHandler _variableHandler;

        private GeodesicProgramOdd _programOdd;
        private GeodesicProgram _programEqual;
        private GeodesicProgram _program;
        private VertexArray _vao;

        private Icosahedron _icosahedron;

        private CameraBase _camera;
        private Matrix4 _modelMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelViewMatrix;
        private Matrix4 _projectionMatrix;

        private const float ClipNear = 2;
        private const float ClipFar = 2000;

        public SphereWindow()
            : base(800, 600, GraphicsMode.Default, "Sphere")
        {
            // disable vsync
            VSync = VSyncMode.Off;
            // initialize variable handler
            _variableHandler = new VariableHandler(this);
            // kerbin radius in [km]
            Radius = 600;
            // highest elevation on kerbin in [km]
            HeightScale = 6.764f;
            TerrainScale = 2;
            // set up camera
            _camera = new ThirdPersonCamera { DefaultOrigin = new Vector3(0, Radius, 0) };
            _camera.Enable(this);
            _camera.DefaultPosition.Z = 1000;
            _camera.ResetToDefault();
            // hook up events
            Load += OnLoad;
            Unload += OnUnload;
            RenderFrame += OnRender;
            UpdateFrame += OnUpdate;
            KeyDown += OnKeyDown;
            Resize += OnResize;
        }

        private void OnLoad(object sender, EventArgs e)
        {
            // maximize window
            WindowState = WindowState.Maximized;
            // load program
            _programOdd = ProgramFactory.Create<GeodesicProgramOdd>();
            _programEqual = ProgramFactory.Create<GeodesicProgram>();
            _program = _programOdd;
            // create icosahedron and set model matrix
            _modelMatrix = Matrix4.CreateScale(1);
            _icosahedron = new Icosahedron(5);
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
            GL.CullFace(CullFaceMode.Back);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
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

        private void OnUpdate(object sender, FrameEventArgs e)
        {
            _variableHandler.Update((float)e.Time);
            //_modelMatrix *= Matrix4.CreateRotationX((float) e.Time * 0.2f);
        }

        private void OnRender(object sender, FrameEventArgs e)
        {
            Title = string.Format("Icosahedron tesselation - edge length: {0} - FPS: {1} - eye: {2}",
                PixelsPerEdge, FrameTimer.FpsBasedOnFramesRendered, _camera.GetEyePosition());
            _viewMatrix = Matrix4.Identity;
            _camera.ApplyCamera(ref _viewMatrix);
            Matrix4.Mult(ref _modelMatrix, ref _viewMatrix, out _modelViewMatrix);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _program.Use();
            _program.ModelMatrix.Set(_modelMatrix);
            _program.ViewMatrix.Set(_viewMatrix);
            _program.ProjectionMatrix.Set(_projectionMatrix);
            _program.ModelViewMatrix.Set(_modelViewMatrix);
            _program.ModelViewProjectionMatrix.Set(_modelViewMatrix*_projectionMatrix);
            _program.NormalMatrix.Set(_modelMatrix.GetNormalMatrix());
            _program.EdgesPerScreenHeight.Set(Height / PixelsPerEdge);
            _program.Radius.Set(Radius);
            _program.TerrainScale.Set(TerrainScale);
            _program.HeightScale.Set(HeightScale);
            _vao.DrawElements(PrimitiveType.Patches, _icosahedron.Indices.Length);
            SwapBuffers();
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.R) _camera.ResetToDefault();
            if (e.Key == Key.F1) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            if (e.Key == Key.F2) GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            if (e.Key == Key.F3) _program = _programOdd;
            if (e.Key == Key.F4) _program = _programEqual;
            if (e.Key == Key.F5 || e.Key == Key.F6)
            {
                _camera.Disable(this);
                var eye = _camera.GetEyePosition();
                switch (e.Key)
                {
                    case Key.F5: _camera = new ThirdPersonCamera(); break;
                    case Key.F6: _camera = new FirstPersonCamera(); break;
                }
                _camera.Position = eye;
                _camera.Enable(this);
            }
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
