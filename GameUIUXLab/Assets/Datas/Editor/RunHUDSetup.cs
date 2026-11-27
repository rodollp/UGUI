using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class RunHUDSetup
{
    private const string Folder = "Assets/Prefabs/RunHUD";
    private static TMP_Text fontTemplate;
    private static readonly Color Gold = new(.92f, .74f, .43f);
    private static readonly Color Text = new(.94f, .92f, .85f);

    // Explicit menu only: opening the project never overwrites an edited HUD prefab.
    [MenuItem("Tools/UI/Install Common Run HUD")]
    public static void Configure()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/CardGame.unity");
        UIScreenFlowController flow = Find<UIScreenFlowController>();
        if (flow.HasCommonHUD) { Debug.Log("RUN_HUD_ALREADY_INSTALLED"); return; }
        PlayHUD playHUD = Find<PlayHUD>();
        var play = new SerializedObject(playHUD);
        fontTemplate = (TMP_Text)play.FindProperty("turnText").objectReferenceValue;
        var flowData = new SerializedObject(flow);
        Transform canvas = ((GameObject)flowData.FindProperty("playScreen").objectReferenceValue).transform.parent;
        RectTransform content = Rect("ScreenContent", canvas);
        Stretch(content, Vector2.zero, new Vector2(0, -132));
        foreach (string name in new[] { "playScreen", "mapScreen", "eventScreen", "resultScreen", "pauseScreen", "refreshScreen" })
        {
            var screen = (GameObject)flowData.FindProperty(name).objectReferenceValue;
            if (screen == null) continue;
            screen.transform.SetParent(content, false);
            Stretch((RectTransform)screen.transform, Vector2.zero, Vector2.zero);
        }
        foreach (string name in new[] { "timeText", "goldText", "stageText" })
        {
            var label = (TMP_Text)play.FindProperty(name).objectReferenceValue;
            if (label != null) label.gameObject.SetActive(false);
        }
        var playScreen = (GameObject)flowData.FindProperty("playScreen").objectReferenceValue;
        foreach (Button button in playScreen.GetComponentsInChildren<Button>(true))
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentMethodName(i) == "ShowPauseScreen") button.gameObject.SetActive(false);

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefabs", "RunHUD");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/CommonRunHUD.prefab");
        if (prefab == null)
        {
            var source = BuildHUD();
            prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, Folder + "/CommonRunHUD.prefab");
            Object.DestroyImmediate(source.gameObject);
        }
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas);
        var hud = instance.GetComponent<RunHUD>();
        Set(hud, "run", Find<RunManager>()); Set(hud, "timer", Find<RunTimer>());
        Set(hud, "flow", flow); Set(hud, "deck", Find<DeckViewUI>()); Set(hud, "overlayRoot", content);
        Set(flow, "commonHUD", instance);
        Set(Find<DeckViewUI>(), "canvasRoot", content);
        Set(Find<RunMapUI>(), "useCommonHUD", true);
        Transform mapScreen = Find<RunMapUI>().transform;
        ((RectTransform)mapScreen.Find("Title")).anchoredPosition = new Vector2(0, 400);
        ((RectTransform)mapScreen.Find("Graph")).sizeDelta = new Vector2(1200, 600);
        ((RectTransform)mapScreen.Find("Hint")).anchoredPosition = new Vector2(0, -355);
        ((RectTransform)mapScreen.Find("ReturnTitle")).anchoredPosition = new Vector2(0, -420);
        var mapStatus = new SerializedObject(Find<RunMapUI>()).FindProperty("statusText").objectReferenceValue as TMP_Text;
        if (mapStatus != null) mapStatus.gameObject.SetActive(false);
        var eventStatus = new SerializedObject(Find<MapEventUI>()).FindProperty("statusText").objectReferenceValue as TMP_Text;
        if (eventStatus != null) eventStatus.gameObject.SetActive(false);
        instance.SetActive(true); // Visible and editable before Play; flow hides it on the title screen.
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("RUN_HUD_SETUP_OK");
    }

    private static RunHUD BuildHUD()
    {
        RectTransform root = Rect("CommonRunHUD", null);
        root.anchorMin = new Vector2(0, 1); root.anchorMax = Vector2.one;
        root.pivot = new Vector2(.5f, 1); root.sizeDelta = new Vector2(0, 132);
        var canvas = root.gameObject.AddComponent<Canvas>(); canvas.overrideSorting = true; canvas.sortingOrder = 100;
        root.gameObject.AddComponent<GraphicRaycaster>();
        var hud = root.gameObject.AddComponent<RunHUD>();
        Image bar = Panel("TopBar", root, new Color(.075f, .12f, .14f));
        bar.rectTransform.anchorMin = new Vector2(0, 1); bar.rectTransform.anchorMax = Vector2.one;
        bar.rectTransform.pivot = new Vector2(.5f, 1); bar.rectTransform.sizeDelta = new Vector2(0, 78);
        Image line = Panel("GoldDivider", root, Gold);
        line.rectTransform.anchorMin = line.rectTransform.anchorMax = new Vector2(0, 1);
        line.rectTransform.anchorMax = Vector2.one; line.rectTransform.pivot = Vector2.up;
        line.rectTransform.anchoredPosition = new Vector2(0, -78); line.rectTransform.sizeDelta = new Vector2(0, 2);

        RectTransform left = Rect("CharacterAndHealth", bar.transform); Place(left, new Vector2(0, .5f), new Vector2(20, 0), new Vector2(460, 64), new Vector2(0, .5f));
        var face = Panel("Portrait", left, new Color(.25f, .32f, .34f));
        Place(face.rectTransform, new Vector2(0, .5f), Vector2.zero, new Vector2(60, 60), new Vector2(0, .5f));
        face.preserveAspect = true; Set(hud, "portrait", face);
        Set(hud, "portraitPlaceholder", Label("PortraitHint", face.transform, "초상화", 14, TextAlignmentOptions.Center).gameObject);
        TMP_Text name = Label("CharacterName", left, "모험가", 19);
        Place(name.rectTransform, new Vector2(0, 1), new Vector2(78, 0), new Vector2(140, 26), Vector2.up); Set(hud, "characterText", name);
        TMP_Text hp = Label("HealthValue", left, "80 / 80", 25);
        hp.color = new Color(1, .51f, .48f);
        Place(hp.rectTransform, new Vector2(0, 1), new Vector2(78, -27), new Vector2(155, 30), Vector2.up); Set(hud, "healthText", hp);
        var track = Panel("HealthTrack", left, new Color(.25f, .18f, .18f));
        Place(track.rectTransform, new Vector2(0, 1), new Vector2(78, -59), new Vector2(150, 4), Vector2.up);
        var fill = Panel("HealthFill", track.transform, new Color(.9f, .29f, .28f));
        fill.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/GeneratedEnemyHealthBar/SolidWhite.png").OfType<Sprite>().First();
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 1; Set(hud, "healthFill", fill);
        TMP_Text goldCaption = Label("GoldCaption", left, "골드", 18); goldCaption.color = Gold;
        Place(goldCaption.rectTransform, new Vector2(0, 1), new Vector2(270, 0), new Vector2(100, 25), Vector2.up);
        TMP_Text gold = Label("GoldValue", left, "0", 27); gold.color = Gold;
        Place(gold.rectTransform, new Vector2(0, 1), new Vector2(270, -27), new Vector2(140, 32), Vector2.up); Set(hud, "goldText", gold);
        TMP_Text node = Label("CurrentNode", bar.transform, "1층 · 경로 선택", 24, TextAlignmentOptions.Center);
        Place(node.rectTransform, new Vector2(.46f, .5f), Vector2.zero, new Vector2(300, 55), new Vector2(.5f, .5f)); Set(hud, "nodeText", node);
        TMP_Text time = Label("ElapsedTime", bar.transform, "00:00:00", 24, TextAlignmentOptions.Center);
        Place(time.rectTransform, new Vector2(1, .5f), new Vector2(-450, 0), new Vector2(150, 55), new Vector2(.5f, .5f)); Set(hud, "timeText", time);
        Button map = Button("MapButton", bar.transform, "지도", -304); Set(hud, "mapButton", map);
        Button deck = Button("DeckButton", bar.transform, "덱 0", -184); Set(hud, "deckButton", deck);
        Set(hud, "deckText", deck.GetComponentInChildren<TMP_Text>());
        Button settings = Button("SettingsButton", bar.transform, "설정", -64); Set(hud, "settingsButton", settings);

        Image relicBar = Panel("RelicBar", root, new Color(.09f, .145f, .16f));
        relicBar.rectTransform.anchorMin = Vector2.zero; relicBar.rectTransform.anchorMax = Vector2.right;
        relicBar.rectTransform.pivot = Vector2.zero; relicBar.rectTransform.sizeDelta = new Vector2(0, 52);
        TMP_Text heading = Label("RelicHeading", relicBar.transform, "유물", 20); heading.color = Gold;
        Place(heading.rectTransform, new Vector2(0, .5f), new Vector2(24, 0), new Vector2(55, 44), new Vector2(0, .5f));
        var scrollRoot = Rect("RelicScroll", relicBar.transform); Stretch(scrollRoot, new Vector2(88, 4), new Vector2(-20, -4));
        var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>(); scroll.vertical = false; scroll.scrollSensitivity = 32;
        var viewport = Panel("Viewport", scrollRoot, Color.clear); viewport.raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var items = Rect("Items", viewport.transform); items.anchorMin = Vector2.zero; items.anchorMax = Vector2.up; items.pivot = new Vector2(0, .5f); items.sizeDelta = Vector2.zero;
        var layout = items.gameObject.AddComponent<HorizontalLayoutGroup>(); layout.spacing = 8;
        layout.childAlignment = TextAnchor.MiddleLeft; layout.childControlHeight = true; layout.childControlWidth = false;
        layout.childForceExpandHeight = true; layout.childForceExpandWidth = false;
        var fitter = items.gameObject.AddComponent<ContentSizeFitter>(); fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.rectTransform; scroll.content = items; scroll.movementType = ScrollRect.MovementType.Clamped;
        var empty = Label("EmptyRelics", items, "보유 유물 없음", 18); empty.color = new Color(.55f, .64f, .63f);
        empty.rectTransform.sizeDelta = new Vector2(190, 40); Set(hud, "emptyRelicsText", empty);
        RunRelicItem item = BuildRelic();
        item.transform.SetParent(items, false);
        Set(hud, "relicTemplate", item);

        var tooltip = Panel("RelicTooltip", root, new Color(.065f, .10f, .12f, .99f)); tooltip.raycastTarget = false;
        Place(tooltip.rectTransform, Vector2.up, new Vector2(24, -138), new Vector2(420, 170), Vector2.up);
        Set(hud, "tooltip", tooltip.rectTransform);
        var title = Label("Title", tooltip.transform, "유물 이름", 23); title.color = Gold;
        Place(title.rectTransform, Vector2.up, new Vector2(18, -12), new Vector2(384, 34), Vector2.up); Set(hud, "tooltipTitle", title);
        var description = Label("Description", tooltip.transform, "유물 효과 설명", 20, TextAlignmentOptions.TopLeft);
        description.textWrappingMode = TextWrappingModes.Normal;
        Stretch(description.rectTransform, new Vector2(18, 12), new Vector2(-18, -58)); Set(hud, "tooltipDescription", description);
        tooltip.gameObject.SetActive(false);
        return hud;
    }

    private static RunRelicItem BuildRelic()
    {
        var image = Panel("RunRelicItem", null, new Color(.15f, .22f, .23f)); image.raycastTarget = true;
        image.rectTransform.sizeDelta = new Vector2(170, 40);
        var item = image.gameObject.AddComponent<RunRelicItem>(); Set(item, "background", image);
        var icon = Panel("Icon", image.transform, Gold);
        Place(icon.rectTransform, new Vector2(0, .5f), new Vector2(7, 0), new Vector2(28, 28), new Vector2(0, .5f)); icon.preserveAspect = true; icon.gameObject.SetActive(false); Set(item, "icon", icon);
        TMP_Text label = Label("Name", image.transform, "유물 항목", 18, TextAlignmentOptions.Center);
        Stretch(label.rectTransform, new Vector2(36, 0), new Vector2(-6, 0)); Set(item, "label", label);
        return item;
    }

    private static Button Button(string name, Transform parent, string caption, float x)
    {
        var image = Panel(name, parent, new Color(.16f, .23f, .25f)); image.raycastTarget = true;
        Place(image.rectTransform, new Vector2(1, .5f), new Vector2(x, 0), new Vector2(108, 48), new Vector2(.5f, .5f));
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        Label("Label", image.transform, caption, 22, TextAlignmentOptions.Center);
        return button;
    }
    private static TMP_Text Label(string name, Transform parent, string text, float size, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var rect = Rect(name, parent); Stretch(rect, Vector2.zero, Vector2.zero);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = fontTemplate.font; label.fontSharedMaterial = fontTemplate.fontSharedMaterial;
        label.text = text; label.fontSize = size; label.color = Text; label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap; label.overflowMode = TextOverflowModes.Ellipsis; label.raycastTarget = false;
        return label;
    }
    private static Image Panel(string name, Transform parent, Color color)
    {
        var rect = Rect(name, parent); Stretch(rect, Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
    private static RectTransform Rect(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); return rect;
    }
    private static void Stretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = min; rect.offsetMax = max;
    }
    private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
    }
    private static T Find<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
    private static void Set(Object target, string name, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo();
        if (PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
    private static void Set(Object target, string name, bool value)
    {
        var data = new SerializedObject(target); data.FindProperty(name).boolValue = value; data.ApplyModifiedPropertiesWithoutUndo();
    }
}
