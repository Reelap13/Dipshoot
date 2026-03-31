using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Settings.UI
{
    public abstract class SettingsUIValueChanger : MonoBehaviour
    {
        [NonSerialized] public UnityEvent<object> OnValueUpdated = new();

        public abstract void Initialize(ISetting setting);
        public abstract void UpdateValue(object value);
    }
}