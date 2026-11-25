using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MapPresentationValidation
{
    public static void Verify()
    {
        var data = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<RunMapData>("Assets/Datas/Adventure/DefaultMap.asset"));
        int mysteryBattles = 0, mysteryEvents = 0;
        try
        {
            foreach (int chance in new[] { 0, 30, 100 })
            {
                data.UnknownBattlePercent = chance;
                for (int seed = 0; seed < 32; seed++)
                {
                    var map = RunMapGenerator.Generate(data, seed);
                    var mysteries = map.Nodes.Where(n => n.IsUnknown).ToArray();
                    Expect(mysteries.Length > 0, "mystery nodes generated");
                    foreach (var node in mysteries)
                    {
                        Expect(node.Label == "미지의 장소", "label does not expose outcome");
                        Expect(node.Type == MapNodeType.Battle || node.Type == MapNodeType.Event, "only normal battle or event");
                        if (chance == 0) Expect(!node.IsCombat && node.Event != null, "zero percent always event");
                        if (chance == 100) Expect(node.IsCombat && node.Encounter != null, "hundred percent always battle");
                        if (chance == 30)
                        {
                            if (node.IsCombat) mysteryBattles++; else mysteryEvents++;
                        }
                    }
                }
            }
            Expect(mysteryBattles > 0 && mysteryEvents > 0, "default mix generates both outcomes");
            var go = new GameObject("IconTest",typeof(RectTransform),typeof(MapNodeIcon));
            try
            {
                var icon = go.GetComponent<MapNodeIcon>();
                foreach (MapNodeType kind in Enum.GetValues(typeof(MapNodeType)))
                {
                    icon.Kind = kind;
                    using (var mesh = new VertexHelper())
                    {
                        typeof(MapNodeIcon).GetMethod("OnPopulateMesh",System.Reflection.BindingFlags.Instance
                            | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly).Invoke(icon,new object[] {mesh});
                        Expect(mesh.currentVertCount > 0,"icon geometry exists");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
            Debug.Log("MAP_PRESENTATION_VALIDATION_OK");
        }
        finally { UnityEngine.Object.DestroyImmediate(data); }
    }
    private static void Expect(bool condition,string message)
    {
        if (!condition) throw new InvalidOperationException("Map presentation: " + message);
    }
    public static void Run()
    {
        try { Verify(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        RefactorValidation.Run();
    }
}
