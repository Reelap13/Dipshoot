using NeoSaveGames;
using NeoSaveGames.Serialization;
using UnityEngine;

namespace NeoFPS
{
    [HelpURL("https://docs.neofps.com/manual/graphicsref-mb-holographicsightsrp.html")]
    public class HolographicSightSrp : MonoBehaviour, IOpticsBrightnessControl, INeoSerializableComponent
    {
        [SerializeField, Tooltip("The renderer with the holographic sight or red dot sight material (from the shader graph) attached.")]
        private Renderer m_Renderer = null;
        [SerializeField, Tooltip("The index of the material with the holographic sight or red dot sight shader.")]
        private int m_MaterialIndex = 0;

        [Header ("Reticule")]

        [SerializeField, Tooltip("The base colour of the reticule")]
        private Color m_ReticuleColor = Color.red;
        [SerializeField, Tooltip("A proxy transform that specifies where the reticule will appear in the object's space.")]
        private Transform m_ReticulePosition = null;
        [SerializeField, Tooltip("An forwards offset for the reticule to project it in front of the sight.")]
        private float m_ReticuleDistance = 250f;
        [SerializeField, Tooltip("The size of the reticule.")]
        private float m_ReticuleSize = 10f;

        [Header ("Brightness")]

        [SerializeField, Tooltip("A series of brightness values that can be cycled through with the \"Optics Brightness +/-\" inputs")]
        private float[] m_BrightnessSettings = new float[] { 0.6f, 0.7f, 0.775f, 0.85f, 0.925f, 1f };
        [SerializeField, Tooltip("The index of the starting brightness setting from the above array")]
        private int m_BrightnessSetting = 4;

        // Constants
        const string k_ShaderParameterReticulePosition = "Vector3_ReticulePosition";
        const string k_ShaderParameterReticuleSize = "Vector1_ReticuleSize";
        const string k_ShaderParameterBrightness = "Vector1_ReticuleBrightness";
        const string k_ShaderParameterColour = "Color_ReticuleColor";

        private static readonly NeoSerializationKey k_ColourKey = new NeoSerializationKey("colour");
        private static readonly NeoSerializationKey k_BrightnessKey = new NeoSerializationKey("brightness");

        private Material m_OpticsMaterial = null;
        private int m_PropIDColour = 0;
        private int m_PropIDBrightness = 0;
        private int m_PropIDReticuleSize = 0;
        private int m_PropIDReticuleOffset = 0;

        public float brightness
        {
            get { return m_OpticsMaterial.GetFloat(m_PropIDBrightness); }
            set { m_OpticsMaterial.SetFloat(m_PropIDBrightness, Mathf.Clamp01(value)); }
        }

        public Color reticuleColor
        {
            get { return m_ReticuleColor; }
            set
            {
                m_ReticuleColor = value;
                m_OpticsMaterial.SetColor(m_PropIDColour, m_ReticuleColor);
            }
        }

        public void SetBrightness(int index)
        {
            m_BrightnessSetting = Mathf.Clamp(index, 0, m_BrightnessSettings.Length - 1);
            brightness = m_BrightnessSettings[m_BrightnessSetting];
        }

        public void IncrementBrightness(bool looping = false)
        {
            if (looping)
            {
                ++m_BrightnessSetting;
                if (m_BrightnessSetting >= m_BrightnessSettings.Length)
                    m_BrightnessSetting = 0;
                brightness = m_BrightnessSettings[m_BrightnessSetting];
            }
            else
            {
                if (m_BrightnessSetting < m_BrightnessSettings.Length - 1)
                {
                    ++m_BrightnessSetting;
                    brightness = m_BrightnessSettings[m_BrightnessSetting];
                }
            }
        }

        public void DecrementBrightness(bool looping = false)
        {
            if (looping)
            {
                --m_BrightnessSetting;
                if (m_BrightnessSetting < 0)
                    m_BrightnessSetting = m_BrightnessSettings.Length - 1;
                brightness = m_BrightnessSettings[m_BrightnessSetting];
            }
            else
            {
                if (m_BrightnessSetting > 0)
                {
                    --m_BrightnessSetting;
                    brightness = m_BrightnessSettings[m_BrightnessSetting];
                }
            }
        }

        protected void OnValidate()
        {
            // Make sure there's always at least 1 brightness setting
            if (m_BrightnessSettings.Length == 0)
                m_BrightnessSettings = new float[] { 1f };

            // Make sure brightness settings are ascending within 0-1 range
            for (int i = 0; i < m_BrightnessSettings.Length; ++i)
            {
                // Clamp lower limit
                if (m_BrightnessSettings[i] > 1f)
                    m_BrightnessSettings[i] = 1f;

                if (i == 0)
                {
                    // Clamp to 0
                    if (m_BrightnessSettings[i] < 0f)
                        m_BrightnessSettings[i] = 0f;
                }
                else
                {
                    // Clamp to previous value
                    if (m_BrightnessSettings[i] < m_BrightnessSettings[i - 1])
                        m_BrightnessSettings[i] = m_BrightnessSettings[i - 1];
                }
            }

            m_BrightnessSetting = Mathf.Clamp(m_BrightnessSetting, 0, m_BrightnessSettings.Length - 1);
        }

        protected void Awake()
        {
            if (m_Renderer != null)
            {
                m_OpticsMaterial = m_Renderer.materials[m_MaterialIndex];
                m_PropIDReticuleSize = Shader.PropertyToID(k_ShaderParameterReticuleSize);
                m_PropIDReticuleOffset = Shader.PropertyToID(k_ShaderParameterReticulePosition);
                m_PropIDBrightness = Shader.PropertyToID(k_ShaderParameterBrightness);
                m_PropIDColour = Shader.PropertyToID(k_ShaderParameterColour);
            }
            else
                Debug.LogError("Holosight does not have a renderer attached: " + name);
        }

        protected void Start()
        {
            reticuleColor = m_ReticuleColor;
            SetBrightness(m_BrightnessSetting);
            UpdateReticulePosition();
        }

        void UpdateReticulePosition()
        {
            if (m_ReticulePosition != null)
                m_OpticsMaterial.SetVector(m_PropIDReticuleOffset, m_Renderer.transform.InverseTransformPoint(m_ReticulePosition.position) + new Vector3(0f, 0f, m_ReticuleDistance));
            else
                m_OpticsMaterial.SetVector(m_PropIDReticuleOffset, new Vector3(0f, 0f, m_ReticuleDistance));

            m_OpticsMaterial.SetFloat(m_PropIDReticuleSize, m_ReticuleSize);

            Debug.Log("Setting reticule distance");
        }

#if UNITY_EDITOR

        float m_LastSize = 0f;
        float m_LastDistance = 0f;

        void Update()
        {
            if (m_ReticuleSize != m_LastSize || m_ReticuleDistance != m_LastDistance)
            {
                m_LastSize = m_ReticuleSize;
                m_LastDistance = m_ReticuleDistance;
                UpdateReticulePosition();
            }
        }

#endif

        public void WriteProperties(INeoSerializer writer, NeoSerializedGameObject nsgo, SaveMode saveMode)
        {
            writer.WriteValue(k_ColourKey, m_ReticuleColor);
            writer.WriteValue(k_BrightnessKey, m_BrightnessSetting);
        }

        public void ReadProperties(INeoDeserializer reader, NeoSerializedGameObject nsgo)
        {
            reader.TryReadValue(k_ColourKey, out m_ReticuleColor, m_ReticuleColor);
            reader.TryReadValue(k_BrightnessKey, out m_BrightnessSetting, m_BrightnessSetting);
        }
    }
}
