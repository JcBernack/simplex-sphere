using DerpGL;
using DerpGL.Buffers;
using DerpGL.Shaders;
using DerpGL.Shapes;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Sphere.Shaders;

namespace Sphere
{
    public class DeferredRenderer
        : GLResource
    {
        private readonly DirectionalLightProgram _directionalProgram;
        private readonly PointLightProgram _pointProgram;
        private readonly VertexArray _vaoFullScreenQuad;
        private readonly Quad _quad;

        private GBuffer _gbuffer;

        public DeferredRenderer()
        {
            _directionalProgram = ProgramFactory.Create<DirectionalLightProgram>();
            _pointProgram = ProgramFactory.Create<PointLightProgram>();
            _quad = new Quad();
            _quad.UpdateBuffers();
            _vaoFullScreenQuad = new VertexArray();
            _vaoFullScreenQuad.Bind();
            _vaoFullScreenQuad.BindAttribute(_directionalProgram.Position, _quad.VertexBuffer);
        }

        protected override void Dispose(bool manual)
        {
            DisposeAll(this);
        }

        public void Resize(int width, int height)
        {
            if (_gbuffer != null) _gbuffer.Dispose();
            _gbuffer = new GBuffer(width, height);
        }

        public void BeginGeometryPass()
        {
            _gbuffer.Bind(FramebufferTarget.DrawFramebuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        public void EndGeometryPass()
        {
            _gbuffer.Unbind(FramebufferTarget.DrawFramebuffer);
        }

        public void BeginLightPass()
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);
            // not needed imho, because the depth buffer is not written if depth test is disabled
            //GL.DepthMask(true);
            GL.Enable(EnableCap.Blend);
            GL.BlendEquation(BlendEquationMode.FuncAdd);
            GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);
            _gbuffer.Bind(FramebufferTarget.ReadFramebuffer);
        }

        public void EndLightPass()
        {
            _gbuffer.Unbind(FramebufferTarget.ReadFramebuffer);
            GL.Disable(EnableCap.Blend);
            //GL.DepthMask(false);
            GL.Enable(EnableCap.DepthTest);
        }

        /// <summary>
        /// Base light properties:
        /// vec3 Color
        /// float AmbientIntensity
        /// float DiffuseIntensity
        /// 
        /// Directional light properties:
        /// vec3 Direction
        /// 
        /// Material properties:
        /// float SpecularIntensity
        /// float SpecularPower (material or global?)
        /// 
        /// Required data:
        /// vec3 EyeWorldPosition
        /// </summary>
        public void DrawDirectionalLight(Vector3 eyePosition, DirectionalLight light)
        {
            _directionalProgram.Use();
            _directionalProgram.ModelViewProjectionMatrix.Set(Matrix4.Identity);
            _directionalProgram.EyePosition.Set(eyePosition);
            // set light properties
            _directionalProgram.LightDirection.Set(light.Direction);
            _directionalProgram.LightColor.Set(light.Color);
            _directionalProgram.AmbientIntensity.Set(light.AmbientIntensity);
            _directionalProgram.DiffuseIntensity.Set(light.DiffuseIntensity);
            _gbuffer.BindBuffers(_directionalProgram);
            // draw fullscreen quad
            _vaoFullScreenQuad.Bind();
            _vaoFullScreenQuad.DrawArrays(_quad.DefaultMode, 0, _quad.Vertices.Length);
        }

        public struct DirectionalLight
        {
            public Vector3 Direction;
            public Vector3 Color;
            public float AmbientIntensity;
            public float DiffuseIntensity;
        }

        public struct PointLight
        {
            public Vector3 Position;
            public Vector3 Attenuation;
            public Vector3 Color;
            public float AmbientIntensity;
            public float DiffuseIntensity;
        }

        public void DrawPointLight(Vector3 eyePosition, PointLight light)
        {
            _pointProgram.Use();
            _pointProgram.ModelViewProjectionMatrix.Set(Matrix4.Identity);
            _pointProgram.EyePosition.Set(eyePosition);
            // set light properties
            _pointProgram.LightPosition.Set(light.Position);
            _pointProgram.Attenuation.Set(light.Attenuation);
            _pointProgram.LightColor.Set(light.Color);
            _pointProgram.AmbientIntensity.Set(light.AmbientIntensity);
            _pointProgram.DiffuseIntensity.Set(light.DiffuseIntensity);
            // bind gbuffer
            _gbuffer.BindBuffers(_pointProgram);
            // draw fullscreen quad
            _vaoFullScreenQuad.Bind();
            _vaoFullScreenQuad.DrawArrays(_quad.DefaultMode, 0, _quad.Vertices.Length);
        }

        public void DrawGBuffer()
        {
            _gbuffer.DumpToScreen();
        }

        public void DrawGBuffer(GBufferType buffer)
        {
            _gbuffer.DrawBuffer(buffer);
        }
    }
}