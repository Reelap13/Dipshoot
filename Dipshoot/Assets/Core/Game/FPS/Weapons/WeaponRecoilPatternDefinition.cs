using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "WeaponRecoilPattern", menuName = "Game/Weapons/WeaponRecoilPattern")]
    public class WeaponRecoilPatternDefinition : ScriptableObject
    {
        [SerializeField] private Vector2[] _pattern;
        [SerializeField] private float _pattern_reset_time = 0.4f;
        [SerializeField] private float _gameplay_scale = 1f;
        [SerializeField] private float _visual_scale = 1f;
        [SerializeField] private float _random_yaw = 0.03f;
        [SerializeField] private float _random_pitch = 0.02f;

        public Vector2[] Pattern => _pattern;
        public float PatternResetTime => _pattern_reset_time;
        public float GameplayScale => _gameplay_scale;
        public float VisualScale => _visual_scale;
        public float RandomYaw => _random_yaw;
        public float RandomPitch => _random_pitch;
    }
}
