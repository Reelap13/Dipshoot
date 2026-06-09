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
            ClearGeneratedLevelRoots();

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

            GameObject root = new(ClientRootName);

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
                SceneManager.MoveGameObjectToScene(root, activeScene);

            try
            {
                WarehouseLevelGenerator.GenerateInto(root.transform, recipe, seed, "GeneratedWarehouse", true);
                HideSpawnMarkerRenderers(root.transform);
            }
            catch (System.Exception exception)
            {
                Debug.LogError($"Failed to generate client warehouse preset '{preset.Id}': {exception}");
            }

            return true;
        }

        public static void ClearGeneratedLevelRoots()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                GameObject[] roots = scene.GetRootGameObjects();
                for (int j = 0; j < roots.Length; j++)
                {
                    GameObject root = roots[j];
                    if (root == null || root.name != ClientRootName)
                        continue;

                    Object.Destroy(root);
                }
            }
        }

        private static void HideSpawnMarkerRenderers(Transform root)
        {
            if (root == null)
                return;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null ||
                    child.name != "SpawnA" &&
                    child.name != "SpawnB")
                {
                    continue;
                }

                Renderer[] renderers = child.GetComponentsInChildren<Renderer>(true);
                for (int j = 0; j < renderers.Length; j++)
                    if (renderers[j] != null)
                        renderers[j].enabled = false;
            }
        }
    }
}
