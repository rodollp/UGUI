using UnityEngine;

public class RunManager : MonoBehaviour
{
    [Header("Starter Deck")]
    [SerializeField] private StarterDeckData starterDeckData;

    public MasterDeck MasterDeck { get; private set; }

    public void StartNewRun()
    {
        if (starterDeckData == null)
        {
            Debug.LogError("시작 덱이 설정되지 않았습니다.");
            return;
        }

        MasterDeck = new MasterDeck(starterDeckData);

        Debug.Log($"새 게임 시작! 보유 카드: {MasterDeck.Cards.Count}장");
    }
}