using UnityEngine;

public class RunManager : MonoBehaviour
{
    [Header("Player Data")]
    [SerializeField] private PlayerData playerData;
    [Header("Stages (in floor order)")]
    [SerializeField] private StageData[] stages;

    public MasterDeck MasterDeck { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }
    public int StageIndex { get; private set; }
    public int Floor => StageIndex + 1;
    public bool HasStageSequence => stages != null && stages.Length > 0;
    public bool IsRunComplete => HasStageSequence && StageIndex >= stages.Length;
    public StageData CurrentStage => HasStageSequence && !IsRunComplete ? stages[StageIndex] : null;
    public EncounterData CurrentEncounter { get; private set; }

    public void StartNewRun()
    {
        if (playerData == null || playerData.StarterDeck == null)
        {
            Debug.LogError("PlayerData 또는 시작 덱이 설정되지 않았습니다.");
            return;
        }
        MasterDeck = new MasterDeck(playerData.StarterDeck);
        PlayerStatus = new PlayerStatus(playerData.MaxHp);
        StageIndex = 0;
        SelectCurrentEncounter();
    }

    // BattleManager가 승리했을 때만 호출합니다.
    public void CompleteStage()
    {
        if (!HasStageSequence || IsRunComplete) return;
        StageIndex++;
        SelectCurrentEncounter();
    }

    private void SelectCurrentEncounter()
    {
        CurrentEncounter = CurrentStage != null ? CurrentStage.SelectEncounter() : null;
    }
}

