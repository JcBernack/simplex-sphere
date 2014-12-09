using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    [VertexShaderSource("Geodesic.Vertex")]
    [TessControlShaderSource("Geodesic.TessControl")]
    [FragmentShaderSource("Geodesic.Fragment")]
    public class GeodesicProgram
        : TransformProgram
    {
        protected GeodesicProgram()
        {
            FeedbackVaryings(TransformFeedbackMode.InterleavedAttribs, FeedbackPosition);
        }

        [VertexAttrib(3, VertexAttribPointerType.Float)]
        public VertexAttrib Position { get; protected set; }

        public TransformOut FeedbackPosition { get; protected set; }

        public Uniform<Matrix4> ModelMatrix { get; protected set; }
        public Uniform<Matrix4> ViewMatrix { get; protected set; }
        public Uniform<Matrix4> ProjectionMatrix { get; protected set; }
        public Uniform<Matrix4> ModelViewMatrix { get; protected set; }
        public Uniform<Matrix4> ModelViewProjectionMatrix { get; protected set; }
        public Uniform<Matrix3> NormalMatrix { get; protected set; }

        public Uniform<Vector3> LightPosition { get; protected set; }
        public Uniform<Vector3> DiffuseMaterial { get; protected set; }
        public Uniform<Vector3> AmbientMaterial { get; protected set; }

        public Uniform<float> Radius { get; protected set; }
        public Uniform<float> EdgesPerScreenHeight { get; protected set; }
        public Uniform<bool> RoundTessellationLevel { get; protected set; }
        public Uniform<float> TerrainScale { get; protected set; }
        public Uniform<float> HeightScale { get; protected set; }
    }

    [TessEvaluationShaderSource("Geodesic.TessEval.Odd")]
    public class GeodesicProgramOdd : GeodesicProgram { }

    [TessEvaluationShaderSource("Geodesic.TessEval.Even")]
    public class GeodesicProgramEven : GeodesicProgram { }

    [TessEvaluationShaderSource("Geodesic.TessEval.Equal")]
    public class GeodesicProgramEqual : GeodesicProgram { }
}
