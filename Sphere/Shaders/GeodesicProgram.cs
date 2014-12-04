using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    [VertexShaderSource("Geodesic.Vertex")]
    [TessControlShaderSource("Geodesic.TessControl")]
    [TessEvaluationShaderSource("Geodesic.TessEval")]
    [GeometryShaderSource("Geodesic.Geometry")]
    [FragmentShaderSource("Geodesic.Fragment")]
    public class GeodesicProgram
        : Program
    {
        [VertexAttrib(3, VertexAttribPointerType.Float)]
        public VertexAttrib Position { get; protected set; }

        public Uniform<Matrix4> ModelMatrix { get; protected set; }
        public Uniform<Matrix4> ViewMatrix { get; protected set; }
        public Uniform<Matrix4> ProjectionMatrix { get; protected set; }
        public Uniform<Matrix4> ModelViewMatrix { get; protected set; }
        public Uniform<Matrix4> ModelViewProjectionMatrix { get; protected set; }
        public Uniform<Matrix3> NormalMatrix { get; protected set; }

        public Uniform<Vector3> LightPosition { get; protected set; }
        public Uniform<Vector3> DiffuseMaterial { get; protected set; }
        public Uniform<Vector3> AmbientMaterial { get; protected set; }
        
        public Uniform<Vector3> CameraPosition { get; protected set; }

        public Uniform<float> ClipNear { get; protected set; }
        public Uniform<float> ClipFar { get; protected set; }

        public Uniform<float> TessellationScale { get; protected set; }
    }
}
