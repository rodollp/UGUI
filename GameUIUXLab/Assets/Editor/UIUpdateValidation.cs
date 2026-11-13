using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class UIUpdateValidation
{
    public static void VerifyMap(RunMapUI ui, RunManager run)
    {
        var root = (RectTransform)new SerializedObject(ui).FindProperty("graphRoot").objectReferenceValue;
        Transform[] instances = root.GetComponentsInChildren<Transform>(true);
        var scroll = ui.GetComponentInChildren<ScrollRect>(true);
        scroll.verticalNormalizedPosition = .4f;
        ui.Refresh(); ui.Refresh(); // Warm equality comparers before measuring.
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 200; i++) ui.Refresh();
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Expect(allocated == 0, "unchanged map refresh allocates no managed memory after warmup");
        Expect(Mathf.Abs(scroll.verticalNormalizedPosition - .4f) < .001f, "unchanged refresh preserves manual scroll");
        run.AddGold(1);
        Expect(instances.SequenceEqual(root.GetComponentsInChildren<Transform>(true)),
            "gold and repeated refresh reuse all map objects");
        var text = (TMP_Text)new SerializedObject(ui).FindProperty("statusText").objectReferenceValue;
        Expect(text.text.Contains($"골드 {run.Gold}"), "gold status still updates immediately");
        run.TrySpendGold(1);
        scroll.verticalNormalizedPosition = 0;
        Debug.Log($"UI_MAP_CACHE_VALIDATION_OK: 200 unchanged refresh calls allocated {allocated} managed bytes.");
    }

    public static void VerifyHUD(PlayHUD hud, RunManager run)
    {
        var flush = (Action)Delegate.CreateDelegate(typeof(Action), hud,
            typeof(PlayHUD).GetMethod("LateUpdate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        var update = (Action)Delegate.CreateDelegate(typeof(Action), hud,
            typeof(PlayHUD).GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic));
        hud.ResetHUD();
        var serialized = new SerializedObject(hud);
        var energy = (TMP_Text)serialized.FindProperty("energyText").objectReferenceValue;
        var timer = (TMP_Text)serialized.FindProperty("timeText").objectReferenceValue;
        string oldEnergy = energy.text;
        Expect(run.PlayerStatus.TryUseEnergy(1) && run.PlayerStatus.TryUseEnergy(1), "energy events generated");
        Expect(ReferenceEquals(energy.text, oldEnergy), "multiple events defer expensive HUD updates");
        flush();
        Expect(energy.text == $"에너지 {run.PlayerStatus.CurrentEnergy} / {run.PlayerStatus.MaxEnergy}", "one flush shows final energy");
        run.PlayerStatus.RestoreEnergy(); flush();
        update(); string oldTime = timer.text;
        for (int i = 0; i < 100; i++) update();
        Expect(ReferenceEquals(oldTime, timer.text), "same displayed second reuses time string");
        Debug.Log("UI_HUD_CACHE_VALIDATION_OK");
    }

    private static void Expect(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("UI update: " + message);
    }
}
