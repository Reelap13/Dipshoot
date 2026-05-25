using UnityEngine;

namespace Game.Players
{
    public static class PlayerVisualLayerUtility
    {
        public static void SetLayerRecursive(GameObject target, string layer_name)
        {
            if (target == null)
                return;

            int layer = LayerMask.NameToLayer(layer_name);
            if (layer < 0)
                return;

            SetLayerRecursive(target.transform, layer);
        }

        public static void SetLayerRecursive(Transform target, int layer)
        {
            if (target == null || layer < 0)
                return;

            if (target.GetComponent<PlayerHitbox>() != null)
                return;

            target.gameObject.layer = layer;

            for (int i = 0; i < target.childCount; i++)
                SetLayerRecursive(target.GetChild(i), layer);
        }
    }
}
