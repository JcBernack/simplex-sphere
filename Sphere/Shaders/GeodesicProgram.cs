﻿using DerpGL.Shaders;
using DerpGL.Shaders.Variables;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shaders
{
    [VertexShaderSource("Geodesic.Vertex")]
    [TessControlShaderSource("Geodesic.TessControl")]
    [TessEvaluationShaderSource("Geodesic.TessEval.Equal")]
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

        public Uniform<float> Radius { get; protected set; }
        public Uniform<float> EdgesPerScreenHeight { get; protected set; }
        public Uniform<float> TerrainScale { get; protected set; }
        public Uniform<float> HeightScale { get; protected set; }
    }

    [TessEvaluationShaderSource("Geodesic.TessEval.Odd")]
    public class GeodesicProgramOdd : GeodesicProgram { }

    [TessEvaluationShaderSource("Geodesic.TessEval.Even")]
    public class GeodesicProgramEven : GeodesicProgram { }
}
