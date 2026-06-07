using Core.ClientPresentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentationEditor
{
    public static class ClientMatchHudV2PrefabFactory
    {
        private const string PrefabPath = "Assets/Core/Client/Presentation/Resources/ClientUI/ClientMatchHudV2Layer.prefab";

        [InitializeOnLoadMethod]
        private static void CreateMissingOnLoad()
        {
            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                    CreatePrefab(false);
            };
        }

        [MenuItem("Tools/Dipshoot/UI/Create Match HUD V2 Prefab")]
        public static void CreateFromMenu()
        {
            CreatePrefab(true);
        }

        private static void CreatePrefab(bool overwrite)
        {
            if (!overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            GameObject root = new("ClientMatchHudV2Layer", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ClientUiLayer), typeof(ClientMatchHudV2Layer));
            RectTransform root_rect = root.GetComponent<RectTransform>();
            root_rect.anchorMin = Vector2.zero;
            root_rect.anchorMax = Vector2.one;
            root_rect.sizeDelta = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;

            SetLayerKind(root.GetComponent<ClientUiLayer>(), ClientUiLayerKind.MatchHud);

            RectTransform top_panel = CreateRect("TopScorePanel", root.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(620f, 78f));
            Image blue_bg = CreateImage("BlueScoreBackground", top_panel, new Vector2(0f, 0.24f), new Vector2(0.38f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.58f));
            TextMeshProUGUI blue_text = CreateText("BlueScoreText", blue_bg.transform, "0", 34f, TextAlignmentOptions.Center, new Color(0.16f, 0.45f, 1f, 1f));
            Image timer_bg = CreateImage("TimerBackground", top_panel, new Vector2(0.39f, 0.24f), new Vector2(0.61f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.72f));
            TextMeshProUGUI timer_text = CreateText("TimerText", timer_bg.transform, "00:00", 30f, TextAlignmentOptions.Center, Color.white);
            Image red_bg = CreateImage("RedScoreBackground", top_panel, new Vector2(0.62f, 0.24f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0.58f));
            TextMeshProUGUI red_text = CreateText("RedScoreText", red_bg.transform, "0", 34f, TextAlignmentOptions.Center, new Color(0.95f, 0.18f, 0.14f, 1f));
            Image progress_bg = CreateImage("CaptureProgressBackground", top_panel, new Vector2(0f, 0f), new Vector2(1f, 0.12f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(1f, 1f, 1f, 0.15f));
            Image progress_fill = CreateImage("CaptureProgressFill", progress_bg.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.7f, 0.7f, 0.7f, 1f));

            Image health_panel = CreateImage("HealthPanel", root.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(34f, 34f), new Vector2(220f, 64f), new Color(0f, 0f, 0f, 0.58f));
            TextMeshProUGUI health_text = CreateText("HealthText", health_panel.transform, "HP 100/100", 26f, TextAlignmentOptions.Center, Color.white);

            Image weapon_panel_image = CreateImage("WeaponPanel", root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-34f, 34f), new Vector2(320f, 118f), new Color(0f, 0f, 0f, 0.58f));
            GameObject weapon_panel = weapon_panel_image.gameObject;
            TextMeshProUGUI weapon_name = CreateText("WeaponNameText", weapon_panel.transform, "", 19f, TextAlignmentOptions.Left, Color.white, new Vector2(18f, -16f), new Vector2(284f, 24f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            TextMeshProUGUI weapon_ammo = CreateText("WeaponAmmoText", weapon_panel.transform, "0", 46f, TextAlignmentOptions.Right, Color.white, new Vector2(-78f, -54f), new Vector2(164f, 54f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
            TextMeshProUGUI weapon_reserve = CreateText("WeaponReserveText", weapon_panel.transform, "/ 0", 24f, TextAlignmentOptions.Left, new Color(1f, 1f, 1f, 0.75f), new Vector2(-76f, -68f), new Vector2(64f, 28f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f));
            TextMeshProUGUI weapon_reload = CreateText("WeaponReloadText", weapon_panel.transform, "", 17f, TextAlignmentOptions.Left, Color.white, new Vector2(18f, 16f), new Vector2(180f, 24f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f));
            Image reload_bg = CreateImage("WeaponReloadProgressBackground", weapon_panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(-28f, 7f), new Color(1f, 1f, 1f, 0.14f));
            Image reload_fill = CreateImage("WeaponReloadProgressFill", reload_bg.transform, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero, Color.white);

            GameObject crosshair = CreateCrosshair(root.transform);
            GameObject hit_marker = CreateHitMarker(root.transform);
            Image damage_overlay = CreateImage("DamageOverlay", root.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, new Color(0.95f, 0.05f, 0.02f, 0f));
            damage_overlay.sprite = LoadDamageVignetteSprite();
            damage_overlay.gameObject.SetActive(false);
            Image[] damage_edges = CreateDamageEdges(root.transform);

            root.GetComponent<ClientMatchHudV2Layer>().ConfigurePrefabReferences(
                blue_bg,
                red_bg,
                blue_text,
                red_text,
                timer_text,
                progress_fill,
                weapon_panel,
                weapon_name,
                weapon_ammo,
                weapon_reserve,
                weapon_reload,
                reload_fill,
                health_text,
                crosshair,
                hit_marker,
                damage_overlay,
                damage_edges);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PrefabPath);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 anchor_min, Vector2 anchor_max, Vector2 pivot, Vector2 anchored_position, Vector2 size_delta)
        {
            GameObject target = new(name, typeof(RectTransform));
            target.transform.SetParent(parent, false);
            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchor_min;
            rect.anchorMax = anchor_max;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored_position;
            rect.sizeDelta = size_delta;
            return rect;
        }

        private static Image CreateImage(string name, Transform parent, Vector2 anchor_min, Vector2 anchor_max, Vector2 pivot, Vector2 anchored_position, Vector2 size_delta, Color color)
        {
            RectTransform rect = CreateRect(name, parent, anchor_min, anchor_max, pivot, anchored_position, size_delta);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float font_size, TextAlignmentOptions alignment, Color color)
        {
            return CreateText(name, parent, text, font_size, alignment, color, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent, string text, float font_size, TextAlignmentOptions alignment, Color color, Vector2 anchored_position, Vector2 size_delta, Vector2 anchor_min, Vector2 anchor_max, Vector2 pivot)
        {
            RectTransform rect = CreateRect(name, parent, anchor_min, anchor_max, pivot, anchored_position, size_delta);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = font_size;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateCrosshair(Transform parent)
        {
            RectTransform root = CreateRect("Crosshair", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40f, 40f));
            CreateLine("Top", root, new Vector2(0f, 9f), new Vector2(2f, 8f), 0f, new Color(1f, 1f, 1f, 0.86f));
            CreateLine("Bottom", root, new Vector2(0f, -9f), new Vector2(2f, 8f), 0f, new Color(1f, 1f, 1f, 0.86f));
            CreateLine("Left", root, new Vector2(-9f, 0f), new Vector2(8f, 2f), 0f, new Color(1f, 1f, 1f, 0.86f));
            CreateLine("Right", root, new Vector2(9f, 0f), new Vector2(8f, 2f), 0f, new Color(1f, 1f, 1f, 0.86f));
            return root.gameObject;
        }

        private static GameObject CreateHitMarker(Transform parent)
        {
            RectTransform root = CreateRect("HitMarker", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            CreateLine("TopRight", root, Vector2.zero, new Vector2(2f, 14f), -45f, new Color(1f, 0.96f, 0.72f, 0f));
            CreateLine("TopLeft", root, Vector2.zero, new Vector2(2f, 14f), 45f, new Color(1f, 0.96f, 0.72f, 0f));
            CreateLine("BottomLeft", root, Vector2.zero, new Vector2(2f, 14f), -45f, new Color(1f, 0.96f, 0.72f, 0f));
            CreateLine("BottomRight", root, Vector2.zero, new Vector2(2f, 14f), 45f, new Color(1f, 0.96f, 0.72f, 0f));
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        private static Image[] CreateDamageEdges(Transform parent)
        {
            RectTransform root = CreateRect("DamageEdges", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Color color = new(0.95f, 0.05f, 0.02f, 0f);
            Image top = CreateImage("Top", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 130f), color);
            Image bottom = CreateImage("Bottom", root, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 130f), color);
            Image left = CreateImage("Left", root, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(130f, 0f), color);
            Image right = CreateImage("Right", root, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(130f, 0f), color);
            root.gameObject.SetActive(false);
            return new[] { top, bottom, left, right };
        }

        private static Sprite LoadDamageVignetteSprite()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/Resources/NeoFPS/Samples/Shared/Sprites/HUD_DamageVignette.png");
            for (int i = 0; i < assets.Length; i++)
                if (assets[i] is Sprite sprite && sprite.name == "HUD_DamageVignette_Full")
                    return sprite;

            return null;
        }

        private static void CreateLine(string name, Transform parent, Vector2 anchored_position, Vector2 size_delta, float rotation_z, Color color)
        {
            Image image = CreateImage(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchored_position, size_delta, color);
            image.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation_z);
        }

        private static void SetLayerKind(ClientUiLayer layer, ClientUiLayerKind kind)
        {
            SerializedObject serialized = new(layer);
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
