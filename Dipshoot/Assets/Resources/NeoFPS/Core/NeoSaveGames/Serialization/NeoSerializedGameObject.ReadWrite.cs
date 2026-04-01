//#define NEOFPS_SAVE_LOGGING

using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        [SerializeField, Tooltip("Save and reload the object's name. This is only required if the name would change at runtime")]
        private bool m_SaveName = false;

        // Object transform

        [Header("Transform")]
        [SerializeField, Tooltip("If and how to serialize the object position")]
        private TransformSerialization m_Position = TransformSerialization.LocalSpace;
        [SerializeField, Tooltip("If and how to serialize the object rotation")]
        private TransformSerialization m_Rotation = TransformSerialization.LocalSpace;
        [SerializeField, Tooltip("Should the object local scale be serialized")]
        private bool m_LocalScale = false;

        // Child object filtering

        [SerializeField, Tooltip("How to filter out child objects. If set to exclude, the objects in the list below will not be serialized. If set to include, only the objects below will be serialized")]
        private NeoSerializationFilter m_FilterChildObjects = NeoSerializationFilter.Exclude;
        [SerializeField]
        private NeoSerializedGameObject[] m_ChildObjects = new NeoSerializedGameObject[0];

        // Neo component filtering

        [SerializeField, Tooltip("How to filter out serialized components. If set to components, the objects in the list below will not be serialized. If set to include, only the components below will be serialized")]
        private NeoSerializationFilter m_FilterNeoComponents = NeoSerializationFilter.Exclude;
        [SerializeField]
        private MonoBehaviour[] m_NeoComponents = new MonoBehaviour[0];

        // Non-neo component formatters

        [SerializeField]
        private Component[] m_OtherComponents = new Component[0];

        // Save-mode overrides

        [SerializeField]
        private Override[] m_Overrides = new Override[0];

        // Prefab

        [SerializeField, HideInInspector]
        private int m_PrefabStrongID = 0;
        [SerializeField, HideInInspector]
        private bool m_IsPrefabRoot = false;

        private static List<INeoSerializableComponent> s_GatheredNeoComponents = new List<INeoSerializableComponent>(16);

        private static readonly NeoSerializationKey k_NameKey = new NeoSerializationKey("name");
        private static readonly NeoSerializationKey k_ActiveKey = new NeoSerializationKey("active");
        private static readonly NeoSerializationKey k_EnabledKey = new NeoSerializationKey("enabled");
        private static readonly NeoSerializationKey k_PositionKey = new NeoSerializationKey("position");
        private static readonly NeoSerializationKey k_RotationKey = new NeoSerializationKey("rotation");
        private static readonly NeoSerializationKey k_ScaleKey = new NeoSerializationKey("scale");
        private static readonly NeoSerializationKey k_SettingsKey = new NeoSerializationKey("settings");

        private bool m_SaveSettings = false;

        public enum TransformSerialization
        {
            LocalSpace,
            WorldSpace,
            Ignore
        }

        public int prefabStrongID
        {
            get { return m_PrefabStrongID; }
        }

        public bool isPrefabRoot
        {
            get { return m_IsPrefabRoot; }
        }

        public bool saveName
        {
            get { return m_SaveName; }
            set
            {
                m_SaveName = value;
                m_SaveSettings = true;
            }
        }

        public NeoSerializationFilter filterChildObjects
        {
            get { return m_FilterChildObjects; }
            set
            {
                m_FilterChildObjects = value;
                m_SaveSettings = true;
            }
        }

        public NeoSerializationFilter filterNeoComponents
        {
            get { return m_FilterNeoComponents; }
            set
            {
                m_FilterNeoComponents = value;
                m_SaveSettings = true;
            }
        }

        bool WillSerializeChildObject(NeoSerializedGameObject child)
        {
            bool filtered = Array.IndexOf(m_ChildObjects, child) != -1;
            if (m_FilterChildObjects == NeoSerializationFilter.Include)
                return filtered;
            else
                return !filtered;
        }

        #region WRITING

        public void WriteGameObject(INeoSerializer writer, SaveMode saveMode)
        {
            if (!initialised)
                Initialise();

            // Write name
            if (m_SaveName)
                writer.WriteValue(k_NameKey, name);

            // Write active state
            writer.WriteValue(k_ActiveKey, gameObject.activeSelf);

            // Save settings (doesn't apply to overrides)
            if (m_SaveSettings)
                writer.WriteValue(k_SettingsKey, new Vector3Int(m_SaveName ? 1 : 0, (int)m_FilterChildObjects, (int)m_FilterNeoComponents));

            // Check for overrides
            Override over = null;
            if (saveMode != SaveMode.Default)
            {
                for (int i = 0; i < m_Overrides.Length; ++i)
                {
                    if (m_Overrides[i].saveMode == saveMode)
                    {
                        over = m_Overrides[i];
                        break;
                    }
                }
            }

            // Get settings
            TransformSerialization serializePosition = m_Position;
            TransformSerialization serializeRotation = m_Rotation;
            bool serializeScale = m_LocalScale;
            var filterChildren = m_FilterChildObjects;
            var childObjects = m_ChildObjects;
            var filterNeoComponents = m_FilterNeoComponents;
            var neoComponents = m_NeoComponents;
            var otherComponents = m_OtherComponents;
            if (over != null)
            {
                switch (over.serializePosition)
                {
                    case Override.OverrideTransformSerialization.LocalSpace:
                        serializePosition = TransformSerialization.LocalSpace;
                        break;
                    case Override.OverrideTransformSerialization.WorldSpace:
                        serializePosition = TransformSerialization.WorldSpace;
                        break;
                    case Override.OverrideTransformSerialization.Ignore:
                        serializePosition = TransformSerialization.Ignore;
                        break;
                }
                switch (over.serializeRotation)
                {
                    case Override.OverrideTransformSerialization.LocalSpace:
                        serializeRotation = TransformSerialization.LocalSpace;
                        break;
                    case Override.OverrideTransformSerialization.WorldSpace:
                        serializeRotation = TransformSerialization.WorldSpace;
                        break;
                    case Override.OverrideTransformSerialization.Ignore:
                        serializeRotation = TransformSerialization.Ignore;
                        break;
                }
                switch (over.serializeLocalScale)
                {
                    case Override.OverrideScaleSerialization.True:
                        serializeScale = true;
                        break;
                    case Override.OverrideScaleSerialization.False:
                        serializeScale = false;
                        break;
                }
                switch (over.filterChildObjects)
                {
                    case Override.OverrideNeoSerializationFilter.Exclude:
                        filterChildren = NeoSerializationFilter.Exclude;
                        childObjects = over.childObjects;
                        break;
                    case Override.OverrideNeoSerializationFilter.Include:
                        filterChildren = NeoSerializationFilter.Include;
                        childObjects = over.childObjects;
                        break;
                }
                switch (over.filterNeoComponents)
                {
                    case Override.OverrideNeoSerializationFilter.Exclude:
                        filterNeoComponents = NeoSerializationFilter.Exclude;
                        neoComponents = over.neoComponents;
                        break;
                    case Override.OverrideNeoSerializationFilter.Include:
                        filterNeoComponents = NeoSerializationFilter.Include;
                        neoComponents = over.neoComponents;
                        break;
                }
                if (over.overrideOtherComponents)
                    otherComponents = over.otherComponents;
            }

            // Write transform
            switch (serializePosition)
            {
                case TransformSerialization.LocalSpace:
                    writer.WriteValue(k_PositionKey, transform.localPosition);
                    break;
                case TransformSerialization.WorldSpace:
                    writer.WriteValue(k_PositionKey, transform.position);
                    break;
            }
            switch (serializeRotation)
            {
                case TransformSerialization.LocalSpace:
                    writer.WriteValue(k_RotationKey, transform.localRotation);
                    break;
                case TransformSerialization.WorldSpace:
                    writer.WriteValue(k_RotationKey, transform.rotation);
                    break;
            }
            if (serializeScale)
                writer.WriteValue(k_ScaleKey, transform.localScale);

            // Write neo-serialized components
            if (filterNeoComponents == NeoSerializationFilter.Include)
            {
                // Write components in m_NeoComponents array only
                for (int i = 0; i < neoComponents.Length; ++i)
                    WriteNeoComponent(writer, neoComponents[i], saveMode);
            }
            else
            {
                GetComponents(s_GatheredNeoComponents);
                for (int i = 0; i < s_GatheredNeoComponents.Count; ++i)
                {
                    // Skip components in m_NeoComponents
                    if (Array.IndexOf(neoComponents, s_GatheredNeoComponents[i]) != -1)
                        continue;

                    WriteNeoComponent(writer, s_GatheredNeoComponents[i], saveMode);
                }
                s_GatheredNeoComponents.Clear();
            }

            // Write other components (always manually included)
            for (int i = 0; i < otherComponents.Length; ++i)
                WriteOtherComponent(writer, otherComponents[i]);

            // Write child objects
            m_NestedObjects.WriteGameObjects(writer, filterChildren, childObjects, saveMode);
        }

        void WriteNeoComponent(INeoSerializer writer, MonoBehaviour component, SaveMode saveMode)
        {
            var c = component as INeoSerializableComponent;
            if (c != null)
            {
                writer.PushContext(SerializationContext.ComponentNeoSerialized, NeoSerializationUtilities.GetPersistentComponentID(component));
                writer.WriteValue(k_EnabledKey, component.enabled);
                c.WriteProperties(writer, this, saveMode);
                writer.PopContext(SerializationContext.ComponentNeoSerialized);
            }
        }

        void WriteNeoComponent(INeoSerializer writer, INeoSerializableComponent component, SaveMode saveMode)
        {
            var c = component as MonoBehaviour;
            if (c != null)
            {
                writer.PushContext(SerializationContext.ComponentNeoSerialized, NeoSerializationUtilities.GetPersistentComponentID(c));
                writer.WriteValue(k_EnabledKey, c.enabled);
                component.WriteProperties(writer, this, saveMode);
                writer.PopContext(SerializationContext.ComponentNeoSerialized);
            }
        }

        void WriteOtherComponent(INeoSerializer writer, Component component)
        {
            if (component != null)
            {
                writer.PushContext(SerializationContext.ComponentNeoFormatted, NeoSerializationUtilities.GetPersistentComponentID(component));
                var formatter = NeoSerializationFormatters.GetFormatter(component);
                if (formatter != null)
                    formatter.WriteProperties(writer, component, this);
                writer.PopContext(SerializationContext.ComponentNeoFormatted);
            }
        }

        #endregion

        #region READING

        public void ReadGameObjectHierarchy(INeoDeserializer reader)
        {
            // Rebuild the hierarchies separate to reading properties
            // Required to resolver references
            m_NestedObjects.ReadGameObjectHierarchy(reader);
        }

        public void ReadGameObjectProperties(INeoDeserializer reader)
        {
            string n;
            if (reader.TryReadValue(k_NameKey, out n, string.Empty))
            {
                name = n;
            }

            Vector3Int settings;
            if (reader.TryReadValue(k_SettingsKey, out settings, Vector3Int.zero))
            {
                m_SaveName = settings.x == 1;
                m_FilterChildObjects = (NeoSerializationFilter)settings.y;
                m_FilterNeoComponents = (NeoSerializationFilter)settings.z;
                m_SaveSettings = true;
            }

            // Read active state
            bool active;
            reader.TryReadValue(k_ActiveKey, out active, gameObject.activeSelf);
            gameObject.SetActive(active);

            // Read transform
            Vector3 position;
            if (reader.TryReadValue(k_PositionKey, out position, Vector3.zero))
            {
                switch (m_Position)
                {
                    case TransformSerialization.LocalSpace:
                        transform.localPosition = position;
                        break;
                    case TransformSerialization.WorldSpace:
                        transform.position = position;
                        break;
                }
            }
            Quaternion rotation;
            if (reader.TryReadValue(k_RotationKey, out rotation, Quaternion.identity))
            {
                switch (m_Rotation)
                {
                    case TransformSerialization.LocalSpace:
                        transform.localRotation = rotation;
                        break;
                    case TransformSerialization.WorldSpace:
                        transform.rotation = rotation;
                        break;
                }
            }
            if (m_LocalScale)
            {
                Vector3 scale;
                if (reader.TryReadValue(k_ScaleKey, out scale, Vector3.one))
                    transform.localScale = scale;
            }

            // Read neo-serialized components
            if (m_FilterNeoComponents == NeoSerializationFilter.Include)
            {
                // Write components in m_NeoComponents array only
                for (int i = 0; i < m_NeoComponents.Length; ++i)
                    ReadNeoComponent(reader, m_NeoComponents[i]);
            }
            else
            {
                GetComponents(s_GatheredNeoComponents);
                for (int i = 0; i < s_GatheredNeoComponents.Count; ++i)
                {
                    // Skip components in m_NeoComponents
                    if (Array.IndexOf(m_NeoComponents, s_GatheredNeoComponents[i]) != -1 ||
                        s_GatheredNeoComponents[i] == null)
                        continue;

                    ReadNeoComponent(reader, s_GatheredNeoComponents[i]);
                }
                s_GatheredNeoComponents.Clear();
            }

            // Read other components (always manually included)
            for (int i = 0; i < m_OtherComponents.Length; ++i)
                ReadOtherComponent(reader, m_OtherComponents[i]);

            // Read child objects
            m_NestedObjects.ReadGameObjectProperties(reader);
        }

        void ReadNeoComponent(INeoDeserializer reader, MonoBehaviour component)
        {
            var c = component as INeoSerializableComponent;
            if (c != null)
            {
                int id = NeoSerializationUtilities.GetPersistentComponentID(component);
                if (reader.PushContext(SerializationContext.ComponentNeoSerialized, id))
                {
                    try
                    {
                        // Get enabled state of the component
                        // Do this first incase reading properties causes coroutines to be started, etc
                        bool isEnabled = true;
                        if (reader.TryReadValue(k_EnabledKey, out isEnabled, true))
                            component.enabled = isEnabled;

                        c.ReadProperties(reader, this);
                    }
#if NEOFPS_SAVE_LOGGING
                    catch (Exception e)
                    {
                        Debug.LogError("Reading component failed due to error: " + e.Message, component.gameObject);
                    }
#endif
                    finally
                    {
                        reader.PopContext(SerializationContext.ComponentNeoSerialized, id);
                    }
                }
            }
        }

        void ReadNeoComponent(INeoDeserializer reader, INeoSerializableComponent component)
        {
            var c = component as MonoBehaviour;
            if (c != null)
            {
                int id = NeoSerializationUtilities.GetPersistentComponentID(c);
                if (reader.PushContext(SerializationContext.ComponentNeoSerialized, id))
                {
                    try
                    {
                        // Get enabled state of the component
                        // Do this first incase reading properties causes coroutines to be started, etc
                        bool isEnabled = true;
                        if (reader.TryReadValue(k_EnabledKey, out isEnabled, true))
                            c.enabled = isEnabled;

                        component.ReadProperties(reader, this);
                    }
#if NEOFPS_SAVE_LOGGING
                    catch (Exception e)
                    {
                        Debug.LogError(string.Format("Reading component ({0}) failed due to error: {1}", component.GetType(), e.Message), c.gameObject);
                    }
#endif
                    finally
                    {
                        reader.PopContext(SerializationContext.ComponentNeoSerialized, id);
                    }
                }
            }
        }

        void ReadOtherComponent(INeoDeserializer reader, Component component)
        {
            // Require formatters system before re-enabling this feature
            if (component != null)
            {
                int id = NeoSerializationUtilities.GetPersistentComponentID(component);
                if (reader.PushContext(SerializationContext.ComponentNeoFormatted, id))
                {
                    try
                    {
                        var formatter = NeoSerializationFormatters.GetFormatter(component);
                        formatter.ReadProperties(reader, component, this);
                    }
#if NEOFPS_SAVE_LOGGING
                    catch (Exception e)
                    {
                        Debug.LogError("Reading component failed due to error: " + e.Message, component.gameObject);
                    }
#endif
                    finally
                    {
                        reader.PopContext(SerializationContext.ComponentNeoFormatted, id);
                    }

                }
            }
        }

        #endregion
    }
}