using UnityEngine;
using NeoSaveGames.Serialization;

namespace NeoSaveGames
{
    [HelpURL("https://docs.neofps.com/manual/savegamesref-mb-additivescenesaveinfo.html")]
    public class AdditiveSceneSaveInfo : NeoSerializedScene
    {
        [SerializeField, Tooltip("The name to use for the additive scene when displaying it to the user.")]
        private string m_DisplayName = "Unnamed Scene";
        [SerializeField, Tooltip("Should this scene be pushed as the current local scene. Prefabs will be instantiated in this scene instead of the main scene depending on the \"Move To Local Scene\" setting in their NeoSerializedGameObject")]
        private bool m_PushLocalScene = true;

        public string displayName
        {
            get { return m_DisplayName; }
        }

        public override bool isMainScene
        {
            get { return false; }
        }

        protected override void Awake_()
        {
            base.Awake_();

            if (m_PushLocalScene)
                SaveGameManager.PushLocalScene(this);
        }

        protected override void OnDestroy_()
        {
            base.OnDestroy_();

            if (m_PushLocalScene)
                SaveGameManager.PopLocalScene(this);
        }
    }
}

