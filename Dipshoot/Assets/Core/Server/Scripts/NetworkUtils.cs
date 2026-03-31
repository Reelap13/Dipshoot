using Mirror;
using UnityEngine;

public static class NetworkUtils
{
    public static T NetworkInstantiate<T>(T prefab, Transform transform, Transform parant = null) where T : MonoBehaviour =>
        NetworkInstantiate(prefab, transform.position, transform.rotation, parant);
    public static GameObject NetworkInstantiate(GameObject prefab, Transform transform, Transform parant = null) =>
        NetworkInstantiate(prefab, transform.position, transform.rotation, parant);

    public static T NetworkInstantiate<T>(T prefab, Vector3 position = default(Vector3),
        Quaternion rotation = default(Quaternion), Transform parant = null) where T : MonoBehaviour =>
        NetworkInstantiate(prefab.gameObject, position, rotation, parant).GetComponent<T>();

    public static GameObject NetworkInstantiate(GameObject prefab, Vector3 position = default(Vector3), 
        Quaternion rotation = default(Quaternion), Transform parant = null)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Try to call NetworkInstantiate without active network server!");
            return null;
        }

        GameObject obj = null;
        if (parant != null)
            obj = Object.Instantiate(prefab, position, rotation, parant);
        else obj = Object.Instantiate(prefab, position, rotation);

        if (!obj.TryGetComponent(out NetworkIdentity identity))
        {
            Debug.LogError("Try to call NetworkIdentity with prefab without NetworkIdentity component! Object wasn't synchronize!");
            return obj;
        }

        NetworkServer.Spawn(obj);
        return obj;
    }
}
