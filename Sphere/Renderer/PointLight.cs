using OpenTK;

namespace Sphere.Renderer
{
    public class PointLight
        : LightBase
    {
        public Vector3 Position;
        public Vector3 Attenuation;

        /// <summary>
        /// Division factor at which every 8-bit color channel will be black.
        /// </summary>
        private const int A = 65536;

        public void SetLinearRange(float range, float distance, float intensity)
        {
            var a = A / range;
            var i0 = distance * intensity * a;
            DiffuseIntensity = i0;
            AmbientIntensity = DiffuseIntensity*(1-intensity);
            Attenuation = new Vector3(0, a, 0);
        }
    }
}