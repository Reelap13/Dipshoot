using UnityEngine;

namespace Settings
{
    public class IntValueSetting : Setting<int>
    {
        [SerializeField] private Vector2Int _limits = new Vector2Int(0, 1);

        public override void UpdateValue(int value)
        {
            if (_limits.x <= value && value <= _limits.y)
                base.UpdateValue(value);
            else Debug.Log($"Updated setting '{Type.ToString()}' error: value '{value}' out of range");
        }
    }
}