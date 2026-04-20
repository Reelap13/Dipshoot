using Mirror;
using UnityEngine;

namespace Game.Players
{
    public class CameraSetter : NetworkBehaviour
    {
        [SerializeField] private Transform _camera_point;

        public override void OnStartAuthority()
        {
            Transform camera = Camera.main.transform;
            camera.SetParent(_camera_point);
            camera.position = _camera_point.position;
            camera.rotation = _camera_point.rotation;
        }
    }
}