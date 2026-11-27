using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapEventUI : MonoBehaviour
{
    [SerializeField] private AdventureController adventure;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button[] choiceButtons;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject upgradePanel;
    private ShopUI shopUI;

    private void Awake()
    {
        var shopRoot = new GameObject("ShopPanel", typeof(RectTransform));
        shopRoot.transform.SetParent(transform, false);
        shopRoot.SetActive(false);
        shopUI = shopRoot.AddComponent<ShopUI>();
        shopUI.Initialize(adventure, upgradePanel.GetComponentsInChildren<CardUI>(true)[0], titleText);
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;
            choiceButtons[i].onClick.AddListener(() => adventure.ChooseEvent(index));
        }
        continueButton.onClick.AddListener(adventure.ReturnFromEvent);
    }
    private void OnEnable() { adventure.OnEventChanged += Refresh; Refresh(); }
    private void OnDisable() => adventure.OnEventChanged -= Refresh;

    private void Refresh()
    {
        MapEventData data = adventure.CurrentEvent;
        if (upgradePanel != null) upgradePanel.SetActive(adventure.IsChoosingUpgrade);
        shopUI.gameObject.SetActive(adventure.IsShopping);
        if (adventure.IsShopping) shopUI.transform.SetAsLastSibling();

        if (data != null) RefreshEventText(data);
        bool showChoices = data != null && !adventure.EventResolved
            && !adventure.IsChoosingUpgrade && !adventure.IsShopping;
        bool treasureRelicAvailable = showChoices && adventure.Run.Map?.ActiveNode?.Type == MapNodeType.Treasure
            && RelicData.GetUnowned(data.TreasureRelicRewards?.RelicPool, adventure.Run.Relics).Count > 0;
        RefreshChoices(showChoices ? data.Choices : null, treasureRelicAvailable);
        continueButton.gameObject.SetActive(adventure.EventResolved);
    }

    private void RefreshEventText(MapEventData data)
    {
        RunManager run = adventure.Run;
        titleText.text = data.Title;
        descriptionText.text = adventure.EventResolved ? adventure.EventResult : data.Description;
        statusText.text = $"체력 {run.PlayerStatus.CurrentHp}/{run.PlayerStatus.MaxHp}  ·  골드 {run.Gold}  ·  덱 {run.MasterDeck.Cards.Count}장";
    }

    private void RefreshChoices(MapEventChoice[] choices, bool treasureRelicAvailable)
    {
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            Button button = choiceButtons[i];
            bool visible = choices != null && i < choices.Length && !adventure.Run.OwnsRelic(choices[i].Relic);
            button.gameObject.SetActive(visible);
            if (!visible) continue;

            MapEventChoice choice = choices[i];
            button.interactable = adventure.CanChooseEvent(i);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            label.text = choice.Label + (treasureRelicAvailable ? " + 유물" : "")
                + (choice.GoldCost > 0 ? $"  [골드 {choice.GoldCost}]" : "")
                + (choice.HealthCost > 0 ? $"  [체력 {choice.HealthCost}]" : "");
            if (choice.UpgradeCard && !adventure.HasUpgradeableCards)
                label.text += "  [강화할 카드 없음]";
        }
    }
}

