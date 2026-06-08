using Game.ProcGen.Warehouse;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.MatchConfig
{
    public static class ClientMatchMapGenerator
    {
        private const string ClientRootName = "ClientGeneratedLevel";

        public static bool GenerateSelectedPreset(string presetId, int seed, string resultUrl)
        {
            MatchPreset preset = MatchPresetRegistry.GetPreset(presetId);
            if (preset == null)
            {
                MatchPresetRegistry registry = MatchPresetRegistry.LoadDefault();
                preset = registry == null ? null : registry.GetDefault();
                if (preset == null)
                {
                    Debug.LogError($"Match preset not found: {presetId}. No local fallback preset.");
                    ClientMatchPresetState.Set(presetId, seed, resultUrl, false);
                    return true;
                }

                Debug.LogWarning($"Match preset not found: {presetId}. Fallback to {preset.Id}.");
            }

            WarehouseRecipe recipe = preset.Recipe;
            ClientMatchPresetState.Set(preset.Id, seed, resultUrl, recipe != null);
            if (recipe == null)
                return true;

            GameObject root = GameObject.Find(ClientRootName);
            if (root == null)
                root = new GameObject(ClientRootName);

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
                SceneManager.MoveGameObjectToScene(root, activeScene);

            try
            {
                WarehouseLevelGenerator.GenerateInto(root.transform, recipe, seed, "GeneratedWarehouse", true);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to generate client warehouse preset '{preset.Id}': {exception}");
            }

            return true;
        }
    }
}
