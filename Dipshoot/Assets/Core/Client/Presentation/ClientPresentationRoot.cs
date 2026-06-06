using System;
using System.Collections.Generic;
using Scripts.UI.SceneUI;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientPresentationRoot : MonoBehaviour
    {
        private readonly List<ClientUiLayer> _layers = new();
        private ClientPresentationState _state = ClientPresentationState.MainMenu;

        public event Action<ClientPresentationState> OnStateUpdated;

        public ClientPresentationState State => _state;

        public void RegisterLayer(ClientUiLayer layer)
        {
            if (layer == null || _layers.Contains(layer))
                return;

            _layers.Add(layer);
            ApplyLayer(layer);
        }

        public void UnregisterLayer(ClientUiLayer layer)
        {
            if (layer == null)
                return;

            _layers.Remove(layer);
        }

        public void SetState(ClientPresentationState state)
        {
            if (_state == state)
            {
                ApplyState();
                return;
            }

            _state = state;
            ApplyState();
            OnStateUpdated?.Invoke(_state);
        }

        private void ApplyState()
        {
            ClientAppRoot.Instance.EnsureEventSystem();
            ApplyInputMode();
            ApplyCameraContext();
            ApplySceneUI();

            foreach (ClientUiLayer layer in _layers)
                ApplyLayer(layer);
        }

        private void ApplyLayer(ClientUiLayer layer)
        {
            if (layer == null)
                return;

            layer.SetVisible(IsLayerVisible(layer.Kind));
        }

        private bool IsLayerVisible(ClientUiLayerKind kind)
        {
            return kind switch
            {
                ClientUiLayerKind.MainMenu => _state == ClientPresentationState.MainMenu ||
                                              _state == ClientPresentationState.MatchMenu,
                ClientUiLayerKind.Lobby => _state == ClientPresentationState.Lobby,
                ClientUiLayerKind.MatchHud => _state == ClientPresentationState.Match,
                ClientUiLayerKind.MatchEnd => _state == ClientPresentationState.MatchEnded,
                ClientUiLayerKind.Loading => _state == ClientPresentationState.MatchLoading,
                ClientUiLayerKind.MatchPause => _state == ClientPresentationState.MatchPause,
                _ => false,
            };
        }

        private void ApplyInputMode()
        {
            ClientInputMode mode = _state switch
            {
                ClientPresentationState.Match => ClientInputMode.Gameplay,
                ClientPresentationState.MatchMenu => ClientInputMode.Overlay,
                ClientPresentationState.MatchEnded => ClientInputMode.Overlay,
                ClientPresentationState.MatchPause => ClientInputMode.Overlay,
                _ => ClientInputMode.Menu,
            };

            ClientAppRoot.Instance.InputRouter.SetMode(mode);
        }

        private void ApplyCameraContext()
        {
            ClientCameraContext context = _state == ClientPresentationState.Match
                ? ClientCameraContext.Match
                : ClientCameraContext.Menu;

            ClientAppRoot.Instance.CameraRouter.SetContext(context);
        }

        private void ApplySceneUI()
        {
            SceneUI scene_ui = SceneUI.Instance;
            if (scene_ui == null)
                return;

            if (scene_ui.ESC != null)
                scene_ui.ESC.SetDefault();

            if (scene_ui.Cursor == null)
                return;

            scene_ui.Cursor.SetDefault();
            if (_state == ClientPresentationState.Match)
                scene_ui.Cursor.Hide();
        }
    }
}
