using Mirror;
using UnityEngine;

namespace Server.ClientSide
{
    public class ClientObjectCreator : NetworkBehaviour
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Transform _spawn_point;
        [SerializeField] private bool _is_spawned_on_start = true;

        public GameObject Object { get; private set; }

        public override void OnStartClient()
        {
            if (_is_spawned_on_start)
                CreateObject();
        }

        public T GetObject<T>() => CreateObject().GetComponent<T>(); 

        private GameObject CreateObject()
        {
            if (Object != null)
                return Object;

            Transform spawn_point = _spawn_point;
            if (spawn_point == null)
                spawn_point = transform;

            Object = Instantiate(_prefab, spawn_point.position, spawn_point.rotation, spawn_point);
            return Object;
        }
    }
}