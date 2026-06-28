using System.Collections.Generic;
using Game.MatchMode;
using UnityEngine;

namespace Core.ClientPresentation
{
    public class ClientKillFeedController : MonoBehaviour
    {
        [SerializeField] private Transform _entries_container;
        [SerializeField] private ClientKillFeedEntryView _entry_prefab;
        [SerializeField] private Sprite _primary_weapon_sprite;
        [SerializeField] private Sprite _secondary_weapon_sprite;
        [SerializeField] private Color _blue_color = new(0.32f, 0.62f, 1f, 1f);
        [SerializeField] private Color _red_color = new(1f, 0.34f, 0.3f, 1f);
        [SerializeField] private Color _neutral_color = new(0.82f, 0.85f, 0.9f, 1f);
        [SerializeField] private float _entry_height = 38f;
        [SerializeField] private float _entry_spacing = 6f;
        [SerializeField] private float _position_lerp_speed = 12f;
        [SerializeField] private int _max_visible_entries = 8;

        private readonly List<ClientKillFeedEntryView> _active_entries = new();
        private readonly Stack<ClientKillFeedEntryView> _pooled_entries = new();
        private ClientUiLayer _layer;
        private MatchHudController _hud_controller;

        private void Awake()
        {
            _layer = GetComponent<ClientUiLayer>();
            if (_layer == null)
            {
                Debug.LogError($"{nameof(ClientKillFeedController)} requires {nameof(ClientUiLayer)}.", this);
                enabled = false;
                return;
            }

            _layer.Initialize(ClientUiLayerKind.MatchHud);
        }

        private void Update()
        {
            BindHudController(ClientAppRoot.Instance.MatchStore.HudController);

            float now = Time.unscaledTime;
            for (int i = _active_entries.Count - 1; i >= 0; i--)
            {
                if (_active_entries[i].Tick(
                        now,
                        Time.unscaledDeltaTime,
                        _position_lerp_speed))
                {
                    continue;
                }

                ReleaseEntryAt(i);
            }

            UpdateTargetPositions();
        }

        private void OnDisable()
        {
            BindHudController(null);
            ClearEntries();
        }

        private void BindHudController(MatchHudController hud_controller)
        {
            if (_hud_controller == hud_controller)
                return;

            if (_hud_controller != null)
            {
                _hud_controller.OnKillFeedEvent -= HandleKillFeedEvent;
                _hud_controller.OnKillFeedCleared -= ClearEntries;
            }

            _hud_controller = hud_controller;
            if (_hud_controller == null)
                return;

            _hud_controller.OnKillFeedEvent += HandleKillFeedEvent;
            _hud_controller.OnKillFeedCleared += ClearEntries;
        }

        private void HandleKillFeedEvent(MatchKillEvent kill_event)
        {
            if (_entry_prefab == null || _entries_container == null)
                return;

            int max_entries = Mathf.Max(1, _max_visible_entries);
            while (_active_entries.Count >= max_entries)
                ReleaseEntryAt(0);

            ClientKillFeedEntryView entry = GetEntry();
            float step = Mathf.Max(1f, _entry_height + _entry_spacing);
            Vector2 initial_position = new(0f, -_active_entries.Count * step);
            entry.SetImmediatePosition(initial_position);
            entry.Play(
                kill_event,
                GetWeaponSprite(kill_event.Weapon),
                GetTeamColor(kill_event.Killer.TeamId),
                GetTeamColor(kill_event.Victim.TeamId),
                Time.unscaledTime);
            _active_entries.Add(entry);
            UpdateTargetPositions();
        }

        private ClientKillFeedEntryView GetEntry()
        {
            if (_pooled_entries.Count > 0)
                return _pooled_entries.Pop();

            return Instantiate(_entry_prefab, _entries_container, false);
        }

        private void ReleaseEntryAt(int index)
        {
            if (index < 0 || index >= _active_entries.Count)
                return;

            ClientKillFeedEntryView entry = _active_entries[index];
            _active_entries.RemoveAt(index);
            entry.Release();
            _pooled_entries.Push(entry);
        }

        private void ClearEntries()
        {
            for (int i = _active_entries.Count - 1; i >= 0; i--)
                ReleaseEntryAt(i);
        }

        private void UpdateTargetPositions()
        {
            float step = Mathf.Max(1f, _entry_height + _entry_spacing);
            for (int i = 0; i < _active_entries.Count; i++)
                _active_entries[i].SetTargetPosition(new Vector2(0f, -i * step));
        }

        private Sprite GetWeaponSprite(MatchKillWeapon weapon)
        {
            return weapon == MatchKillWeapon.Secondary
                ? _secondary_weapon_sprite
                : _primary_weapon_sprite;
        }

        private Color GetTeamColor(TeamId team)
        {
            return team switch
            {
                TeamId.Blue => _blue_color,
                TeamId.Red => _red_color,
                _ => _neutral_color,
            };
        }
    }
}
