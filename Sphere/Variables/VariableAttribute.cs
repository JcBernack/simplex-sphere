using System;
using OpenTK.Input;

namespace Sphere.Variables
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class VariableAttribute
        : Attribute
    {
        public Key IncKey { get; set; }
        public Key DecKey { get; set; }
        public VariableScaling Scaling { get; set; }
        public float Parameter { get; set; }

        public VariableAttribute(Key incKey, Key decKey, VariableScaling scaling, float parameter)
        {
            IncKey = incKey;
            DecKey = decKey;
            Scaling = scaling;
            Parameter = parameter;
        }
    }
}