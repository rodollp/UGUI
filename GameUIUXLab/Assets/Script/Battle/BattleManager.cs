using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RunManager runManager;
    [SerializeField] private Player player;
    [SerializeField] private CardManager cardManager;

    public void StartBattle()
    {
        if (runManager.PlayerStatus == null)
        {
            Debug.LogError("PlayerStatus가 생성되지 않았습니다.");
            return;
        }

        if (runManager.MasterDeck == null)
        {
            Debug.LogError("MasterDeck이 생성되지 않았습니다.");
            return;
        }

        // 플레이어 상태 및 UI 초기화
        player.Initialize(runManager.PlayerStatus);

        // 카드 시스템 초기화
        cardManager.StartBattle();
    }
}