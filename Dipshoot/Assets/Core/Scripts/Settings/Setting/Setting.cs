using SavingSystem;
using System;
using UnityEngine;
using UnityEngine.Events;

namespace Settings
{
    public abstract class Setting<T> : MonoBehaviour, ISetting
    {
        private static string FOLDER = "Settings";

        [NonSerialized] public UnityEvent<T> OnUpdatedValue = new();

        [SerializeField] private SettingType _type;
        [SerializeField] private T _default_value;

        private T _value;

        public void Save()
        {
            FileManager.SaveFile(FOLDER, Id.ToString(), _value);
        }

        public void Load()
        {
            if (FileManager.IsFileExists(FOLDER, Id.ToString()))
                UpdateValue(FileManager.LoadFile<T>(FOLDER, Id.ToString()));
            else UpdateValue(_default_value);
        }

        public virtual void UpdateValue(T value)
        {
            _value = value;
            Save();
            OnUpdatedValue.Invoke(value);
        }

        public void ResetToDefault()
        {
            UpdateValue(_default_value);
        }

        public void SetValue(object value) => UpdateValue((T)value);
        public object GetValue() => _value;

        public Type GetValueType() => typeof(T);

        public T Value { get { return _value; } }

        public int Id => (int)_type;

        public SettingType Type => _type;
    }
}