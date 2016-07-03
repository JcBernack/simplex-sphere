using System;
using ObjectTK.Tools.Shapes;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace Sphere.Shapes
{
    public class Icosahedron
        : IndexedShape
    {
        public Icosahedron()
        {
            DefaultMode = PrimitiveType.Triangles;
            
            var t = (float) ((1.0 + Math.Sqrt(5.0))/2.0);
            Vertices = new[]
            {
                new Vector3(-1, t, 0),
                new Vector3( 1, t, 0),
                new Vector3(-1,-t, 0),
                new Vector3( 1,-t, 0),

                new Vector3( 0,-1, t),
                new Vector3( 0, 1, t),
                new Vector3( 0,-1,-t),
                new Vector3( 0, 1,-t),

                new Vector3( t, 0,-1),
                new Vector3( t, 0, 1),
                new Vector3(-t, 0,-1),
                new Vector3(-t, 0, 1)
            };
            
            // normalize vector to unit length
            for (var i = 0; i < Vertices.Length; i++) Vertices[i].Normalize();

            Indices = new uint[]
            {
                // 5 faces around point 0
                0, 5, 11,
                0, 1, 5,
                0, 7, 1,
                0, 10, 7,
                0, 11, 10,

                // 5 adjacent faces
                1, 9, 5,
                5, 4, 11,
                11, 2, 10,
                10, 6, 7,
                7, 8, 1,

                // 5 faces around point 3
                3, 4, 9,
                3, 2, 4,
                3, 6, 2,
                3, 8, 6,
                3, 9, 8,

                // 5 adjacent faces
                4, 5, 9,
                2, 11, 4,
                6, 10, 2,
                8, 7, 6,
                9, 1, 8,
            };
        }

        public Icosahedron(int tessLevel)
            : this()
        {
            var tessellator = new Tessellator(_ => _.Normalized());
            for (var i = 0; i < tessLevel; i++)
            {
                tessellator.Tessellate(this);
                Vertices = tessellator.Vertices;
                Indices = tessellator.Indices;
            }
        }
    }
}