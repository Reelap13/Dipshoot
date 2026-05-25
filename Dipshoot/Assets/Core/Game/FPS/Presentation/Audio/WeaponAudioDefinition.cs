using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "WeaponAudioDefinition", menuName = "Game/Audio/WeaponAudioDefinition")]
    public class WeaponAudioDefinition : ScriptableObject
    {
        [SerializeField] private AudioCue _fire;
        [SerializeField] private AudioCue _reload;

        public AudioCue Fire => _fire;
        public AudioCue Reload => _reload;
    }
}
