using System.Collections.Generic;
using UnityEngine;

namespace Settings
{
    public class IntListSetting : Setting<int>
    {
        [SerializeField] private List<int> _values;

        public override void UpdateValue(int value)
        {
            if (_values.Contains(value))
                base.UpdateValue(value);
            else Debug.Log($"Updated setting '{Type.ToString()}' error: value '{value}' out of range");
        }
    }
}