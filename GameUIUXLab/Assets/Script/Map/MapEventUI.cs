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
        if (data == null) return;
        RunManager run = adventure.Run;
        if (upgradePanel != null) upgradePanel.SetActive(adventure.IsChoosingUpgrade);
        shopUI.gameObject.SetActive(adventure.IsShopping);
        if (adventure.IsShopping) shopUI.transform.SetAsLastSibling();
        titleText.text = data.Title;
        descriptionText.text = adventure.EventResolved ? adventure.EventResult : data.Description;
        statusText.text = $"체력 {run.PlayerStatus.CurrentHp}/{run.PlayerStatus.MaxHp}  ·  골드 {run.Gold}  ·  덱 {run.MasterDeck.Cards.Count}장";
        for (int i = 0; i < choiceButtons.Length; i++)
        {
            bool visible = !adventure.EventResolved && !adventure.IsChoosingUpgrade && !adventure.IsShopping && i < data.Choices.Length;
            choiceButtons[i].gameObject.SetActive(visible);
            if (!visible) continue;
            MapEventChoice choice = data.Choices[i];
            choiceButtons[i].interactable = adventure.CanChooseEvent(i);
            choiceButtons[i].GetComponentInChildren<TMP_Text>(true).text = choice.Label
                + (choice.GoldCost > 0 ? $"  [골드 {choice.GoldCost}]" : "")
                + (choice.HealthCost > 0 ? $"  [체력 {choice.HealthCost}]" : "");
            if (choice.UpgradeCard && adventure.UpgradeCandidates.Length == 0)
                choiceButtons[i].GetComponentInChildren<TMP_Text>(true).text += "  [강화할 카드 없음]";
        }
        continueButton.gameObject.SetActive(adventure.EventResolved);
    }
}

