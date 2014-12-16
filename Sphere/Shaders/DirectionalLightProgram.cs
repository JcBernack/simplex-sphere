using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using OpenTK;

namespace Sphere.Shaders
{
    [VertexShaderSource("Shading.Vertex")]
    [FragmentShaderSource("Shading.Fragment.Light.Directional")]
    public class DirectionalLightProgram
        : LightProgram
    {
        public Uniform<Vector3> LightDirection { get; protected set; }
    }
}