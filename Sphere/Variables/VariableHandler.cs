using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenTK.Input;

namespace Sphere.Variables
{
    public class VariableHandler
    {
        private readonly object _instance;
        private readonly Dictionary<VariableAttribute, FieldInfo> _variables;

        public VariableHandler(object instance)
        {
            _instance = instance;
            _variables = new Dictionary<VariableAttribute, FieldInfo>();
            foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = field.GetCustomAttributes<VariableAttribute>().FirstOrDefault();
                if (attribute == null || field.FieldType != typeof(float)) continue;
                _variables.Add(attribute, field);
            }
        }

        public void Update(float factor)
        {
            var state = Keyboard.GetState();
            foreach (var pair in _variables)
            {
                var step = 0;
                if (state.IsKeyDown(pair.Key.IncKey)) step++;
                if (state.IsKeyDown(pair.Key.DecKey)) step--;
                if (step == 0) continue;
                // get current value
                var value = (float) pair.Value.GetValue(_instance);
                switch (pair.Key.Scaling)
                {
                    case VariableScaling.Linear:
                        value += pair.Key.Parameter * step * factor;
                        break;
                    case VariableScaling.Exponential:
                        value *= 1 + pair.Key.Parameter * step * factor;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                pair.Value.SetValue(_instance, value);
            }
        }
    }
}