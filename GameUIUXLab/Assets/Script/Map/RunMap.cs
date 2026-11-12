using System;
using System.Collections.Generic;

public enum MapNodeType { Battle, Elite, Event, Rest, Treasure, Boss }

public sealed class MapNode
{
    private readonly List<int> outgoing = new List<int>();
    public int Id { get; }
    public int Row { get; }
    public int Column { get; }
    public MapNodeType Type { get; }
    public EncounterData Encounter { get; }
    public MapEventData Event { get; }
    public bool Completed { get; internal set; }
    public IReadOnlyList<int> Outgoing { get; }
    public bool IsCombat => Type == MapNodeType.Battle || Type == MapNodeType.Elite || Type == MapNodeType.Boss;

    internal MapNode(int id, int row, int column, MapNodeType type, EncounterData encounter, MapEventData data)
    {
        Id = id; Row = row; Column = column; Type = type; Encounter = encounter; Event = data;
        Outgoing = outgoing.AsReadOnly();
    }

    internal void Connect(int id) { if (!outgoing.Contains(id)) outgoing.Add(id); }

    public string Label
    {
        get
        {
            switch (Type)
            {
                case MapNodeType.Elite: return "정예";
                case MapNodeType.Event: return "사건";
                case MapNodeType.Rest: return "휴식";
                case MapNodeType.Treasure: return "보물";
                case MapNodeType.Boss: return "보스";
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

    public bool CanEnter(int id)
    {
        MapNode node = Find(id);
        if (node == null || node.Completed || ActiveNode != null || IsComplete) return false;
        return LastCompletedNode == null ? node.Row == 0 : Contains(LastCompletedNode.Outgoing, id);
    }

    public bool TryEnter(int id)
    {
        if (!CanEnter(id)) return false;
        ActiveNode = Find(id);
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

public static class RunMapGenerator
{
    public static RunMap Generate(RunMapData data, int seed)
    {
        if (data == null || !data.IsValid()) throw new ArgumentException("맵 설정을 확인하세요.");
        var random = new System.Random(seed);
        var nodes = new List<MapNode>();
        for (int row = 0; row < data.Rows; row++)
        {
            int rotation = random.Next(3);
            int columns = row == data.Rows - 1 ? 1 : 3;
            for (int col = 0; col < columns; col++)
            {
                MapNodeType type;
                if (row == data.Rows - 1) type = MapNodeType.Boss;
                else if (row == 0) type = MapNodeType.Battle;
                else if (row == data.Rows - 2) type = MapNodeType.Rest;
                else if (row == 1)
                    type = new[] { MapNodeType.Event, MapNodeType.Battle, MapNodeType.Treasure }[(col + rotation) % 3];
                else if (row == 2 && col == 1) type = MapNodeType.Elite;
                else type = new[] { MapNodeType.Battle, MapNodeType.Battle, MapNodeType.Event,
                    MapNodeType.Treasure, MapNodeType.Elite }[random.Next(5)];
                EncounterData encounter = null;
                MapEventData mapEvent = null;
                switch (type)
                {
                    case MapNodeType.Battle: encounter = Pick(data.Battles, random); break;
                    case MapNodeType.Elite: encounter = Pick(data.Elites, random); break;
                    case MapNodeType.Boss: encounter = Pick(data.Bosses, random); break;
                    case MapNodeType.Rest: mapEvent = data.Rest; break;
                    case MapNodeType.Treasure: mapEvent = data.Treasure; break;
                    default: mapEvent = Pick(data.Events, random); break;
                }
                nodes.Add(new MapNode(row * 3 + col, row, columns == 1 ? 1 : col, type, encounter, mapEvent));
            }
        }
        var map = new RunMap(nodes, data.Rows, seed);
        for (int row = 0; row < data.Rows - 1; row++)
        {
            for (int col = 0; col < 3; col++)
                map.Find(row * 3 + col).Connect(row == data.Rows - 2 ? (row + 1) * 3 : (row + 1) * 3 + col);
            if (row == data.Rows - 2) continue;
            int boundary = random.Next(2);
            if (random.Next(2) == 0) map.Find(row * 3 + boundary).Connect((row + 1) * 3 + boundary + 1);
            else map.Find(row * 3 + boundary + 1).Connect((row + 1) * 3 + boundary);
        }
        return map;
    }

    private static T Pick<T>(T[] values, System.Random random) => values[random.Next(values.Length)];
}

