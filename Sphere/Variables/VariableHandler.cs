using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ObjectTK;
using OpenTK;
using OpenTK.Input;

namespace Sphere.Variables
{
    public class VariableHandler
    {
        private readonly object _instance;
        private readonly Dictionary<VariableBaseAttribute, FieldInfo> _variables;

        public VariableHandler(object instance)
        {
            _instance = instance;
            _variables = new Dictionary<VariableBaseAttribute, FieldInfo>();
            foreach (var field in instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var attribute = field.GetCustomAttributes<VariableBaseAttribute>(false).FirstOrDefault();
                if (attribute == null) continue;
                _variables.Add(attribute, field);
            }
        }

        public void Enable(GameWindow window)
        {
            window.UpdateFrame += Update;
            window.KeyDown += KeyDown;
        }

        public void Disable(GameWindow window)
        {
            window.UpdateFrame -= Update;
            window.KeyDown -= KeyDown;
        }

        private void Update(object sender, FrameEventArgs frameEventArgs)
        {
            Handle(ScaleMode.Continuous, Keyboard.GetState(), (float)frameEventArgs.Time);
        }

        private void KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            Handle(ScaleMode.KeyPress, e.Keyboard, 1);
        }

        private void Handle(ScaleMode mode, KeyboardState state, float factor)
        {
            foreach (var pair in _variables)
            {
                if (pair.Key.Mode != mode) continue;
                // get current value
                var value = (float)Convert.ChangeType(pair.Value.GetValue(_instance), typeof(float));
                // handle input to get new value
                value = pair.Key.Handle(value, factor, state);
                // convert and set new value
                pair.Value.SetValue(_instance, Convert.ChangeType(value, pair.Value.FieldType));
            }
        }
    }
}