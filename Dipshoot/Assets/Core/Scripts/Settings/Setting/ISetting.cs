using System;
using UnityEngine;

namespace Settings
{
    public interface ISetting
    {
        public int Id { get; }
        public void Save();
        public void Load();
        public void ResetToDefault();
        public SettingType Type { get; }
        public void SetValue(object value); 
        public object GetValue();
        public Type GetValueType();
    }
}