using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private Slider hpSlider;

    private int turn = 1;
    private float playTime;
    private float currentHp = 100f;
    private float maxHp = 100f;

    private void Start()
    {
        hpSlider.minValue = 0f;
        hpSlider.maxValue = maxHp;

        RefreshUI();
    }

    private void Update()
    {
        playTime += Time.deltaTime;
        RefreshTimeUI();
    }

    public void TakeDamage()
    {
        currentHp = Mathf.Max(0f, currentHp - 10f);
        RefreshHpUI();
    }

    public void NextTurn()
    {
        turn++;
        RefreshTurnUI();
    }

    private void RefreshUI()
    {

        RefreshTurnUI();
        RefreshTimeUI();
        RefreshHpUI();
    }

    private void RefreshTurnUI()
    {
        turnText.text = $"Turn : {turn}";
    }

    private void RefreshTimeUI()
    {
        timeText.text = $"Time : {playTime:00:00}";
    }

    private void RefreshHpUI()
    {
        hpSlider.value = currentHp;
    }
}