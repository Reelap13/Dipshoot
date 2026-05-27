using System.Collections.Generic;
using UnityEngine;

namespace Game.Players
{
    public static class WeaponVfxUtility
    {
        private static readonly HashSet<string> LoggedWarnings = new();

        public static void PlayParticles(GameObject root)
        {
            if (root == null)
                return;

            ParticleSystem[] particles = root.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                particles[i].gameObject.SetActive(true);
                particles[i].Clear(true);
                particles[i].Play(true);
            }
        }

        public static void LogWarningOnce(Object context, string key, string message)
        {
            if (string.IsNullOrWhiteSpace(key) || !LoggedWarnings.Add(key))
                return;

            Debug.LogWarning(message, context);
        }
    }
}
