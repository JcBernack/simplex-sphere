using OpenTK.Input;

namespace Sphere.Variables
{
    public class ToggleVariableAttribute
        : VariableBaseAttribute
    {
        public Key Key { get; set; }
        public float OnValue { get; set; }
        public float OffValue { get; set; }

        public ToggleVariableAttribute()
        {
            Mode = ScaleMode.KeyPress;
            OnValue = 1;
            OffValue = 0;
        }

        public override float Handle(float value, float factor, KeyboardState state)
        {
            if (!state.IsKeyDown(Key)) return value;
            return value == OnValue ? OffValue : OnValue;
        }
    }
}