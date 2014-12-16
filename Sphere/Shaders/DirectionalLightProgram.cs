using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using DerpGL.Textures;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    public abstract class GBufferProgram
        : Program
    {
        public Uniform<Vector2> InverseScreenSize { get; protected set; }
        public TextureUniform<Texture2D> GPosition { get; protected set; }
        public TextureUniform<Texture2D> GDiffuse { get; protected set; }
        public TextureUniform<Texture2D> GNormal { get; protected set; }
        public TextureUniform<Texture2D> GTexCoord { get; protected set; }
    }

    public abstract class LightProgram
        : GBufferProgram
    {
        [VertexAttrib(3, VertexAttribPointerType.Float)]
        public VertexAttrib Position { get; protected set; }

        public Uniform<Matrix4> ModelViewProjectionMatrix { get; protected set; }
        public Uniform<Vector3> EyePosition { get; protected set; }
        
        public Uniform<Vector3> LightColor { get; protected set; }
        public Uniform<float> AmbientIntensity { get; protected set; }
        public Uniform<float> DiffuseIntensity { get; protected set; }
    }

    [VertexShaderSource("Shading.Vertex")]
    [FragmentShaderSource("Shading.Fragment.Light.Directional")]
    public class DirectionalLightProgram
        : LightProgram
    {
        public Uniform<Vector3> LightDirection { get; protected set; }
    }

    [VertexShaderSource("Shading.Vertex")]
    [FragmentShaderSource("Shading.Fragment.Light.Point")]
    public class PointLightProgram
        : LightProgram
    {
        public Uniform<Vector3> LightPosition { get; protected set; }
        public Uniform<Vector3> Attenuation { get; protected set; }
    }
}