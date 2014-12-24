namespace Sphere.Shapes
{
    public static class TessellationHelper
    {
        /// <summary>
        /// Calculate number of triangles produced by barycentric subdivision, i.e. tessellation.<br/>
        /// The inner and all outer tessellation levels are assumed to be equal.
        /// </summary>
        /// <remarks>
        /// Source: http://stackoverflow.com/a/15506444/804614
        /// </remarks>
        /// <param name="tessLevel">Tessellation level.</param>
        /// <returns>The number of triangles</returns>
        public static int GetTriangleFaces(int tessLevel)
        {
            if (tessLevel < 0) return 1;
            if (tessLevel == 0) return 0;
            return ((2 * tessLevel - 2) * 3) + GetTriangleFaces(tessLevel - 2);
        }
    }
}