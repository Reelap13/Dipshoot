using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NeoFPS.SinglePlayer;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/hudref-mb-hudhealthmeter.html")]
	public class HudHealthMeter : PlayerCharacterHudBase
    {
        [SerializeField, Tooltip("The rect transform of the filled health bar.")]
        private RectTransform m_BarRect = null;

        private IHealthManager m_HealthManager = null;

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unsubscribe from old character
            if (m_HealthManager != null)
                m_HealthManager.onHealthChanged -= OnHealthChanged;
        }

        public override void OnPlayerCharacterChanged(ICharacter character)
        {
			if (m_HealthManager != null)
                m_HealthManager.onHealthChanged -= OnHealthChanged;

            if (character as Component != null)
                m_HealthManager = character.GetComponent<IHealthManager>();
            else
                m_HealthManager = null;

            if (m_HealthManager != null)
			{
                m_HealthManager.onHealthChanged += OnHealthChanged;
				OnHealthChanged (0f, m_HealthManager.health, false, null);
				gameObject.SetActive (true);
			}
			else
                gameObject.SetActive (false);
		}

		protected virtual void OnHealthChanged (float from, float to, bool critical, IDamageSource source)
        {
            m_BarRect.localScale = new Vector2(to / m_HealthManager.healthMax, 1f);
		}
    }
}