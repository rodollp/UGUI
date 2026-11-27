using System;
using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
    [Header("Player Data")]
    [SerializeField] private PlayerData playerData;
    [Header("Stages (in floor order)")]
    [SerializeField] private StageData[] stages;
    [SerializeField] private RunMapData mapData;
    public RunMap Map { get; private set; }
    public bool HasMap => Map != null;
    public int RunVersion { get; private set; }
    public event Action OnMapChanged;

    public MasterDeck MasterDeck { get; private set; }
    public PlayerStatus PlayerStatus { get; private set; }
    private readonly List<RelicData> relics = new();
    public IReadOnlyList<RelicData> Relics => relics;
    public int Gold { get; private set; }
    public int CardsRemoved { get; private set; }
    public int CardRemovalPrice => (int)Math.Min(int.MaxValue, 50L + 50L * CardsRemoved);
    private bool removingCard;
    public event Action OnGoldChanged;
    public int StageIndex { get; private set; }
    public int Floor => StageIndex + 1;
    public bool HasStageSequence => stages != null && stages.Length > 0;
    public bool IsRunComplete => HasMap ? Map.IsComplete : HasStageSequence && StageIndex >= stages.Length;
    public StageData CurrentStage => !HasMap && HasStageSequence && !IsRunComplete ? stages[StageIndex] : null;
    public EncounterData CurrentEncounter { get; private set; }

    public void StartNewRun()
    {
        if (playerData == null || playerData.StarterDeck == null)
        {
            Debug.LogError("PlayerData 또는 시작 덱이 설정되지 않았습니다.");
            return;
        }
        if (mapData != null && !mapData.IsValid())
        {
            Debug.LogError("맵의 전투·사건·휴식·보물·상점 설정을 확인하세요.");
            return;
        }
        MasterDeck = new MasterDeck(playerData.StarterDeck);
        PlayerStatus = new PlayerStatus(playerData.MaxHp);
        relics.Clear();
        TryAddRelic(playerData.StartingRelic);
        Gold = 0;
        CardsRemoved = 0;
        OnGoldChanged?.Invoke();
        StageIndex = 0;
        Map = mapData == null ? null : RunMapGenerator.Generate(mapData,
            mapData.UseFixedSeed ? mapData.Seed : Guid.NewGuid().GetHashCode());
        RunVersion++;
        SelectCurrentEncounter();
        OnMapChanged?.Invoke();
    }

    // 현재 노드의 진행을 확정합니다.
    public void CompleteStage()
    {
        if (HasMap)
        {
            if (Map.CompleteActiveNode()) OnMapChanged?.Invoke();
            return;
        }
        if (!HasStageSequence || IsRunComplete) return;
        StageIndex++;
        SelectCurrentEncounter();
    }

    private void SelectCurrentEncounter()
    {
        CurrentEncounter = CurrentStage != null ? CurrentStage.SelectEncounter() : null;
    }

    public bool OwnsRelic(RelicData relic) => relic != null && relics.Contains(relic);

    private bool CanAcquireRelic(RelicData relic) =>
        relic != null && PlayerStatus is { IsDead: false } && !OwnsRelic(relic);

    public bool TryAddRelic(RelicData relic)
    {
        if (!CanAcquireRelic(relic)) return false;
        AddRelic(relic);
        return true;
    }

    internal bool TryBuyRelic(RelicData relic, int price)
    {
        if (price <= 0 || Gold < price || !CanAcquireRelic(relic)) return false;
        Gold -= price;
        AddRelic(relic);
        OnGoldChanged?.Invoke();
        return true;
    }

    private void AddRelic(RelicData relic)
    {
        relics.Add(relic);
        PlayerStatus.AddStrikeDamageBonus(relic.StrikeDamageBonus);
    }

    public int AddGold(int amount)
    {
        if (amount <= 0 || PlayerStatus == null) return 0;
        int added = Math.Min(amount, int.MaxValue - Gold);
        Gold += added;
        if (added > 0) OnGoldChanged?.Invoke();
        return added;
    }

    public bool TrySpendGold(int amount)
    {
        if (amount < 0 || PlayerStatus == null || amount > Gold) return false;
        Gold -= amount;
        if (amount > 0) OnGoldChanged?.Invoke();
        return true;
    }

    public bool TryEnterMapNode(int id)
    {
        if (!HasMap || PlayerStatus == null || PlayerStatus.IsDead || !Map.TryEnter(id)) return false;
        StageIndex = Map.ActiveNode.Row;
        CurrentEncounter = Map.ActiveNode.Encounter;
        OnMapChanged?.Invoke();
        return true;
    }

    public bool TryRemoveCard(CardInstance card)
    {
        if (removingCard || PlayerStatus == null || PlayerStatus.IsDead || MasterDeck == null
            || card == null || !System.Linq.Enumerable.Contains(MasterDeck.Cards, card)
            || Gold < CardRemovalPrice) return false;
        removingCard = true;
        try
        {
            if (!TrySpendGold(CardRemovalPrice)) return false;
            MasterDeck.RemoveCard(card);
            if (CardsRemoved < int.MaxValue) CardsRemoved++;
            return true;
        }
        finally { removingCard = false; }
    }
}

