using UnityEngine;

namespace NeoFPS
{
	public interface IAdditiveTransform : IMonoBehaviour
	{
		void UpdateTransform ();

        bool bypassPositionMultiplier { get; }
        bool bypassRotationMultiplier { get; }

        Quaternion rotation { get; }
		Vector3 position { get; }
	}
}
