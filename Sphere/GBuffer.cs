using System;
using System.Collections.Generic;
using DerpGL.Buffers;
using DerpGL.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Sphere.Shaders;

namespace Sphere
{
    public enum GBufferType
    {
        Position,
        Diffuse,
        Normal,
        TexCoord
    }

    /// <summary>
    /// TODO: properly dispose GL resources
    /// </summary>
    public class GBuffer
    {
        private readonly int _width;
        private readonly int _height;

        private readonly FrameBuffer _fbo;
        private readonly Dictionary<GBufferType, Texture2D> _textures;
        private readonly Texture2D _depthTexture;

        public GBuffer(int width, int height)
        {
            _width = width;
            _height = height;
            _fbo = new FrameBuffer();
            _textures = new Dictionary<GBufferType, Texture2D>();
            _fbo.Bind(FramebufferTarget.Framebuffer);
            // add depth texture
            _depthTexture = new Texture2D((SizedInternalFormat)All.DepthComponent32f, width, height);
            _fbo.Attach(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, _depthTexture);
            // add color textures
            var i = 0;
            var attachments = new List<DrawBuffersEnum>();
            foreach (GBufferType bufferType in Enum.GetValues(typeof (GBufferType)))
            {
                var texture = new Texture2D(SizedInternalFormat.Rgba32f, width, height);
                var attachment = FramebufferAttachment.ColorAttachment0 + i++;
                _fbo.Attach(FramebufferTarget.Framebuffer, attachment, texture);
                _textures.Add(bufferType, texture);
                attachments.Add((DrawBuffersEnum)attachment);
            }
            // enable all color attachments
            GL.DrawBuffers(attachments.Count, attachments.ToArray());
            // check if everything went ok
            _fbo.CheckState(FramebufferTarget.Framebuffer);
            FrameBuffer.Unbind(FramebufferTarget.Framebuffer);
        }

        public void DrawBuffer(GBufferType buffer)
        {
            Bind(FramebufferTarget.ReadFramebuffer);
            SetReadBuffer(buffer);
            GL.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            Unbind(FramebufferTarget.ReadFramebuffer);
        }

        public void DumpToScreen()
        {
            // make sure the default framebuffer is bound and cleared
            FrameBuffer.Unbind(FramebufferTarget.Framebuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            // bind the framebuffer for reading
            Bind(FramebufferTarget.ReadFramebuffer);
            var w = _width;
            var h = _height;
            var w2 = w / 2;
            var h2 = h / 2;
            SetReadBuffer(GBufferType.Position);
            GL.BlitFramebuffer(0, 0, w, h, 0, 0, w2, h2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            SetReadBuffer(GBufferType.Diffuse);
            GL.BlitFramebuffer(0, 0, w, h, w2, 0, w, h2, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            SetReadBuffer(GBufferType.Normal);
            GL.BlitFramebuffer(0, 0, w, h, 0, h2, w2, h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            SetReadBuffer(GBufferType.TexCoord);
            GL.BlitFramebuffer(0, 0, w, h, w2, h2, w, h, ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            Unbind(FramebufferTarget.ReadFramebuffer);
        }

        public void Bind(FramebufferTarget target)
        {
            _fbo.Bind(target);
        }

        public void Unbind(FramebufferTarget target)
        {
            FrameBuffer.Unbind(target);
        }

        private void SetReadBuffer(GBufferType buffer)
        {
            _fbo.AssertActive(FramebufferTarget.ReadFramebuffer);
            GL.ReadBuffer(ReadBufferMode.ColorAttachment0 + (int)buffer);
        }

        public void BindBuffers(GBufferProgram program)
        {
            program.InverseScreenSize.Set(new Vector2(1f / _width, 1f / _height));
            program.GPosition.BindTexture(TextureUnit.Texture0, _textures[GBufferType.Position]);
            program.GDiffuse.BindTexture(TextureUnit.Texture1, _textures[GBufferType.Diffuse]);
            program.GNormal.BindTexture(TextureUnit.Texture2, _textures[GBufferType.Normal]);
            program.GTexCoord.BindTexture(TextureUnit.Texture3, _textures[GBufferType.TexCoord]);
        }
    }
}