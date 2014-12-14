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
        private readonly DirectionalLightProgram _lightProgram;
        private readonly VertexArray _vaoFullScreenQuad;
        private readonly Quad _quad;

        private GBuffer _gbuffer;

        public DeferredRenderer()
        {
            _lightProgram = ProgramFactory.Create<DirectionalLightProgram>();
            _quad = new Quad();
            _quad.UpdateBuffers();
            _vaoFullScreenQuad = new VertexArray();
            _vaoFullScreenQuad.Bind();
            _vaoFullScreenQuad.BindAttribute(_lightProgram.Position, _quad.VertexBuffer);
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

        public void DirectionalLight()
        {
            _lightProgram.Use();
            _lightProgram.ModelViewProjectionMatrix.Set(Matrix4.Identity);
            _gbuffer.BindBuffers(_lightProgram);
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