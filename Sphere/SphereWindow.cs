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
        private GeodesicProgramEqual _programEqual;
        private GeodesicProgramOdd _programOdd;
        private GeodesicProgram _program;
        private VertexArray _vao;

        private float _terrainScale;
        private Icosahedron _icosahedron;

        private CameraBase _camera;
        private Matrix4 _modelMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelViewMatrix;
        private Matrix4 _projectionMatrix;

        private const float ClipNear = 2;
        private const float ClipFar = 1000;
        private float _pixelsPerEdge = 30;
        private float _radius;
        private float _heightScale;

        public SphereWindow()
            : base(800, 600, GraphicsMode.Default, "Sphere")
        {
            // disable vsync
            VSync = VSyncMode.Off;
            // set up camera
            _camera = new ThirdPersonCamera();
            _camera.Enable(this);
            _camera.DefaultPosition.Z = 700;
            _camera.ResetToDefault();
            // set default tesselation levels
            _terrainScale = 1;
            _heightScale = 1;
            _radius = 50;
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
            _programEqual = ProgramFactory.Create<GeodesicProgramEqual>();
            _program = _programOdd;
            // create icosahedron and set model matrix
            _modelMatrix = Matrix4.CreateScale(1);
            _icosahedron = new Icosahedron(4);
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
            GL.CullFace(CullFaceMode.Front);
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
            //_modelMatrix *= Matrix4.CreateRotationX((float) e.Time * 0.2f);
        }

        private void OnRender(object sender, FrameEventArgs e)
        {
            Title = string.Format("Icosahedron tesselation - edge length: {3} - terrain scale: {0} - FPS: {1} - eye: {2}",
                _terrainScale, FrameTimer.FpsBasedOnFramesRendered, _camera.GetEyePosition(), _pixelsPerEdge);
            _viewMatrix = Matrix4.Identity;
            _camera.ApplyCamera(ref _viewMatrix);
            Matrix4.Mult(ref _modelMatrix, ref _viewMatrix, out _modelViewMatrix);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            _program.Use();
            _program.AmbientMaterial.Set(new Vector3(0.2f, 0.2f, 0.2f));
            _program.DiffuseMaterial.Set(new Vector3(0.25f, 0.75f, 0.75f));
            _program.LightPosition.Set(new Vector3(0, 2000, 10));
            _program.ModelMatrix.Set(_modelMatrix);
            _program.ViewMatrix.Set(_viewMatrix);
            _program.ProjectionMatrix.Set(_projectionMatrix);
            _program.ModelViewMatrix.Set(_modelViewMatrix);
            _program.ModelViewProjectionMatrix.Set(_modelViewMatrix*_projectionMatrix);
            _program.NormalMatrix.Set(_modelMatrix.GetNormalMatrix());
            _program.EdgesPerScreenHeight.Set(Height / _pixelsPerEdge);
            _program.Radius.Set(_radius);
            _program.TerrainScale.Set(_terrainScale);
            _program.HeightScale.Set(_heightScale);
            _vao.DrawElements(PrimitiveType.Patches, _icosahedron.Indices.Length);
            SwapBuffers();
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.R) _camera.ResetToDefault();
            if (e.Key == Key.Up) _radius *= 1.01f;
            if (e.Key == Key.Down) _radius *= 0.99f;
            if (e.Key == Key.Right) _pixelsPerEdge += 1;
            if (e.Key == Key.Left) _pixelsPerEdge -= 1;
            if (e.Key == Key.PageUp) _heightScale *= 1.1f;
            if (e.Key == Key.PageDown) _heightScale *= 0.9f;
            if (e.Key == Key.Home) _terrainScale *= 1.01f;
            if (e.Key == Key.End) _terrainScale *= 0.99f;
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
