using System.Collections.Generic;
using TMPro;
using UnityEngine;

internal sealed class EnemyIntentOverlay
{
    private readonly RectTransform root;
    private readonly TMP_Text fontTemplate;
    private readonly Canvas canvas;
    private readonly List<EnemyIntentView> views = new();
    private Camera battleCamera;
    private int boundCount;

    public EnemyIntentOverlay(RectTransform root, TMP_Text fontTemplate)
    {
        this.root = root;
        this.fontTemplate = fontTemplate;
        canvas = root.GetComponentInParent<Canvas>(true);
    }

    public void Refresh(IReadOnlyList<Enemy> enemies, PlayerStatus player)
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            if (i == views.Count) views.Add(new EnemyIntentView(root, fontTemplate));
            views[i].Refresh(enemies[i], player);
        }
        for (int i = enemies.Count; i < boundCount; i++) views[i].Clear();
        boundCount = enemies.Count;
    }

    public void Clear()
    {
        for (int i = 0; i < boundCount; i++) views[i].Clear();
        boundCount = 0;
    }

    public void UpdatePositions(bool battleScreenVisible)
    {
        if (boundCount == 0) return;

        bool show = battleScreenVisible && root.gameObject.activeInHierarchy;
        if (show && battleCamera == null) battleCamera = Camera.main;
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? canvas.worldCamera : null;
        for (int i = 0; i < boundCount; i++)
        {
            if (show && battleCamera != null) views[i].FollowEnemy(battleCamera, uiCamera);
            else views[i].Hide();
        }
    }
}
