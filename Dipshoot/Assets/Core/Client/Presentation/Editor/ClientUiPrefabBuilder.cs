using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Core.ClientPresentation.Editor
{
    public static class ClientUiPrefabBuilder
    {
        private const string ResourcesFolder = "Assets/Core/Client/Presentation/Resources";
        private const string PrefabsFolder = "Assets/Core/Client/Presentation/Resources/ClientUI";
        private const string AutoBuildRequestFile = "Temp/RebuildClientUiPrefabs.request";

        [InitializeOnLoadMethod]
        private static void BuildAllIfRequested()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoBuildRequestFile))
                    return;

                try
                {
                    BuildAll();
                    Debug.Log("Client UI prefabs rebuilt from request file.");
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
                finally
                {
                    if (File.Exists(AutoBuildRequestFile))
                        File.Delete(AutoBuildRequestFile);
                }
            };
        }

        [MenuItem("Dipshoot/Client UI/Rebuild Prefabs")]
        public static void BuildAll()
        {
            EnsureFolders();

            GameObject error_message_prefab = BuildErrorMessagePrefab();
            BuildMainMenuLayer();
            BuildLobbyLayer();
            BuildErrorLayer(error_message_prefab);
            BuildLoadingLayer();
            BuildMatchHudLayer();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildMainMenuLayer()
        {
            GameObject root = CreateLayerRoot<ClientMainMenuLayer>(
                "ClientMainMenuLayer",
                10,
                ClientUiLayerKind.MainMenu);

            GameObject panel = CreatePanel(
                "MainMenuPanel",
                root.transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(34f, 0f),
                new Vector2(420f, -68f),
                new Color(0f, 0f, 0f, 0.72f));

            Text nickname_text = CreateText(
                "NicknameText",
                panel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(-44f, 36f),
                22,
                TextAnchor.MiddleLeft);

            Text title = CreateText(
                "Title",
                panel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -92f),
                new Vector2(-44f, 48f),
                34,
                TextAnchor.MiddleLeft);
            title.text = "Dipshoot";

            InputField lobby_code_field = CreateInputField(
                "LobbyCodeField",
                panel.transform,
                new Vector2(0f, 90f),
                new Vector2(320f, 48f),
                "Lobby code");

            Button create_lobby_button = CreateButton(
                "CreateLobbyButton",
                panel.transform,
                new Vector2(0f, 26f),
                new Vector2(320f, 48f),
                "Create Lobby");

            Button enter_lobby_button = CreateButton(
                "EnterLobbyButton",
                panel.transform,
                new Vector2(0f, -38f),
                new Vector2(320f, 48f),
                "Enter Lobby");

            Button return_to_match_button = CreateButton(
                "ReturnToMatchButton",
                panel.transform,
                new Vector2(0f, 26f),
                new Vector2(320f, 48f),
                "Return to Match");

            Button leave_match_button = CreateButton(
                "LeaveMatchButton",
                panel.transform,
                new Vector2(0f, -38f),
                new Vector2(320f, 48f),
                "Leave Match");

            Button exit_button = CreateButton(
                "ExitButton",
                panel.transform,
                new Vector2(0f, -190f),
                new Vector2(320f, 48f),
                "Exit");

            SerializedObject serialized_object = new(root.GetComponent<ClientMainMenuLayer>());
            Set(serialized_object, "_nickname_text", nickname_text);
            Set(serialized_object, "_lobby_code_field", lobby_code_field);
            Set(serialized_object, "_create_lobby_button", create_lobby_button);
            Set(serialized_object, "_enter_lobby_button", enter_lobby_button);
            Set(serialized_object, "_return_to_match_button", return_to_match_button);
            Set(serialized_object, "_leave_match_button", leave_match_button);
            Set(serialized_object, "_exit_button", exit_button);
            serialized_object.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, "ClientMainMenuLayer.prefab");
        }

        private static void BuildLobbyLayer()
        {
            GameObject root = CreateLayerRoot<ClientLobbyLayer>(
                "ClientLobbyLayer",
                11,
                ClientUiLayerKind.Lobby);

            GameObject panel = CreatePanel(
                "LobbyPanel",
                root.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(620f, 560f),
                new Color(0f, 0f, 0f, 0.72f));

            Text title = CreateText(
                "Title",
                panel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -34f),
                new Vector2(-48f, 44f),
                32,
                TextAnchor.MiddleLeft);
            title.text = "Lobby";

            Text lobby_code_text = CreateText(
                "LobbyCodeText",
                panel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -86f),
                new Vector2(-48f, 34f),
                22,
                TextAnchor.MiddleLeft);

            Text capacity_text = CreateText(
                "CapacityText",
                panel.transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -124f),
                new Vector2(-48f, 34f),
                20,
                TextAnchor.MiddleLeft);

            List<Text> player_rows = new();
            for (int i = 0; i < 8; i++)
            {
                Text row = CreateText(
                    $"PlayerRow{i}",
                    panel.transform,
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f),
                    new Vector2(0.5f, 1f),
                    new Vector2(0f, -178f - i * 36f),
                    new Vector2(-48f, 30f),
                    20,
                    TextAnchor.MiddleLeft);
                player_rows.Add(row);
            }

            Button leave_button = CreateButton(
                "LeaveButton",
                panel.transform,
                new Vector2(-170f, -232f),
                new Vector2(230f, 48f),
                "Leave");

            Button start_game_button = CreateButton(
                "StartGameButton",
                panel.transform,
                new Vector2(170f, -232f),
                new Vector2(230f, 48f),
                "Start Game");

            SerializedObject serialized_object = new(root.GetComponent<ClientLobbyLayer>());
            Set(serialized_object, "_lobby_code_text", lobby_code_text);
            Set(serialized_object, "_capacity_text", capacity_text);
            Set(serialized_object, "_leave_button", leave_button);
            Set(serialized_object, "_start_game_button", start_game_button);
            SerializedProperty player_rows_property = serialized_object.FindProperty("_player_rows");
            player_rows_property.arraySize = player_rows.Count;
            for (int i = 0; i < player_rows.Count; i++)
                player_rows_property.GetArrayElementAtIndex(i).objectReferenceValue = player_rows[i];
            serialized_object.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, "ClientLobbyLayer.prefab");
        }

        private static void BuildErrorLayer(GameObject message_prefab)
        {
            GameObject root = CreateLayerRoot<ClientErrorLayer>(
                "ClientErrorLayer",
                120,
                ClientUiLayerKind.MainMenu);

            GameObject container = new("ErrorContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            container.transform.SetParent(root.transform, false);
            RectTransform rect_transform = container.GetComponent<RectTransform>();
            rect_transform.anchorMin = new Vector2(1f, 1f);
            rect_transform.anchorMax = new Vector2(1f, 1f);
            rect_transform.pivot = new Vector2(1f, 1f);
            rect_transform.anchoredPosition = new Vector2(-28f, -28f);
            rect_transform.sizeDelta = new Vector2(420f, 420f);

            VerticalLayoutGroup layout_group = container.GetComponent<VerticalLayoutGroup>();
            layout_group.childAlignment = TextAnchor.UpperRight;
            layout_group.childControlWidth = true;
            layout_group.childControlHeight = false;
            layout_group.childForceExpandWidth = true;
            layout_group.childForceExpandHeight = false;
            layout_group.spacing = 8f;

            SerializedObject serialized_object = new(root.GetComponent<ClientErrorLayer>());
            Set(serialized_object, "_container", container.transform);
            Set(serialized_object, "_message_prefab", message_prefab);
            serialized_object.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, "ClientErrorLayer.prefab");
        }

        private static GameObject BuildErrorMessagePrefab()
        {
            GameObject panel = CreatePanel(
                "ClientErrorMessage",
                null,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 54f),
                new Color(0.45f, 0.04f, 0.04f, 0.92f));
            panel.AddComponent<CanvasGroup>();

            Text text = CreateText(
                "Text",
                panel.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-24f, 0f),
                18,
                TextAnchor.MiddleLeft);
            text.text = "Error";

            return SavePrefab(panel, "ClientErrorMessage.prefab");
        }

        private static void BuildLoadingLayer()
        {
            GameObject root = CreateLayerRoot<ClientLoadingLayer>(
                "ClientLoadingLayer",
                100,
                ClientUiLayerKind.Loading);

            Image background = root.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.78f);

            Text text = CreateText(
                "LoadingText",
                root.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                32,
                TextAnchor.MiddleCenter);
            text.text = "Loading match...";

            SavePrefab(root, "ClientLoadingLayer.prefab");
        }

        private static void BuildMatchHudLayer()
        {
            GameObject root = CreateLayerRoot<ClientMatchHudLayer>(
                "ClientMatchHudLayer",
                20,
                ClientUiLayerKind.MatchHud);

            GameObject score_panel = CreatePanel(
                "ScorePanel",
                root.transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(620f, 92f),
                new Color(0f, 0f, 0f, 0.58f));

            Text score_text = CreateText(
                "ScoreText",
                score_panel.transform,
                new Vector2(0f, 0.42f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                34,
                TextAnchor.MiddleCenter);

            Text round_text = CreateText(
                "RoundText",
                score_panel.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.42f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                20,
                TextAnchor.MiddleCenter);

            GameObject phase_banner = CreatePanel(
                "PhaseBanner",
                root.transform,
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(640f, 84f),
                new Color(0f, 0f, 0f, 0.62f));

            Text phase_text = CreateText(
                "PhaseText",
                phase_banner.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                30,
                TextAnchor.MiddleCenter);

            GameObject result_panel = CreatePanel(
                "ResultPanel",
                root.transform,
                new Vector2(0.5f, 0.58f),
                new Vector2(0.5f, 0.58f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(560f, 88f),
                new Color(0f, 0f, 0f, 0.5f));

            Text result_text = CreateText(
                "ResultText",
                result_panel.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter);

            GameObject point_panel = CreatePanel(
                "PointPanel",
                root.transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 34f),
                new Vector2(640f, 96f),
                new Color(0f, 0f, 0f, 0.58f));

            Image point_owner_strip = CreateImage(
                "PointOwner",
                point_panel.transform,
                new Vector2(0f, 0f),
                new Vector2(0.018f, 1f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Color(0.7f, 0.7f, 0.7f, 1f));

            Text point_text = CreateText(
                "PointText",
                point_panel.transform,
                new Vector2(0.05f, 0.48f),
                new Vector2(0.95f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleLeft);

            Text inside_text = CreateText(
                "InsideText",
                point_panel.transform,
                new Vector2(0.05f, 0f),
                new Vector2(0.95f, 0.42f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                18,
                TextAnchor.MiddleLeft);

            GameObject progress_background = CreatePanel(
                "PointProgressBackground",
                point_panel.transform,
                new Vector2(0.05f, 0.42f),
                new Vector2(0.95f, 0.48f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 1f, 1f, 0.15f));

            Image point_progress_fill = CreateImage(
                "PointProgressFill",
                progress_background.transform,
                Vector2.zero,
                new Vector2(0f, 1f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Color(0.7f, 0.7f, 0.7f, 1f));

            GameObject weapon_panel = CreatePanel(
                "WeaponPanel",
                root.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-34f, 34f),
                new Vector2(320f, 118f),
                new Color(0f, 0f, 0f, 0.58f));
            weapon_panel.GetComponent<Image>().raycastTarget = false;

            Text weapon_name_text = CreateText(
                "WeaponNameText",
                weapon_panel.transform,
                new Vector2(0f, 0.64f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-28f, 0f),
                20,
                TextAnchor.MiddleLeft);

            Text weapon_ammo_text = CreateText(
                "WeaponAmmoText",
                weapon_panel.transform,
                new Vector2(0f, 0.24f),
                new Vector2(0.58f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                42,
                TextAnchor.MiddleRight);

            Text weapon_reserve_text = CreateText(
                "WeaponReserveText",
                weapon_panel.transform,
                new Vector2(0.58f, 0.24f),
                new Vector2(1f, 0.72f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-28f, 0f),
                24,
                TextAnchor.MiddleLeft);

            Text weapon_reload_text = CreateText(
                "WeaponReloadText",
                weapon_panel.transform,
                new Vector2(0f, 0f),
                new Vector2(1f, 0.26f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-28f, 0f),
                17,
                TextAnchor.MiddleLeft);

            GameObject weapon_reload_background = CreatePanel(
                "WeaponReloadBackground",
                weapon_panel.transform,
                new Vector2(0.08f, 0.06f),
                new Vector2(0.92f, 0.1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 1f, 1f, 0.15f));
            weapon_reload_background.GetComponent<Image>().raycastTarget = false;

            Image weapon_reload_progress_fill = CreateImage(
                "WeaponReloadProgressFill",
                weapon_reload_background.transform,
                Vector2.zero,
                new Vector2(0f, 1f),
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Color(1f, 1f, 1f, 0.78f));
            weapon_reload_progress_fill.raycastTarget = false;

            GameObject crosshair = CreateCrosshair(root.transform);

            SerializedObject serialized_object = new(root.GetComponent<ClientMatchHudLayer>());
            Set(serialized_object, "_phase_banner", phase_banner);
            Set(serialized_object, "_result_panel", result_panel);
            Set(serialized_object, "_score_text", score_text);
            Set(serialized_object, "_round_text", round_text);
            Set(serialized_object, "_phase_text", phase_text);
            Set(serialized_object, "_result_text", result_text);
            Set(serialized_object, "_point_text", point_text);
            Set(serialized_object, "_inside_text", inside_text);
            Set(serialized_object, "_point_owner_strip", point_owner_strip);
            Set(serialized_object, "_point_progress_fill", point_progress_fill);
            Set(serialized_object, "_weapon_panel", weapon_panel);
            Set(serialized_object, "_weapon_name_text", weapon_name_text);
            Set(serialized_object, "_weapon_ammo_text", weapon_ammo_text);
            Set(serialized_object, "_weapon_reserve_text", weapon_reserve_text);
            Set(serialized_object, "_weapon_reload_text", weapon_reload_text);
            Set(serialized_object, "_weapon_reload_progress_fill", weapon_reload_progress_fill);
            Set(serialized_object, "_crosshair", crosshair);
            serialized_object.ApplyModifiedPropertiesWithoutUndo();

            SavePrefab(root, "ClientMatchHudLayer.prefab");
        }

        private static GameObject CreateCrosshair(Transform parent)
        {
            GameObject root = new("Crosshair", typeof(RectTransform));
            root.transform.SetParent(parent, false);

            RectTransform rect_transform = root.GetComponent<RectTransform>();
            rect_transform.anchorMin = new Vector2(0.5f, 0.5f);
            rect_transform.anchorMax = new Vector2(0.5f, 0.5f);
            rect_transform.pivot = new Vector2(0.5f, 0.5f);
            rect_transform.anchoredPosition = Vector2.zero;
            rect_transform.sizeDelta = new Vector2(40f, 40f);

            Color color = new(1f, 1f, 1f, 0.86f);
            CreateImage("Top", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 9f), new Vector2(2f, 8f), color).raycastTarget = false;
            CreateImage("Bottom", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -9f), new Vector2(2f, 8f), color).raycastTarget = false;
            CreateImage("Left", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-9f, 0f), new Vector2(8f, 2f), color).raycastTarget = false;
            CreateImage("Right", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(9f, 0f), new Vector2(8f, 2f), color).raycastTarget = false;

            return root;
        }

        private static GameObject CreateLayerRoot<T>(
            string name,
            int sorting_order,
            ClientUiLayerKind kind) where T : Component
        {
            GameObject root = new(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ClientUiLayer), typeof(T));
            RectTransform rect_transform = root.GetComponent<RectTransform>();
            rect_transform.anchorMin = Vector2.zero;
            rect_transform.anchorMax = Vector2.one;
            rect_transform.offsetMin = Vector2.zero;
            rect_transform.offsetMax = Vector2.zero;

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sorting_order;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GraphicRaycaster raycaster = root.GetComponent<GraphicRaycaster>();
            raycaster.ignoreReversedGraphics = true;

            SerializedObject serialized_object = new(root.GetComponent<ClientUiLayer>());
            SerializedProperty kind_property = serialized_object.FindProperty("_kind");
            kind_property.enumValueIndex = (int)kind;
            serialized_object.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            Color color)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (parent != null)
                target.transform.SetParent(parent, false);

            RectTransform rect_transform = target.GetComponent<RectTransform>();
            rect_transform.anchorMin = anchor_min;
            rect_transform.anchorMax = anchor_max;
            rect_transform.pivot = pivot;
            rect_transform.anchoredPosition = anchored_position;
            rect_transform.sizeDelta = size_delta;

            Image image = target.GetComponent<Image>();
            image.color = color;
            return target;
        }

        private static Image CreateImage(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            Color color)
        {
            GameObject target = CreatePanel(name, parent, anchor_min, anchor_max, pivot, anchored_position, size_delta, color);
            return target.GetComponent<Image>();
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchor_min,
            Vector2 anchor_max,
            Vector2 pivot,
            Vector2 anchored_position,
            Vector2 size_delta,
            int font_size,
            TextAnchor alignment)
        {
            GameObject target = new(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            target.transform.SetParent(parent, false);

            RectTransform rect_transform = target.GetComponent<RectTransform>();
            rect_transform.anchorMin = anchor_min;
            rect_transform.anchorMax = anchor_max;
            rect_transform.pivot = pivot;
            rect_transform.anchoredPosition = anchored_position;
            rect_transform.sizeDelta = size_delta;

            Text text = target.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = font_size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.supportRichText = true;
            return text;
        }

        private static Button CreateButton(
            string name,
            Transform parent,
            Vector2 anchored_position,
            Vector2 size_delta,
            string label)
        {
            GameObject target = CreatePanel(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchored_position,
                size_delta,
                new Color(0.08f, 0.08f, 0.08f, 0.92f));

            Button button = target.AddComponent<Button>();
            button.targetGraphic = target.GetComponent<Image>();

            Text text = CreateText(
                "Text",
                target.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                22,
                TextAnchor.MiddleCenter);
            text.text = label;

            return button;
        }

        private static InputField CreateInputField(
            string name,
            Transform parent,
            Vector2 anchored_position,
            Vector2 size_delta,
            string placeholder)
        {
            GameObject target = CreatePanel(
                name,
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                anchored_position,
                size_delta,
                new Color(1f, 1f, 1f, 0.92f));

            InputField input = target.AddComponent<InputField>();
            input.targetGraphic = target.GetComponent<Image>();

            Text text = CreateText(
                "Text",
                target.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(14f, 0f),
                new Vector2(-28f, 0f),
                22,
                TextAnchor.MiddleLeft);
            text.color = Color.black;
            text.raycastTarget = true;

            Text placeholder_text = CreateText(
                "Placeholder",
                target.transform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                new Vector2(14f, 0f),
                new Vector2(-28f, 0f),
                22,
                TextAnchor.MiddleLeft);
            placeholder_text.text = placeholder;
            placeholder_text.color = new Color(0f, 0f, 0f, 0.48f);

            input.textComponent = text;
            input.placeholder = placeholder_text;
            return input;
        }

        private static void Set(SerializedObject serialized_object, string property_name, Object value)
        {
            SerializedProperty property = serialized_object.FindProperty(property_name);
            property.objectReferenceValue = value;
        }

        private static GameObject SavePrefab(GameObject target, string file_name)
        {
            string path = $"{PrefabsFolder}/{file_name}";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(target, path);
            Object.DestroyImmediate(target);
            return prefab;
        }

        private static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/Core/Client/Presentation", "Resources");

            if (!AssetDatabase.IsValidFolder(PrefabsFolder))
                AssetDatabase.CreateFolder(ResourcesFolder, "ClientUI");
        }

        private static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font != null ? font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
    }
}
