using UnityEngine;

namespace Settings
{
    public class FloatValueSetting : Setting<float>
    {
        [field: SerializeField]
        public Vector2 Limits { get; private set; } = new Vector2(0f, 1f);

        public override void UpdateValue(float value)
        {
            if (Limits.x <= value && value <= Limits.y)
                base.UpdateValue(value);
            else Debug.Log($"Updated setting '{Type.ToString()}' error: value '{value}' out of range");
        }
    }
}