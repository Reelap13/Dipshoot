using System.Collections.Generic;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public static class NeoSerializedObjectRegistry
    {
        private static Dictionary<int, RegisteredPrefab> s_RegisteredPrefabs = new Dictionary<int, RegisteredPrefab>();
        private static Dictionary<int, RegisteredAsset> s_RegisteredAssets = new Dictionary<int, RegisteredAsset>();
        static HashSet<int> s_PrefabErrorIDs = new HashSet<int>();
        static HashSet<int> s_AssetErrorIDs = new HashSet<int>();

        private class RegisteredPrefab
        {
            public int references;
            public NeoSerializedGameObject prefab;

            public void Increment()
            {
                ++references;
            }

            public bool Decrement()
            {
                --references;
                return references > 0;
            }

            public RegisteredPrefab(NeoSerializedGameObject p)
            {
                prefab = p;
                references = 1;
            }
        }

        private class RegisteredAsset
        {
            public int references;
            public INeoSerializableAsset asset;

            public void Increment()
            {
                ++references;
            }

            public bool Decrement()
            {
                --references;
                return references > 0;
            }

            public RegisteredAsset(INeoSerializableAsset a)
            {
                asset = a;
                references = 1;
            }
        }

        public static NeoSerializedGameObject GetPrefab(int prefabID)
        {
            RegisteredPrefab result;
            if (s_RegisteredPrefabs.TryGetValue(prefabID, out result))
                return result.prefab;
            else
            {
                if (!s_PrefabErrorIDs.Contains(prefabID))
                {
                    Debug.LogWarning("Attempting to retrieve prefab with ID that hasn't been registered. ID: " + prefabID);
                    s_PrefabErrorIDs.Add(prefabID);
                }
                return null;
            }
        }

        public static bool IsPrefabRegistered(int prefabID)
        {
            return s_RegisteredPrefabs.ContainsKey(prefabID);
        }

        public static INeoSerializableAsset GetAsset(int assetID)
        {
            RegisteredAsset result;
            if (s_RegisteredAssets.TryGetValue(assetID, out result))
                return result.asset;
            else
            {
                if (!s_AssetErrorIDs.Contains(assetID))
                {
                    Debug.LogWarning("Attempting to retrieve asset with ID that hasn't been registered. ID: " + assetID);
                    s_AssetErrorIDs.Add(assetID);
                }
                return null;
            }
        }

        public static bool IsAssetRegistered(int assetID)
        {
            return s_RegisteredAssets.ContainsKey(assetID);
        }

        #region REGISTRATION

        public static void RegisterPrefab(NeoSerializedGameObject prefab)
        {
            if (prefab != null)
            {
                RegisteredPrefab registeredPrefab;
                if (s_RegisteredPrefabs.TryGetValue(prefab.prefabStrongID, out registeredPrefab))
                {
                    // Check the objects match
                    if (registeredPrefab.prefab != prefab)
                        Debug.LogError("Prefab registration error (duplicate IDs). Please run the project checker at Tools/NeoFPS/Kerplah");

                    registeredPrefab.Increment();
                }
                else
                    s_RegisteredPrefabs.Add(prefab.prefabStrongID, new RegisteredPrefab(prefab));
            }
        }

        public static void RegisterPrefabs(NeoSerializedGameObject[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; ++i)
                RegisterPrefab(prefabs[i]);
        }

        public static void UnregisterPrefab(NeoSerializedGameObject prefab)
        {
            if (prefab != null)
            {
                RegisteredPrefab registeredPrefab;
                if (s_RegisteredPrefabs.TryGetValue(prefab.prefabStrongID, out registeredPrefab))
                {
                    if (registeredPrefab.references == 1)
                        s_RegisteredPrefabs.Remove(prefab.prefabStrongID);
                    else
                        registeredPrefab.Decrement();
                }
            }
        }

        public static void UnregisterPrefabs(NeoSerializedGameObject[] prefabs)
        {
            for (int i = 0; i < prefabs.Length; ++i)
                UnregisterPrefab(prefabs[i]);
        }

        public static void RegisterAsset(INeoSerializableAsset asset)
        {
            if (asset != null)
            {
                RegisteredAsset registeredAsset;
                if (s_RegisteredAssets.TryGetValue(asset.GetInstanceID(), out registeredAsset))
                    registeredAsset.Increment();
                else
                    s_RegisteredAssets.Add(asset.GetInstanceID(), new RegisteredAsset(asset));
            }
        }

        public static void RegisterAssets(INeoSerializableAsset[] assets)
        {
            for (int i = 0; i < assets.Length; ++i)
                RegisterAsset(assets[i]);
        }

        public static void UnregisterAsset(INeoSerializableAsset asset)
        {
            if (asset != null)
            {
                RegisteredAsset registeredAsset;
                if (s_RegisteredAssets.TryGetValue(asset.GetInstanceID(), out registeredAsset))
                {
                    if (registeredAsset.references == 1)
                        s_RegisteredAssets.Remove(asset.GetInstanceID());
                    else
                        registeredAsset.Decrement();
                }
            }
        }

        public static void UnregisterAssets(INeoSerializableAsset[] assets)
        {
            for (int i = 0; i < assets.Length; ++i)
                UnregisterAsset(assets[i]);
        }

        #endregion
    }
}