using System;
using OpenTK.Input;

namespace Sphere.Variables
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public abstract class VariableBaseAttribute
        : Attribute
    {
        public ScaleMode Mode { get; set; }

        public abstract float Handle(float value, float factor, KeyboardState state);
    }
}