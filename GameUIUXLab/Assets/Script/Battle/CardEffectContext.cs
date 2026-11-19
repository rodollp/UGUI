using System;
using System.Collections.Generic;

public class CardEffectContext
{
    public PlayerStatus Player { get; }
    public EnemyStatus Target { get; }
    public IReadOnlyList<EnemyStatus> Targets { get; }

    public CardEffectContext(
        PlayerStatus player,
        EnemyStatus target,
        IReadOnlyList<EnemyStatus> targets = null)
    {
        Player = player;
        Target = target;
        Targets = targets ?? (target == null ? Array.Empty<EnemyStatus>() : new[] { target });
    }
}
