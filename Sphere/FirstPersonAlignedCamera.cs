using ObjectTK.Cameras;
using OpenTK;

namespace Sphere
{
    /// <summary>
    /// First person camera with an additional alignment point which defines the "bottom" direction to simplify controls.
    /// </summary>
    public class FirstPersonAlignedCamera
        : FirstPersonCamera
    {
        /// <summary>
        /// Specifies an alignment point which is used to calculate the "bottom" direction and align the camera appropriately
        /// </summary>
        public Vector3 AlignmentPoint;

        /// <summary>
        /// Points from the AlignmentPoint to the camera Position.
        /// </summary>
        public Vector3 Up { get { return Position - AlignmentPoint; } }

        protected override void UpdateFrame(object sender, FrameEventArgs e)
        {
            var step = GetStep((float)e.Time);
            // rotate step so that (0,1,0) points in the direction of Up
            var alignmentRotation = DetermineRotation(Up.Normalized(), Vector3.UnitY);
            Vector3.Transform(ref step, ref alignmentRotation, out step);
            Position += step;
        }

        public override void ApplyCamera(ref Matrix4 matrix)
        {
            //var tangent = Vector3.Cross(Up, Vector3.UnitY);
            //var tangent1 = tangent.Normalized();
            //var tangent2 = Vector3.Cross(Up, tangent).Normalized();
            var alignmentRotation = new Matrix4(DetermineRotation(Vector3.UnitY, Up.Normalized()));
            matrix = Matrix4.CreateTranslation(-Position)
                * alignmentRotation
                * Matrix4.CreateRotationY(Yaw)
                * Matrix4.CreateRotationX(Pitch)
                * matrix;
        }

        /// <summary>
        /// </summary>
        /// <remarks>
        /// Source: http://math.stackexchange.com/a/476311/96955
        /// </remarks>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix3 DetermineRotation(Vector3 a, Vector3 b)
        {
            Vector3 v;
            Vector3.Cross(ref a, ref b, out v);
            float c;
            Vector3.Dot(ref a, ref b, out c);
            var vx = new Matrix3(0, -v[2], v[1],
                v[2], 0, -v[0],
                -v[1], v[0], 0);
            var mat = Matrix3.Identity;
            Matrix3.Add(ref mat, ref vx, out mat);
            Matrix3.Mult(ref vx, ref vx, out vx);
            Matrix3.Mult(ref vx, (1 - c) / v.LengthSquared, out vx);
            Matrix3.Add(ref mat, ref vx, out mat);
            return mat;
        }
    }
}