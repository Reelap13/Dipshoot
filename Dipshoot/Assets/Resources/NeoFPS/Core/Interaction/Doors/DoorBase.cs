using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeoSaveGames.Serialization;
using NeoSaveGames;
using UnityEngine.Events;

namespace NeoFPS
{
    public abstract class DoorBase : MonoBehaviour, INeoSerializableComponent
    {
        [SerializeField, Tooltip("An optional occlusion portal component for the doorway. This will be synced to the door opening and closing so that it occludes objects when closed.")]
        private OcclusionPortal m_OcclusionPortal = null;

        private static readonly NeoSerializationKey k_NormalisedOpenKey = new NeoSerializationKey("normalisedOpen");
        private static readonly NeoSerializationKey k_StateKey = new NeoSerializationKey("state");
        private static readonly NeoSerializationKey k_LockedKey = new NeoSerializationKey("locked");

        public event UnityAction onIsLockedChanged;

        public virtual bool reversible
        {
            get { return false; }
        }

        public abstract float normalisedOpen
        {
            get;
            protected set;
        }

        public DoorState state
        {
            get;
            protected set;
        }

        public bool isLocked
        {
            get;
            private set;
        }

        public OcclusionPortal occlusionPortal
        {
            get { return m_OcclusionPortal; }
        }

        public void Open(bool reverse = false)
        {
            if (state == DoorState.Closed)
            {
                if (isLocked)
                    OnTryOpenLocked();
                else
                    OnOpen(reverse);
            }
            else
                OnOpen(reverse);
        }

        public void Close()
        {
            if (state != DoorState.Closed)
                OnClose();
        }

        protected abstract void OnOpen(bool reverse);
        protected abstract void OnClose();
        protected abstract void OnTryOpenLocked();

        public void Lock()
        {
            if (!isLocked)
            {
                isLocked = true;
                OnLockedStateChangedInternal();
            }
        }

        public void LockSilent()
        {
            if (!isLocked)
                isLocked = true;
        }

        public void Unlock()
        {
            if (isLocked)
            {
                isLocked = false;
                OnLockedStateChangedInternal();
            }
        }

        public void UnlockSilent()
        {
            if (isLocked)
                isLocked = false;
        }

        void OnLockedStateChangedInternal ()
        {
            OnLockedStateChanged(isLocked);
            onIsLockedChanged?.Invoke();
        }

        protected abstract void OnLockedStateChanged(bool locked);

        public virtual bool IsTransformInFrontOfDoor(Transform t)
        {
            return true;
        }

        public virtual void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_NormalisedOpenKey, normalisedOpen);
            writer.WriteValue(k_StateKey, (int)state);
            writer.WriteValue(k_LockedKey, isLocked);
        }

        public virtual void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            if (reader.TryReadValue(k_NormalisedOpenKey, out var floatResult, normalisedOpen))
                normalisedOpen = floatResult;

            if (reader.TryReadValue(k_StateKey, out var intResult, (int)state))
                state = (DoorState)intResult;

            if (reader.TryReadValue(k_LockedKey, out var locked, isLocked))
                isLocked = locked;

            // Update the occlusion portal
            if (occlusionPortal != null)
                occlusionPortal.open = state != DoorState.Closed;
        }
    }
}