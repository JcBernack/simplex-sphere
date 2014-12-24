using ObjectTK.Cameras;
using OpenTK;
using OpenTK.Input;

namespace Sphere
{
    public class LookAtCamera
        : CameraBase
    {
        /// <summary>
        /// Specifies the mouse speed when rotating.
        /// </summary>
        public float MouseSpeed = 0.0025f;

        /// <summary>
        /// Specifies the speed when moving.
        /// </summary>
        public float MoveSpeed = 6f;

        public Vector3 LookAt;

        public Vector3 AlignmentPoint;
        public Vector3 Up { get { return Position - AlignmentPoint; } }

        public LookAtCamera()
        {
            LookAt = new Vector3(0, 0, -1);
            if (Vector3.Dot(Up, LookAt) < 0.0001f) LookAt = new Vector3(0, 0.6f, 0.8f);
        }

        public override void ApplyCamera(ref Matrix4 matrix)
        {
            matrix = Matrix4.LookAt(Position, Position + LookAt, Up) * matrix;
        }

        public override Vector3 GetEyePosition()
        {
            return Position;
        }

        public override void Enable(GameWindow window)
        {
            window.Mouse.Move += MouseMove;
            window.UpdateFrame += UpdateFrame;
        }

        public override void Disable(GameWindow window)
        {
            window.Mouse.Move -= MouseMove;
            window.UpdateFrame -= UpdateFrame;
        }

        private void MouseMove(object sender, MouseMoveEventArgs e)
        {
            var dx = e.XDelta;
            var dy = e.YDelta;
            var state = Mouse.GetState();
            if (state.IsButtonDown(MouseButton.Left))
            {
                LookAt = Vector3.Transform(LookAt, Matrix3.CreateFromAxisAngle(Up, -dx*MouseSpeed));
                LookAt = Vector3.Transform(LookAt, Matrix3.CreateFromAxisAngle(Vector3.Cross(Up, LookAt), dy * MouseSpeed));
            }
            //TODO: prevent large pitch angles > 90°
            // renormalize LookAt vector to prevent summing up of floating point errors
            LookAt.Normalize();
        }

        protected Vector3 GetStep(float timeStep)
        {
            var state = Keyboard.GetState();
            var step = Vector3.Zero;
            var leftRight = Vector3.Cross(Up, LookAt).Normalized();
            if (state.IsKeyDown(Key.W)) step += LookAt;
            if (state.IsKeyDown(Key.S)) step -= LookAt;
            if (state.IsKeyDown(Key.A)) step += leftRight;
            if (state.IsKeyDown(Key.D)) step -= leftRight;
            if (state.IsKeyDown(Key.Space)) step += Up.Normalized();
            if (state.IsKeyDown(Key.LControl)) step -= Up.Normalized();
            return step * MoveSpeed * timeStep;
        }

        protected virtual void UpdateFrame(object sender, FrameEventArgs e)
        {
            Position += GetStep((float)e.Time);
        }
    }
}