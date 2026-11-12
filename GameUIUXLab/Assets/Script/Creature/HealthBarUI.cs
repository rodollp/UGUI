using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image hpFill;
    [SerializeField] private TextMeshProUGUI hpText;

    [SerializeField] private GameObject blockGroup;
    [SerializeField] private TextMeshProUGUI blockText;

    [SerializeField] private TextMeshProUGUI debuffText;
    private CreatureStatus status;

    public void Initialize(CreatureStatus targetStatus)
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;

        status = targetStatus;
        EnsureDebuffText();

        if (status != null)
        {
            status.OnStatusChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void RefreshUI()
    {
        if (status == null)
            return;

        if (debuffText != null)
        {
            debuffText.text = status.GetDebuffDescription();
            debuffText.gameObject.SetActive(debuffText.text.Length > 0);
        }

        // 체력 표시
        hpFill.fillAmount = status.MaxHp > 0
            ? (float)status.CurrentHp / status.MaxHp
            : 0f;

        hpText.text = $"{status.CurrentHp} / {status.MaxHp}";

        // 방어도 표시
        bool hasBlock = status.CurrentBlock > 0;

        blockGroup.SetActive(hasBlock);

        if (hasBlock)
        {
            blockText.text = status.CurrentBlock.ToString();
        }
    }

    private void EnsureDebuffText()
    {
        if (debuffText != null || hpText == null) return;
        debuffText = Instantiate(hpText, hpText.transform.parent);
        debuffText.name = "DebuffText";
        RectTransform rect = debuffText.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = new Vector2(0, -5);
        rect.sizeDelta = new Vector2(240, 55);
        debuffText.fontSize = 17;
        debuffText.enableAutoSizing = true;
        debuffText.fontSizeMin = 12;
        debuffText.fontSizeMax = 17;
        debuffText.alignment = TextAlignmentOptions.Top;
        debuffText.color = new Color(1, 0.72f, 0.35f);
        debuffText.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;
    }
}
