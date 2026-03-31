using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NetworkUtils
{
    public static T NetworkInstantiate<T>(T prefab, Transform transform, Transform parent = null) where T : MonoBehaviour =>
        NetworkInstantiate(prefab, transform.position, transform.rotation, parent);
    public static GameObject NetworkInstantiate(GameObject prefab, Transform transform, Transform parant = null) =>
        NetworkInstantiate(prefab, transform.position, transform.rotation, parant);

    public static T NetworkInstantiate<T>(T prefab, Vector3 position = default(Vector3),
        Quaternion rotation = default(Quaternion), Transform parent = null) where T : MonoBehaviour =>
        NetworkInstantiate(prefab.gameObject, position, rotation, parent).GetComponent<T>();

    public static GameObject NetworkInstantiate(GameObject prefab, Vector3 position = default(Vector3), 
        Quaternion rotation = default(Quaternion), Transform parent = null)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Try to call NetworkInstantiate without active network server!");
            return null;
        }

        GameObject obj = null;
        if (parent != null)
            obj = Object.Instantiate(prefab, position, rotation, parent);
        else obj = Object.Instantiate(prefab, position, rotation);

        if (!obj.TryGetComponent(out NetworkIdentity identity))
        {
            Debug.LogError("Try to call NetworkIdentity with prefab without NetworkIdentity component! Object wasn't synchronize!");
            return obj;
        }

        NetworkServer.Spawn(obj);
        return obj;
    }

    public static T NetworkMatchInstantiate<T>(T prefab, Scene scene, System.Guid match_id, 
        Transform transform, Transform parent = null) where T : MonoBehaviour =>
        NetworkMatchInstantiate(prefab, scene, match_id, transform.position, transform.rotation, parent);
    public static T NetworkMatchInstantiate<T>(T prefab, Scene scene, System.Guid match_id, 
        Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), Transform parent = null) where T : MonoBehaviour
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("Try to call NetworkInstantiate without active network server!");
            return null;
        }

        T obj = Object.Instantiate(prefab, position, rotation);
        SceneManager.MoveGameObjectToScene(obj.gameObject, scene);

        if (!obj.TryGetComponent(out NetworkIdentity identity))
        {
            Debug.LogError("Try to call NetworkIdentity with prefab without NetworkIdentity component! Object wasn't synchronize!");
            return obj;
        }

        if (!obj.TryGetComponent(out NetworkMatch match))
        {
            Debug.LogError("Try to call NetworkMatch with prefab without NetworkMatch component! Object wasn't synchronize!");
            return obj;
        }

        match.matchId = match_id;
        
        if (parent != null)
            obj.transform.SetParent(parent);

        NetworkServer.Spawn(obj.gameObject);
        return obj;
    }
}
