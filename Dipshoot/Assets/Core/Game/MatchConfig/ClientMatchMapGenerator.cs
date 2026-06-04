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
                Debug.LogError($"Match preset not found: {presetId}");
                return false;
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

            WarehouseLevelGenerator.GenerateInto(root.transform, recipe, seed, "GeneratedWarehouse", true);
            return true;
        }
    }
}
