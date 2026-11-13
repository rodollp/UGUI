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
    private const float NodeWidth = 160f;
    private static float NodeHeight(int rows) => Mathf.Min(52f, 600f / Mathf.Max(1, rows - 1) - 14f);

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

    private Vector2 Position(MapNode node, int rows) =>
        new Vector2((node.Column - 1) * 310, -300 + node.Row * 600f / Mathf.Max(1, rows - 1));

    public void Refresh()
    {
        RunManager run = adventure.Run;
        if (!run.HasMap || run.PlayerStatus == null) return;
        titleText.text = run.IsRunComplete ? "탐험 완료" : "갈림길의 탑";
        statusText.text = $"체력 {run.PlayerStatus.CurrentHp}/{run.PlayerStatus.MaxHp}   ·   골드 {run.Gold}   ·   덱 {run.MasterDeck.Cards.Count}장";
        foreach (Transform child in graphRoot)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        foreach (MapNode node in run.Map.Nodes)
            foreach (int next in node.Outgoing)
            {
                MapNode target = run.Map.Find(next);
                Vector2 from = Position(node, run.Map.Rows);
                Vector2 to = Position(target, run.Map.Rows);
                Vector2 direction = (to - from).normalized;
                float inset = Mathf.Min((NodeWidth / 2 + 2) / Mathf.Max(0.001f, Mathf.Abs(direction.x)),
                    (NodeHeight(run.Map.Rows) / 2 + 2) / Mathf.Max(0.001f, Mathf.Abs(direction.y)));
                from += direction * inset;
                to -= direction * inset;
                var line = new GameObject("Path", typeof(RectTransform), typeof(Image));
                line.transform.SetParent(graphRoot, false);
                RectTransform rect = line.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = (from + to) / 2;
                rect.sizeDelta = new Vector2((to - from).magnitude, 4);
                rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg);
                Image image = line.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = node.Completed ? new Color(0.77f, 0.6f, 0.28f, 0.8f) : new Color(0.28f, 0.33f, 0.38f);
            }

        foreach (MapNode node in run.Map.Nodes)
        {
            Button button = Instantiate(nodeTemplate, graphRoot);
            button.name = "MapNode_" + node.Id;
            button.gameObject.SetActive(true);
            RectTransform rect = button.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Position(node, run.Map.Rows);
            rect.sizeDelta = new Vector2(NodeWidth, NodeHeight(run.Map.Rows));
            bool available = run.Map.CanEnter(node.Id);
            button.interactable = available && !run.PlayerStatus.IsDead;
            button.GetComponent<Image>().color = node.Completed ? new Color(0.36f, 0.29f, 0.17f)
                : available ? new Color(0.8f, 0.55f, 0.2f) : new Color(0.17f, 0.21f, 0.26f);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = $"{node.Row + 1}층 · {node.Label}" + (node.Completed ? " · 완료" : "");
            label.fontSize = 22;
            label.enableAutoSizing = true;
            label.fontSizeMin = 13;
            label.fontSizeMax = 22;
            label.color = available ? new Color(0.09f, 0.07f, 0.04f) : new Color(0.85f, 0.86f, 0.87f);
            int id = node.Id;
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() => adventure.SelectNode(id));
        }
    }
}

