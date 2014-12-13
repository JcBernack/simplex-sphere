using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using DerpGL.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    public class GBufferProgram
        : Program
    {
        public Uniform<Vector2> InverseScreenSize { get; protected set; }
        public TextureUniform<Texture2D> GPosition { get; protected set; }
        public TextureUniform<Texture2D> GDiffuse { get; protected set; }
        public TextureUniform<Texture2D> GNormal { get; protected set; }
        public TextureUniform<Texture2D> GTexCoord { get; protected set; }
    }

    [VertexShaderSource("Shading.Vertex")]
    [FragmentShaderSource("Shading.Fragment.Light.Directional")]
    public class DirectionalLightProgram
        : GBufferProgram
    {
        [VertexAttrib(3, VertexAttribPointerType.Float)]
        public VertexAttrib Position { get; protected set; }

        public Uniform<Matrix4> ModelViewProjectionMatrix { get; protected set; }
    }
}