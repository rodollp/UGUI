using System;
using System.Collections.Generic;

public static class RunMapGenerator
{
    public const int FirstShopFloor = 4;
    private const int ColumnsPerFloor = 3;

    public static RunMap Generate(RunMapData data, int seed)
    {
        if (data == null || !data.IsValid()) throw new ArgumentException("맵 설정을 확인하세요.");

        // Keep the order of random draws stable for fixed seeds.
        var random = new Random(seed);
        List<MapNode> nodes = CreateNodes(data, random);
        PlaceShop(nodes, data, seed);
        ConnectFloors(nodes, data.Rows, random);
        ReplaceConsecutiveSpecialNodes(nodes, data, random);
        return new RunMap(nodes, data.Rows, seed);
    }

    private static List<MapNode> CreateNodes(RunMapData data, Random random)
    {
        var nodes = new List<MapNode>();
        for (int row = 0; row < data.Rows; row++)
        {
            int rotation = random.Next(ColumnsPerFloor);
            bool bossFloor = row == data.Rows - 1;
            int columns = bossFloor ? 1 : ColumnsPerFloor;
            for (int column = 0; column < columns; column++)
            {
                MapNodeType type = SelectNodeType(data, row, column, rotation, random);
                int id = nodes.Count;
                int displayColumn = bossFloor ? ColumnsPerFloor / 2 : column;
                nodes.Add(CreateNode(data, id, row, displayColumn, type, random));
            }
        }
        return nodes;
    }

    private static MapNodeType SelectNodeType(RunMapData data, int row, int column, int rotation, Random random)
    {
        if (row == data.Rows - 1) return MapNodeType.Boss;
        if (row == 0) return MapNodeType.Battle;
        if (row == data.TreasureRow) return MapNodeType.Treasure;
        if (row == data.Rows - 2) return MapNodeType.Rest;
        if (row == 1)
            return (column + rotation) % ColumnsPerFloor == 0 ? MapNodeType.Event : MapNodeType.Battle;

        return PickRegularType(data, row + 1, random);
    }

    private static MapNode CreateNode(RunMapData data, int id, int row, int column, MapNodeType type, Random random)
    {
        // Resolve mystery nodes once and keep the outcome hidden until entry.
        bool unknown = type == MapNodeType.Event;
        if (unknown && random.Next(100) < data.UnknownBattlePercent) type = MapNodeType.Battle;

        (EncounterData Encounter, MapEventData Event) content = type switch
        {
            MapNodeType.Battle => (Pick(data.Battles, random), null),
            MapNodeType.Elite => (Pick(data.Elites, random), null),
            MapNodeType.Boss => (Pick(data.Bosses, random), null),
            MapNodeType.Rest => (null, data.Rest),
            MapNodeType.Treasure => (null, data.Treasure),
            _ => (null, Pick(data.Events, random))
        };
        return new MapNode(id, row, column, type, content.Encounter, content.Event, unknown);
    }

    private static void PlaceShop(List<MapNode> nodes, RunMapData data, int seed)
    {
        if (data.Shop == null) return;
        var candidates = nodes.FindAll(node => node.Row + 1 >= FirstShopFloor
            && node.Row < data.Rows - 2 && node.Row != data.TreasureRow);
        if (candidates.Count == 0) return;

        // Shop placement uses its own sequence so encounters and paths stay independent.
        var shopRandom = new Random(seed);
        MapNode slot = candidates[shopRandom.Next(candidates.Count)];
        nodes[slot.Id] = new MapNode(slot.Id, slot.Row, slot.Column, MapNodeType.Shop,
            null, null, shop: data.Shop);
    }

    private static void ConnectFloors(List<MapNode> nodes, int rows, Random random)
    {
        for (int row = 0; row < rows - 1; row++)
        {
            int floorStart = row * ColumnsPerFloor;
            int nextFloorStart = floorStart + ColumnsPerFloor;
            bool nextIsBoss = row == rows - 2;
            for (int column = 0; column < ColumnsPerFloor; column++)
                nodes[floorStart + column].Connect(nextFloorStart + (nextIsBoss ? 0 : column));
            if (nextIsBoss) continue;

            int boundary = random.Next(ColumnsPerFloor - 1);
            if (random.Next(2) == 0) nodes[floorStart + boundary].Connect(nextFloorStart + boundary + 1);
            else nodes[floorStart + boundary + 1].Connect(nextFloorStart + boundary);
        }
    }

    private static void ReplaceConsecutiveSpecialNodes(List<MapNode> nodes, RunMapData data, Random random)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            MapNode source = nodes[i];
            if (source.Type is not (MapNodeType.Rest or MapNodeType.Elite or MapNodeType.Shop)) continue;
            foreach (int targetId in source.Outgoing)
            {
                MapNode target = nodes[targetId];
                if (target.Type != source.Type) continue;

                // Preserve the guaranteed rest immediately before the boss.
                bool targetIsRequiredRest = target.Type == MapNodeType.Rest && target.Row == data.Rows - 2;
                MapNode slot = targetIsRequiredRest ? source : target;
                MapNode battle = CreateNode(data, slot.Id, slot.Row, slot.Column, MapNodeType.Battle, random);
                foreach (int nextId in slot.Outgoing) battle.Connect(nextId);
                nodes[slot.Id] = battle;

                if (targetIsRequiredRest) break;
            }
        }
    }

    private static MapNodeType PickRegularType(RunMapData data, int floor, Random random)
    {
        var pool = new List<MapNodeType> { MapNodeType.Battle, MapNodeType.Battle, MapNodeType.Event };
        if (floor >= data.EliteStartFloor) pool.Add(MapNodeType.Elite);
        if (floor >= data.RestStartFloor) pool.Add(MapNodeType.Rest);
        return pool[random.Next(pool.Count)];
    }

    private static T Pick<T>(T[] values, Random random) => values[random.Next(values.Length)];
}
