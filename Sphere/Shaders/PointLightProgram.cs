using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using OpenTK;

namespace Sphere.Shaders
{
    [VertexShaderSource("Shading.Vertex")]
    [FragmentShaderSource("Shading.Fragment.Light.Point")]
    public class PointLightProgram
        : LightProgram
    {
        public Uniform<Vector3> LightPosition { get; protected set; }
        public Uniform<Vector3> Attenuation { get; protected set; }
    }
}