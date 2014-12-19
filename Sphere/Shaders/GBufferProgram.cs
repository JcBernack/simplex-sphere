using ObjectTK.Shaders;
using ObjectTK.Shaders.Variables;
using ObjectTK.Textures;
using OpenTK;

namespace Sphere.Shaders
{
    public abstract class GBufferProgram
        : Program
    {
        public Uniform<Vector2> InverseScreenSize { get; protected set; }
        public TextureUniform<Texture2D> GPosition { get; protected set; }
        public TextureUniform<Texture2D> GNormal { get; protected set; }
        public TextureUniform<Texture2D> GDiffuse { get; protected set; }
        public TextureUniform<Texture2D> GAux { get; protected set; }
    }
}