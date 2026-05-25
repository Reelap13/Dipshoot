using UnityEngine;

namespace Game.Players
{
    [CreateAssetMenu(fileName = "FootstepAudioSet", menuName = "Game/Audio/FootstepAudioSet")]
    public class FootstepAudioSet : ScriptableObject
    {
        [SerializeField] private AudioCue _crouch_steps;
        [SerializeField] private AudioCue _walk_steps;
        [SerializeField] private AudioCue _run_steps;
        [SerializeField] private AudioCue _jump;
        [SerializeField] private AudioCue _land_soft;
        [SerializeField] private AudioCue _land_hard;

        public AudioCue CrouchSteps => _crouch_steps;
        public AudioCue WalkSteps => _walk_steps;
        public AudioCue RunSteps => _run_steps;
        public AudioCue Jump => _jump;
        public AudioCue LandSoft => _land_soft;
        public AudioCue LandHard => _land_hard;
    }
}
