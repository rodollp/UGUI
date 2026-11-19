using System.Collections.Generic;

public enum MapNodeType { Battle, Elite, Event, Rest, Treasure, Boss, Shop }

public sealed class MapNode
{
    private readonly List<int> outgoing = new List<int>();
    public int Id { get; }
    public int Row { get; }
    public int Column { get; }
    public MapNodeType Type { get; }
    public bool IsUnknown { get; }
    public EncounterData Encounter { get; }
    public MapEventData Event { get; }
    public ShopData Shop { get; }
    public bool Completed { get; internal set; }
    public IReadOnlyList<int> Outgoing { get; }
    public bool IsCombat => Type == MapNodeType.Battle || Type == MapNodeType.Elite || Type == MapNodeType.Boss;

    internal MapNode(int id, int row, int column, MapNodeType type, EncounterData encounter, MapEventData data,
        bool isUnknown = false, ShopData shop = null)
    {
        Id = id; Row = row; Column = column; Type = type; Encounter = encounter; Event = data;
        Shop = shop;
        IsUnknown = isUnknown;
        Outgoing = outgoing.AsReadOnly();
    }

    internal void Connect(int id) { if (!outgoing.Contains(id)) outgoing.Add(id); }

    public string Label
    {
        get
        {
            if (IsUnknown && !Completed) return "미지의 장소";
            switch (Type)
            {
                case MapNodeType.Elite: return "정예";
                case MapNodeType.Event: return "사건";
                case MapNodeType.Rest: return "휴식";
                case MapNodeType.Treasure: return "보물";
                case MapNodeType.Boss: return "보스";
                case MapNodeType.Shop: return "상점";
                default: return "전투";
            }
        }
    }
}

public sealed class RunMap
{
    private readonly List<MapNode> nodes;
    public int Seed { get; }
    public int Rows { get; }
    public IReadOnlyList<MapNode> Nodes { get; }
    public MapNode ActiveNode { get; private set; }
    public MapNode LastCompletedNode { get; private set; }
    public bool IsComplete => LastCompletedNode != null && LastCompletedNode.Type == MapNodeType.Boss;

    internal RunMap(List<MapNode> nodes, int rows, int seed)
    {
        this.nodes = nodes;
        Nodes = nodes.AsReadOnly(); Rows = rows; Seed = seed;
    }

    public MapNode Find(int id) => nodes.Find(n => n.Id == id);

    public bool CanEnter(int id) => CanEnterNode(Find(id));

    private bool CanEnterNode(MapNode node)
    {
        if (node == null || node.Completed || ActiveNode != null || IsComplete) return false;
        return LastCompletedNode == null ? node.Row == 0 : Contains(LastCompletedNode.Outgoing, node.Id);
    }

    public bool TryEnter(int id)
    {
        MapNode node = Find(id);
        if (!CanEnterNode(node)) return false;
        ActiveNode = node;
        return true;
    }

    public bool CompleteActiveNode()
    {
        if (ActiveNode == null) return false;
        ActiveNode.Completed = true;
        LastCompletedNode = ActiveNode;
        ActiveNode = null;
        return true;
    }

    public void CancelActiveNode() => ActiveNode = null;

    private static bool Contains(IReadOnlyList<int> ids, int id)
    {
        foreach (int value in ids) if (value == id) return true;
        return false;
    }
}

