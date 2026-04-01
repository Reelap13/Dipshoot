using NeoCC;
using NeoFPS;
using NeoFPS.SinglePlayer;
using UnityEngine;

namespace NeoFPS
{
	public class AimTargetTransform : MonoBehaviour
	{
		[SerializeField, Range(0f, 90f), Tooltip("")]
		private float m_MaxAngle = 0f;
		[SerializeField, Tooltip("")]
		private bool m_Continuous = true;

		private IAimController m_AimController = null;
		private Transform m_CameraTransform = null;

		private void Start()
		{
			enabled = false;
		}

		void UpdateConstraints()
		{
			var dir = (transform.position - m_CameraTransform.position).normalized;
			m_AimController.SetYawConstraints(dir, 2f * m_MaxAngle);
			float pitch = Mathf.Rad2Deg * Mathf.Asin(dir.y);
			m_AimController.SetPitchConstraints(Mathf.Max(-89f, pitch - m_MaxAngle), Mathf.Min(89f, pitch + m_MaxAngle));
		}

		private void Update()
		{
			if (m_AimController != null && m_Continuous)
				UpdateConstraints();
		}

		private void OnDestroy()
		{
			if (m_AimController != null)
				ResetTarget();
		}

		public void SetTarget()
		{
			var character = FpsSoloCharacter.localPlayerCharacter;
			if (character != null && character.TryGetComponent(out m_AimController))
			{
				if (character.fpCamera.transform != null)
					m_CameraTransform = character.fpCamera.transform;
				else
					m_CameraTransform = character.transform;

				if (!m_Continuous)
					UpdateConstraints();

				enabled = true;
			}
		}
		 
		public void ResetTarget()
		{
			if (m_AimController != null)
			{
				m_AimController.ResetPitchConstraints();
				m_AimController.ResetYawConstraints();
				m_AimController = null;
			}

			enabled = false;
		}
	}
}