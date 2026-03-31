using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace Server.ServerSide
{
    public class ServerObjectCreator : NetworkBehaviour
    {
        [SerializeField] private GameObject _object_prefab;
        [SerializeField] private bool _spawn_on_start = true;
        [SerializeField] private bool _is_set_parant = true;    

        private GameObject _object;

        public override void OnStartServer()
        {
            if (_spawn_on_start)
                CreateObject();
        }

        public T GetObject<T>() => CreateObject().GetComponent<T>();    

        public GameObject CreateObject()
        {
            if (_object != null)
                return _object;

            _object = NetworkUtils.NetworkInstantiate(_object_prefab, transform);
            if (_is_set_parant)
                _object.transform.SetParent(transform, true);
            return _object;
        }

        public GameObject Object { get { return _object; } }    
    }
}