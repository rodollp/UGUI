using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private Slider hpSlider;
    [SerializeField] private TextMeshProUGUI hpText;

    private CreatureStatus status;

    public void Initialize(CreatureStatus targetStatus)
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;

        status = targetStatus;
        status.OnStatusChanged += RefreshUI;

        RefreshUI();
    }

    private void RefreshUI()
    {
        hpSlider.value = (float)status.CurrentHp / status.MaxHp;
        hpText.text = $"{status.CurrentHp} / {status.MaxHp}";
    }

    private void OnDestroy()
    {
        if (status != null)
            status.OnStatusChanged -= RefreshUI;
    }
}