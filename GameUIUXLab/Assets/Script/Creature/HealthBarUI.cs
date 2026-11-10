using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Image hpFill;
    [SerializeField] private TextMeshProUGUI hpText;

    [SerializeField] private GameObject blockGroup;
    [SerializeField] private TextMeshProUGUI blockText;

    private CreatureStatus status;

    public void Initialize(CreatureStatus targetStatus)
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;

        status = targetStatus;

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

    private void OnDestroy()
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;
    }
}