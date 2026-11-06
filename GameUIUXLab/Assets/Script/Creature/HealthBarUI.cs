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

        hpFill.fillAmount = status.MaxHp > 0
            ? (float)status.CurrentHp / status.MaxHp
            : 0f;

        hpText.text = $"{status.CurrentHp} / {status.MaxHp}";

        // CreatureStatus의 실제 블록 프로퍼티 이름에 맞춰 연결
    }

    private void OnDestroy()
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;
    }
}