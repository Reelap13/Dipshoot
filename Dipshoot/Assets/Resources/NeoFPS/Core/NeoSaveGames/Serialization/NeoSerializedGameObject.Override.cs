using System;
using UnityEngine;

namespace NeoSaveGames.Serialization
{
    public sealed partial class NeoSerializedGameObject : MonoBehaviour
    {
        [Serializable]
        public class Override
        {
            [SerializeField, Tooltip("The save mode this override applies to")]
            private int m_SaveMode = 1;

            [Header("Transform")]
            [SerializeField, Tooltip("If and how to serialize the object position")]
            private OverrideTransformSerialization m_Position = OverrideTransformSerialization.UseDefault;
            [SerializeField, Tooltip("If and how to serialize the object rotation")]
            private OverrideTransformSerialization m_Rotation = OverrideTransformSerialization.UseDefault;
            [SerializeField, Tooltip("Should the object local scale be serialized")]
            private OverrideScaleSerialization m_LocalScale = OverrideScaleSerialization.UseDefault;

            [SerializeField, Tooltip("How to filter out child objects. If set to exclude, the objects in the list below will not be serialized. If set to include, only the objects below will be serialized")]
            private OverrideNeoSerializationFilter m_FilterChildObjects = OverrideNeoSerializationFilter.UseDefault;
            [SerializeField]
            private NeoSerializedGameObject[] m_ChildObjects = new NeoSerializedGameObject[0];

            [SerializeField, Tooltip("How to filter out serialized components. If set to components, the objects in the list below will not be serialized. If set to include, only the components below will be serialized")]
            private OverrideNeoSerializationFilter m_FilterNeoComponents = OverrideNeoSerializationFilter.UseDefault;
            [SerializeField]
            private MonoBehaviour[] m_NeoComponents = new MonoBehaviour[0];

            [SerializeField, Tooltip("Override the other components list instead of default")]
            private bool m_OverrideOtherComponents = false;
            [SerializeField]
            private Component[] m_OtherComponents = new Component[0];

#if UNITY_EDITOR
            // Inspector foldout expansion tracking
            [HideInInspector] public bool expandOverride = true;
            [HideInInspector] public bool expandChildObjects = false;
            [HideInInspector] public bool expandNeoComponents = false;
            [HideInInspector] public bool expandOtherComponents = false;
#endif

            // Accessors
            public SaveMode saveMode { get { return m_SaveMode; } }
            public OverrideTransformSerialization serializePosition { get { return m_Position; } }
            public OverrideTransformSerialization serializeRotation { get { return m_Rotation; } }
            public OverrideScaleSerialization serializeLocalScale { get { return m_LocalScale; } }
            public OverrideNeoSerializationFilter filterChildObjects { get { return m_FilterChildObjects; } }
            public NeoSerializedGameObject[] childObjects { get { return m_ChildObjects; } }
            public OverrideNeoSerializationFilter filterNeoComponents { get { return m_FilterNeoComponents; } }
            public MonoBehaviour[] neoComponents { get { return m_NeoComponents; } }
            public bool overrideOtherComponents { get { return m_OverrideOtherComponents; } }
            public Component[] otherComponents { get { return m_OtherComponents; } }

            public enum OverrideTransformSerialization
            {
                UseDefault,
                LocalSpace,
                WorldSpace,
                Ignore
            }

            public enum OverrideScaleSerialization
            {
                UseDefault,
                True,
                False
            }

            public enum OverrideNeoSerializationFilter
            {
                UseDefault,
                Exclude,
                Include
            }

            public Override(SaveMode mode)
            {
                m_SaveMode = mode;
            }

            public void ApplyLimiter(INeoSerializedGameObjectLimiter limiter)
            {
                if (limiter.restrictChildObjects)
                {
                    m_FilterChildObjects = OverrideNeoSerializationFilter.UseDefault;
                    if (m_ChildObjects.Length > 0)
                        m_ChildObjects = new NeoSerializedGameObject[0];
                }

                if (limiter.restrictNeoComponents)
                {
                    m_FilterNeoComponents = OverrideNeoSerializationFilter.UseDefault;
                    if (m_NeoComponents.Length > 0)
                        m_NeoComponents = new MonoBehaviour[0];
                }

                if (limiter.restrictOtherComponents)
                {
                    m_OverrideOtherComponents = false;
                }
            }
        }
    }
}