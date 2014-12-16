using OpenTK;

namespace Sphere.Renderer
{
    public class PointLight
        : LightBase
    {
        public Vector3 Position;
        public Vector3 Attenuation;
    }
}