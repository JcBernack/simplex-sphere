using System;
using ObjectTK;
using ObjectTK.Buffers;
using ObjectTK.Cameras;
using ObjectTK.Shaders;
using ObjectTK.Utilities;
using log4net.Config;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using Sphere.Renderer;
using Sphere.Shaders;
using Sphere.Variables;

namespace Sphere
{
    public class SphereWindow
        : DerpWindow
    {
        [Variable(Speed = 10, IncKey = Key.Right, DecKey = Key.Left)]
        public float PixelsPerEdge;
        
        [Variable(Speed = 100, IncKey = Key.Up, DecKey = Key.Down)]
        public float Radius;

        [Variable(Speed = 50, IncKey = Key.PageUp, DecKey = Key.PageDown)]
        public float HeightScale;

        [Variable(Speed = 1, IncKey = Key.Home, DecKey = Key.End)]
        public float TerrainScale;

        [Variable(Speed = 0.1f, IncKey = Key.Insert, DecKey = Key.Delete)]
        public float Persistence;

        [Variable(Speed = 1, IncKey = Key.KeypadPlus, DecKey = Key.KeypadMinus, Minimum = 1, Maximum = 10, Mode = ScaleMode.KeyPress, Function = ScaleFunction.Linear)]
        public float Octaves;

        [ToggleVariable(Key = Key.F1)]
        public bool EnableWireframe;

        [ToggleVariable(Key = Key.F2)]
        public bool EnableNoiseTexture;

        [ToggleVariable(Key = Key.F3)]
        public bool EnableFragmentNormal;

        [ToggleVariable(Key = Key.F4)]
        public bool FixedTessellation;

        private readonly VariableHandler _variableHandler;
        
        private GeodesicProgramOdd _programOdd;
        private GeodesicProgram _programEqual;
        private GeodesicProgram _program;
        
        private VertexArray _vao;
        private Icosahedron _icosahedron;
        
        private DeferredRenderer _deferredRenderer;
        
        private CameraBase _camera;
        private Matrix4 _modelMatrix;
        private Matrix4 _viewMatrix;
        private Matrix4 _modelViewMatrix;
        private Matrix4 _projectionMatrix;
        private bool _renderGBuffer;
        private GBufferType _renderbufferType;

        private const float ClipNear = 0.01f;
        private const float ClipFar = 2000;

        public SphereWindow()
            : base(800, 600, GraphicsMode.Default, "Sphere")
        {
            // disable vsync
            VSync = VSyncMode.Off;
            // initialize variable handler
            _variableHandler = new VariableHandler(this);
            // default features
            EnableNoiseTexture = true;
            EnableFragmentNormal = true;
            // set tessellation quality
            PixelsPerEdge = 50;
            // kerbin radius in [km]
            Radius = 600;
            // highest elevation on kerbin in [km]
            //HeightScale = 6.764f;
            HeightScale = 51.64162f;
            TerrainScale = 1.40356827f;
            Persistence = 0.4292259f;
            Octaves = 10;
            // set up camera
            //_camera = new ThirdPersonCamera { DefaultOrigin = new Vector3(0, Radius, 0) };
            _camera = new ThirdPersonCamera();
            _camera.Enable(this);
            _camera.DefaultPosition.Z = 3*Radius;
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
            // backface culling is done in the tesselation control shader
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
            GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
            // lighting stuff
            _deferredRenderer = new DeferredRenderer();
            // enable controls
            _variableHandler.Enable(this);
        }

        private void OnUnload(object sender, EventArgs e)
        {
            // disable controls
            _variableHandler.Disable(this);
            // dispose resources
            GLResource.DisposeAll(this);
        }

        private void OnResize(object sender, EventArgs e)
        {
            //BUG: resize event is fired on minimize with a ClientSize of 0x0 which causes all kinds of bugs
            // adjust the viewport
            GL.Viewport(ClientSize);
            // resize G buffer
            _deferredRenderer.Resize(Width, Height);
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
            Title = string.Format("Icosahedron tesselation - FPS: {0} - eye: {1} - edge length: {2} - r: {3} - h: {4} - t: {5} - p: {6}, o: {7} ",
                FrameTimer.FpsBasedOnFramesRendered, _camera.GetEyePosition(), PixelsPerEdge, Radius, HeightScale, TerrainScale, Persistence, Octaves);
            _viewMatrix = Matrix4.Identity;
            _camera.ApplyCamera(ref _viewMatrix);
            if (!FixedTessellation) Matrix4.Mult(ref _modelMatrix, ref _viewMatrix, out _modelViewMatrix);
            
            // geometry pass
            _program.Use();
            _program.ModelMatrix.Set(_modelMatrix);
            _program.ViewMatrix.Set(_viewMatrix);
            _program.ProjectionMatrix.Set(_projectionMatrix);
            _program.ModelViewMatrix.Set(_modelViewMatrix);
            _program.ModelViewProjectionMatrix.Set(_modelMatrix*_viewMatrix*_projectionMatrix);
            _program.NormalMatrix.Set(_modelMatrix.GetNormalMatrix());
            _program.EdgesPerScreenHeight.Set(Height / PixelsPerEdge);
            _program.Radius.Set(Radius);
            _program.TerrainScale.Set(TerrainScale);
            _program.HeightScale.Set(HeightScale);
            _program.Persistence.Set(Persistence);
            _program.Octaves.Set((int)Octaves);
            _program.EnableFragmentNormal.Set(EnableFragmentNormal);
            _program.EnableNoiseTexture.Set(EnableNoiseTexture);
            _program.EnableWireframe.Set(EnableWireframe);

            _deferredRenderer.BeginGeometryPass();
            _vao.Bind();
            _vao.DrawElements(PrimitiveType.Patches, _icosahedron.Indices.Length);
            _deferredRenderer.EndGeometryPass();

            if (_renderGBuffer)
            {
                _deferredRenderer.DrawGBuffer(_renderbufferType);
            }
            else
            {
                _deferredRenderer.BeginLightPass();
                var eye = _camera.GetEyePosition();
                var dirLight = new DirectionalLight
                {
                    Direction = new Vector3(0,-1,0),
                        //-(float) Math.Sin(FrameTimer.TimeRunning/1000), 0, (float) Math.Cos(FrameTimer.TimeRunning/1000)),
                    Color = new Vector3(1.0f, 0.8f, 0.2f),
                    AmbientIntensity = 0.1f,
                    DiffuseIntensity = 0.5f
                };
                _deferredRenderer.DrawDirectionalLight(eye, dirLight);
                //var light = new PointLight
                //{
                //    Position =
                //        new Vector3(0, 0,
                //            Radius + HeightScale*(1.5f + 0.5f*MathF.Sin((float) (FrameTimer.TimeRunning/500)))),
                //    Attenuation = new Vector3(0, 0.1f, 0.1f),
                //    Color = new Vector3(1),
                //    AmbientIntensity = 100,
                //    DiffuseIntensity = 100
                //};
                //var rot = Matrix3.CreateRotationY(MathF.PI/4);
                //for (var i = 0; i < 2; i++)
                //{
                //    _deferredRenderer.DrawPointLight(eye, light);
                //    Vector3.Transform(ref light.Position, ref rot, out light.Position);
                //}
                var largeLight = new PointLight
                {
                    Position = new Vector3(2*Radius, 0, 0),
                    Color = new Vector3(1,0,0)
                };
                largeLight.SetLinearRange(10*Radius, 2*Radius, 0.95f);
                _deferredRenderer.DrawPointLight(eye, largeLight);
                //largeLight.Position = new Vector3(0, 0, 2*Radius);
                //largeLight.Color = new Vector3(0,1,0);
                //_deferredRenderer.DrawPointLight(eye, largeLight);
                largeLight.Position = new Vector3(-2*Radius, 0, 0);
                largeLight.Color = new Vector3(0,0,1);
                _deferredRenderer.DrawPointLight(eye, largeLight);

                _deferredRenderer.EndLightPass();
            }

            SwapBuffers();
        }

        private void OnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            if (e.Key == Key.R) _camera.ResetToDefault();
            if (e.Key == Key.F11) _program = _programOdd;
            if (e.Key == Key.F12) _program = _programEqual;
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
            if (e.Key == Key.Number1) SetBuffer(GBufferType.Position);
            if (e.Key == Key.Number2) SetBuffer(GBufferType.Normal);
            if (e.Key == Key.Number3) SetBuffer(GBufferType.Diffuse);
            if (e.Key == Key.Number4) SetBuffer(GBufferType.Aux);
        }

        private void SetBuffer(GBufferType buffer)
        {
            if (_renderGBuffer && _renderbufferType == buffer)
            {
                _renderGBuffer = false;
                return;
            }
            _renderGBuffer = true;
            _renderbufferType = buffer;
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
