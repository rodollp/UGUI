public class CardEffectContext
{
    public PlayerStatus Player { get; }
    public EnemyStatus Target { get; }

    public CardEffectContext(
        PlayerStatus player,
        EnemyStatus target)
    {
        Player = player;
        Target = target;
    }
}