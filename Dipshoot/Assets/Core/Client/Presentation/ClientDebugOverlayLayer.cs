using System.Globalization;
using System.Text;
using Game.Players;
using Mirror;
using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Core.ClientPresentation
{
    public class ClientDebugOverlayLayer : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _root_group;
        [SerializeField] private TMP_Text _content_text;
        [SerializeField] private float _update_interval = 0.1f;

        private readonly StringBuilder _builder = new(512);
        private PlayerCharacter _local_character;
        private float _next_update_time;
        private float _fps;
        private bool _is_visible;

        private void Awake()
        {
            CacheReferences();
            SetVisible(false);
        }

        private void Update()
        {
            if (WasTogglePressed())
                SetVisible(!_is_visible);

            UpdateFps();
            if (!_is_visible || Time.unscaledTime < _next_update_time)
                return;

            _next_update_time = Time.unscaledTime + Mathf.Max(0.02f, _update_interval);
            UpdateText();
        }

        private void CacheReferences()
        {
            if (_root_group == null)
                _root_group = GetComponent<CanvasGroup>();

            if (_content_text == null)
                _content_text = GetComponentInChildren<TMP_Text>(true);
        }

        private void SetVisible(bool is_visible)
        {
            _is_visible = is_visible;
            CacheReferences();

            if (_root_group != null)
            {
                _root_group.alpha = is_visible ? 1f : 0f;
                _root_group.interactable = false;
                _root_group.blocksRaycasts = false;
            }
        }

        private static bool WasTogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.f9Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F9);
#endif
        }

        private void UpdateFps()
        {
            if (Time.unscaledDeltaTime <= 0f)
                return;

            float frame_fps = 1f / Time.unscaledDeltaTime;
            _fps = _fps <= 0f ? frame_fps : Mathf.Lerp(_fps, frame_fps, 0.1f);
        }

        private void UpdateText()
        {
            if (_content_text == null)
                return;

            PlayerCharacter character = GetLocalCharacter();
            StateSynchronizer state_synchronizer = character == null
                ? null
                : character.GetComponent<StateSynchronizer>();
            WeaponController weapon_controller = character == null
                ? null
                : character.GetComponent<WeaponController>();

            _builder.Clear();
            _builder.AppendLine("DEBUG  F9");
            _builder.Append("FPS: ").Append(Format(_fps)).AppendLine();
            _builder.Append("Ping: ").Append(Format((float)(NetworkTime.rtt * 1000d))).Append(" ms").AppendLine();
            _builder.Append("Jitter: ").Append(Format((float)(NetworkTime.rttVariance * 1000d))).Append(" ms").AppendLine();

            if (character == null || character.TickManager == null)
            {
                _builder.AppendLine("Player: none");
                _content_text.text = _builder.ToString();
                return;
            }

            int client_tick = character.TickManager.CurrentTick;
            _builder.AppendLine();
            _builder.Append("NetId: ").Append(character.netId).AppendLine();
            _builder.Append("ClientTick: ").Append(client_tick).AppendLine();

            if (state_synchronizer != null)
            {
                double estimated_server_tick = state_synchronizer.EstimatedServerTick;
                _builder.Append("EstimatedServerTick: ").Append(Format((float)estimated_server_tick)).AppendLine();
                _builder.Append("Server-Client: ").Append(Format((float)(estimated_server_tick - client_tick))).AppendLine();
                _builder.Append("TickScale: ").Append(Format(character.TickManager.CurrentTickRateScale)).AppendLine();
                _builder.Append("OwnerServerTick: ").Append(state_synchronizer.LastReceivedStateTick).AppendLine();
                _builder.Append("OwnerInputTick: ").Append(state_synchronizer.LastProcessedInputTick).AppendLine();
                _builder.Append("ServerInputQueue: ")
                    .Append(state_synchronizer.LastServerInputQueueDepth)
                    .Append(" / ")
                    .Append(state_synchronizer.LastServerInputTargetDepth)
                    .AppendLine();
                _builder.Append("ServerInputSource: ")
                    .Append(state_synchronizer.LastServerInputSource)
                    .Append(" missing=")
                    .Append(state_synchronizer.LastServerMissingInputTicks)
                    .Append(" resets=")
                    .Append(state_synchronizer.LastServerBufferResetCount)
                    .Append(" jitter=")
                    .Append(Format(state_synchronizer.LastServerInputJitterTicks))
                    .AppendLine();
                _builder.Append("RemoteAppliedTick: ").Append(state_synchronizer.LastAppliedStateTick).AppendLine();
                _builder.Append("RemoteRenderTick: ")
                    .Append(Format(character.TickManager.RemoteRenderTick))
                    .AppendLine();
                _builder.Append("RemoteBuffer: ").Append(state_synchronizer.RemoteBufferCount).AppendLine();
                _builder.Append("RemoteBack: ")
                    .Append(Format(state_synchronizer.RemoteInterpolationBackMs))
                    .Append(" ms / ")
                    .Append(state_synchronizer.RemoteInterpolationBackTicks)
                    .AppendLine(" ticks");
            }

            if (weapon_controller == null)
            {
                _content_text.text = _builder.ToString();
                return;
            }

            _builder.Append("HitBias: ")
                .Append(Format(weapon_controller.LagCompensationHitRegBiasMs))
                .AppendLine(" ms");

            if (weapon_controller.HasLastConfirmedShot)
                AppendShot(weapon_controller.LastConfirmedShot);

            _content_text.text = _builder.ToString();
        }

        private void AppendShot(ShotResult shot)
        {
            _builder.AppendLine();
            _builder.AppendLine("LastShot:");
            _builder.Append("input/server: ").Append(shot.InputTick).Append(" / ").Append(shot.ServerTick).AppendLine();
            _builder.Append("view/validated/clamped: ")
                .Append(shot.ShotViewTick)
                .Append(" / ")
                .Append(shot.ValidatedShotViewTick)
                .Append(" / ")
                .Append(shot.ShotTimestampClamped)
                .AppendLine();
            _builder.Append("query/snapshot: ").Append(shot.HitboxQueryTick).Append(" / ").Append(shot.HitboxSnapshotTick).AppendLine();
            _builder.Append("visual/bias/input/server rewind: ")
                .Append(shot.LagCompensationVisualBackTicks)
                .Append(" / ")
                .Append(shot.LagCompensationBiasTicks)
                .Append(" / ")
                .Append(shot.LagCompensationRewindTicks)
                .Append(" / ")
                .Append(shot.ShotServerRewindTicks)
                .AppendLine();
            _builder.Append("hit/dmg/kill: ")
                .Append(shot.HasHit)
                .Append(" / ")
                .Append(shot.DidDamage)
                .Append(" / ")
                .Append(shot.DidKill)
                .AppendLine();
            _builder.Append("target: ").Append(shot.HitNetId).Append(" ").Append(shot.HitboxType).AppendLine();
        }

        private PlayerCharacter GetLocalCharacter()
        {
            if (_local_character != null && _local_character.isOwned)
                return _local_character;

            PlayerCharacter[] characters = FindObjectsByType<PlayerCharacter>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == null || !characters[i].isOwned)
                    continue;

                _local_character = characters[i];
                return _local_character;
            }

            _local_character = null;
            return null;
        }

        private static string Format(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
