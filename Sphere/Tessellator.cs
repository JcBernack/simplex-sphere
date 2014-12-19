using System;
using System.Collections.Generic;
using ObjectTK.Shapes;
using OpenTK;

namespace Sphere
{
    public class Tessellator
    {
        public Vector3[] Vertices { get { return _vertices.ToArray(); } }
        public uint[] Indices { get { return _indices.ToArray(); } }

        public Func<Vector3, Vector3> EvaluationHandler;

        private Dictionary<Vector3, uint> _vertexIndices;
        private List<Vector3> _vertices;
        private List<uint> _indices;

        public Tessellator(Func<Vector3, Vector3> evaluationHandler)
        {
            EvaluationHandler = evaluationHandler;
        }

        private void Reset()
        {
            _vertices = new List<Vector3>();
            _indices = new List<uint>();
            _vertexIndices = new Dictionary<Vector3, uint>();
        }

        /// <summary>
        /// Subdivides each triangle face in the shape to four triangles.
        /// </summary>
        /// <param name="shape">The shape to tesselate.</param>
        public void Tessellate(IndexedShape shape)
        {
            Reset();
            Tessellate(shape.Vertices, shape.Indices);
        }

        /// <summary>
        /// Subdivides each triangle face to four triangles.
        /// </summary>
        /// <param name="vertices">The face vertices.</param>
        /// <param name="indices">The face indices.</param>
        private void Tessellate(Vector3[] vertices, uint[] indices)
        {
            // iterate over faces
            for (var i = 0; i < indices.Length; i += 3)
            {
                // get current face
                var v1 = vertices[indices[i + 0]];
                var v2 = vertices[indices[i + 1]];
                var v3 = vertices[indices[i + 2]];
                // subdivice edges
                var v12 = (v1 + v2) * 0.5f;
                var v23 = (v2 + v3) * 0.5f;
                var v31 = (v3 + v1) * 0.5f;
                // add four new faces
                AddFace(v1, v12, v31);
                AddFace(v12, v2, v23);
                AddFace(v31, v12, v23);
                AddFace(v31, v23, v3);
            }
            // call evaluation handler for each vertex
            if (EvaluationHandler == null) return;
            for (var i = 0; i < _vertices.Count; i++)
            {
                _vertices[i] = EvaluationHandler(_vertices[i]);
            }
        }

        /// <summary>
        /// Adds a triangle face by adding the vertices and their indices.
        /// </summary>
        /// <param name="v1">Specifies the first vertex of the triangle.</param>
        /// <param name="v2">Specifies the second vertex of the triangle.</param>
        /// <param name="v3">Specifies the third vertex of the triangle.</param>
        private void AddFace(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            _indices.Add(AddVertex(v1));
            _indices.Add(AddVertex(v2));
            _indices.Add(AddVertex(v3));
        }

        /// <summary>
        /// Adds the given vertex and returns its index. If the vertex already exists returns the index of that vertex instead.
        /// </summary>
        /// <param name="vertex">The vertex to add.</param>
        /// <returns>The index to the vertex.</returns>
        private uint AddVertex(Vector3 vertex)
        {
            uint index;
            if (_vertexIndices.TryGetValue(vertex, out index)) return index;
            index = (uint)_vertices.Count;
            _vertices.Add(vertex);
            _vertexIndices.Add(vertex, index);
            return index;
        }
    }
}