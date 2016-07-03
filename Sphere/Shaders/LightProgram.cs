using ObjectTK.Shaders.Sources;
using ObjectTK.Shaders.Variables;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    [VertexShaderSource("Shading.Vertex")]
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
}