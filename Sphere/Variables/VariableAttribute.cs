using System;
using OpenTK.Input;

namespace Sphere.Variables
{
    public class VariableAttribute
        : VariableBaseAttribute
    {
        public ScaleFunction Function { get; set; }
        public Key IncKey { get; set; }
        public Key DecKey { get; set; }
        public float Speed { get; set; }
        public float Minimum { get; set; }
        public float Maximum { get; set; }

        public VariableAttribute()
        {
            Mode = ScaleMode.Continuous;
            Function = ScaleFunction.Linear;
            Speed = 1;
            Minimum = float.MinValue;
            Maximum = float.MaxValue;
        }

        public override float Handle(float value, float factor, KeyboardState state)
        {
            var step = 0;
            if (state.IsKeyDown(IncKey)) step++;
            if (state.IsKeyDown(DecKey)) step--;
            return ScaleFunc(value, step * factor);
        }

        private float ScaleFunc(float value, float factor)
        {
            // apply scale function
            switch (Function)
            {
                case ScaleFunction.Linear:
                    value += Speed * factor;
                    break;
                case ScaleFunction.Exponential:
                    value *= 1 + Speed * factor;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            // check bounds
            if (value < Minimum) value = Minimum;
            if (value > Maximum) value = Maximum;
            return value;
        }
    }
}