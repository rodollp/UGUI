
using UnityEngine;

public class RunManager : MonoBehaviour
{
    [Header("Player Data")]
    [SerializeField] private PlayerData playerData;

    public MasterDeck MasterDeck { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }

    public void StartNewRun()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerData가 설정되지 않았습니다.");
            return;
        }

        if (playerData.StarterDeck == null)
        {
            Debug.LogError("시작 덱이 설정되지 않았습니다.");
            return;
        }

        MasterDeck = new MasterDeck(playerData.StarterDeck);
        PlayerStatus = new PlayerStatus(playerData.MaxHp);

        Debug.Log($"새 게임 시작! 보유 카드: {MasterDeck.Cards.Count}장");
        Debug.Log($"시작 체력: {PlayerStatus.CurrentHp}/{PlayerStatus.MaxHp}");
    }
}
