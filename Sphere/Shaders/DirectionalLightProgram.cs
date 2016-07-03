using ObjectTK.Shaders.Sources;
using ObjectTK.Shaders.Variables;
using OpenTK;

namespace Sphere.Shaders
{
    [FragmentShaderSource("Shading.Fragment.Light.Directional")]
    public class DirectionalLightProgram
        : LightProgram
    {
        public Uniform<Vector3> LightDirection { get; protected set; }
    }
}