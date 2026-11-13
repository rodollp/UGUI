using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RunMapUI : MonoBehaviour
{
    [SerializeField] private AdventureController adventure;
    [SerializeField] private RectTransform graphRoot;
    [SerializeField] private Button nodeTemplate;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text statusText;
    private const float RowSpacing = 126f;
    private const float IconSize = 68f;
    private ScrollRect scroll;
    private int shownVersion = -1;
    private int focusedNode = -2;
    private static readonly Color Ink = new Color(.20f,.17f,.14f);
    private static readonly Color Gold = new Color(.54f,.29f,.08f);

    private void OnEnable()
    {
        adventure.Run.OnMapChanged += Refresh;
        adventure.Run.OnGoldChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        adventure.Run.OnMapChanged -= Refresh;
        adventure.Run.OnGoldChanged -= Refresh;
    }

    private Vector2 Position(MapNode node, int rows)
    {
        // Stable node-specific offsets give paths a hand-drawn layout without rerolling outcomes.
        float offset = node.Type == MapNodeType.Boss ? 0 : ((node.Id * 37 + 11) % 35 - 17);
        return new Vector2((node.Column - 1) * 240 + offset,
            -(rows - 1) * RowSpacing / 2 + node.Row * RowSpacing);
    }

    private void PrepareViewport(int rows)
    {
        if (scroll == null)
        {
            var viewport = new GameObject("MapViewport", typeof(RectTransform), typeof(Image),
                typeof(RectMask2D), typeof(ScrollRect));
            var rect = viewport.GetComponent<RectTransform>();
            rect.SetParent(graphRoot.parent, false);
            rect.anchorMin = graphRoot.anchorMin; rect.anchorMax = graphRoot.anchorMax;
            rect.pivot = graphRoot.pivot; rect.anchoredPosition = graphRoot.anchoredPosition;
            rect.sizeDelta = graphRoot.sizeDelta;
            viewport.GetComponent<Image>().color = new Color(0,0,0,0);
            graphRoot.SetParent(rect, false);
            graphRoot.anchorMin = graphRoot.anchorMax = graphRoot.pivot = new Vector2(.5f,.5f);
            scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = rect; scroll.content = graphRoot;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 45;
            var background = GetComponent<Image>();
            if (background != null) background.color = new Color(.85f,.80f,.68f);
            titleText.color = statusText.color = Ink;
            Transform hint = transform.Find("Hint");
            if (hint != null && hint.TryGetComponent<TMP_Text>(out var hintText))
            {
                hintText.text = "휠 / 드래그로 지도 이동 · 작은 뿔: 전투 · 큰 뿔: 정예 · ?: 전투 또는 사건 · 모닥불: 휴식";
                hintText.color = Ink;
                hintText.enableAutoSizing = true; hintText.fontSizeMin = 15; hintText.fontSizeMax = 23;
            }
        }
        graphRoot.sizeDelta = new Vector2(1100, (rows - 1) * RowSpacing + 160);
    }

    public void Refresh()
    {
        RunManager run = adventure.Run;
        if (!run.HasMap || run.PlayerStatus == null) return;
        PrepareViewport(run.Map.Rows);
        titleText.text = run.IsRunComplete ? "탐험 완료" : "갈림길의 탑";
        statusText.text = $"체력 {run.PlayerStatus.CurrentHp}/{run.PlayerStatus.MaxHp}   ·   골드 {run.Gold}   ·   덱 {run.MasterDeck.Cards.Count}장";
        foreach (Transform child in graphRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (MapNode node in run.Map.Nodes)
            foreach (int id in node.Outgoing) DrawPath(node, run.Map.Find(id), run.Map.Rows);
        foreach (MapNode node in run.Map.Nodes) DrawNode(node, run);

        int completed = run.Map.LastCompletedNode?.Id ?? -1;
        if (shownVersion != run.RunVersion || focusedNode != completed)
        {
            shownVersion = run.RunVersion; focusedNode = completed;
            int row = run.Map.IsComplete ? run.Map.Rows - 1 : (run.Map.LastCompletedNode?.Row + 1 ?? 0);
            Canvas.ForceUpdateCanvases();
            float height = scroll.viewport.rect.height;
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = Mathf.Clamp01((80 + row * RowSpacing - height / 2)
                / Mathf.Max(1, graphRoot.rect.height - height));
        }
    }

    private void DrawPath(MapNode node, MapNode target, int rows)
    {
        Vector2 from = Position(node, rows), to = Position(target, rows);
        Vector2 direction = (to-from).normalized;
        from += direction * 43; to -= direction * 43;
        int count = Mathf.Max(2, Mathf.CeilToInt((to-from).magnitude / 18));
        for (int i=0; i<count; i++)
        {
            var dash = new GameObject("Path", typeof(RectTransform), typeof(Image));
            var rect = dash.GetComponent<RectTransform>(); rect.SetParent(graphRoot,false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.anchoredPosition = Vector2.Lerp(from,to,(i+.5f)/count);
            rect.sizeDelta = new Vector2(8,2.5f);
            rect.localRotation = Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var mark = dash.GetComponent<Image>(); mark.raycastTarget = false;
            mark.color = node.Completed && target.Completed ? Gold : new Color(.35f,.30f,.24f,.45f);
        }
    }

    private void DrawNode(MapNode node, RunManager run)
    {
        Button button = Instantiate(nodeTemplate, graphRoot);
        button.name = "MapNode_" + node.Id;
        button.gameObject.SetActive(true);
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f,.5f);
        rect.anchoredPosition = Position(node,run.Map.Rows);
        rect.sizeDelta = new Vector2(IconSize+16,IconSize+16);
        bool available = run.Map.CanEnter(node.Id);
        button.interactable = available && !run.PlayerStatus.IsDead;
        button.transition = Selectable.Transition.None;
        button.GetComponent<Image>().color = Color.clear;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        label.text = "";
        var iconObject = new GameObject("Icon",typeof(RectTransform),typeof(MapNodeIcon));
        var iconRect = iconObject.GetComponent<RectTransform>(); iconRect.SetParent(rect,false);
        iconRect.sizeDelta = new Vector2(IconSize,IconSize);
        var icon = iconObject.GetComponent<MapNodeIcon>();
        icon.raycastTarget = false;
        icon.Kind = node.Type; icon.Unknown = node.IsUnknown && !node.Completed;
        icon.Completed = node.Completed; icon.Available = available;
        icon.color = available || node.Completed ? Gold : Ink;
        var floorObject = new GameObject("Floor",typeof(RectTransform),typeof(TextMeshProUGUI));
        var floor = floorObject.GetComponent<TextMeshProUGUI>();
        floor.rectTransform.SetParent(rect,false); floor.rectTransform.anchoredPosition = new Vector2(-65,0);
        floor.rectTransform.sizeDelta = new Vector2(38,24);
        floor.font = label.font; floor.fontSize = 15; floor.alignment = TextAlignmentOptions.Center;
        floor.color = new Color(.40f,.35f,.28f); floor.text = (node.Row+1).ToString(); floor.raycastTarget = false;
        int id = node.Id;
        button.onClick.AddListener(() => adventure.SelectNode(id));
    }
}
