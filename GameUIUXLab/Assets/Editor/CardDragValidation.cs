using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public static class CardDragValidation
{
    [MenuItem("Tools/Battle/Validate Card Drag Cancel")]
    public static void Verify()
    {
        var parent = new GameObject("DragTestParent", typeof(RectTransform));
        var go = new GameObject("DragTestCard", typeof(RectTransform));
        var events = new GameObject("DragTestEvents", typeof(EventSystem));
        var data = ScriptableObject.CreateInstance<CardData>();
        var mouse = InputSystem.AddDevice<Mouse>();
        try
        {
            go.transform.SetParent(parent.transform, false);
            var card = go.AddComponent<CardUI>();
            Call(card, "Awake");
            typeof(CardUI).GetProperty("CardInstance").GetSetMethod(true)
                .Invoke(card, new object[] { new CardInstance(data) });
            card.SetBasePosition(new Vector2(20, 30));
            var pointer = new PointerEventData(events.GetComponent<EventSystem>()) {
                button = PointerEventData.InputButton.Left, position = new Vector2(100, 200)
            };
            int drops = 0;
            card.OnCardDropped += (_, __) => drops++;
            card.OnBeginDrag(pointer);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
            InputSystem.Update();
            Call(card, "Update");
            card.OnDrag(pointer);
            card.OnEndDrag(pointer);
            Expect(drops == 0, "right click suppresses drop after left release");
            Expect(go.GetComponent<RectTransform>().anchoredPosition == new Vector2(20, 30), "returns to hand");
            Release(mouse);
            card.OnBeginDrag(pointer);
            card.OnEndDrag(pointer);
            Expect(drops == 1, "next left drag works normally");
            card.OnBeginDrag(pointer);
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
            InputSystem.Update();
            card.OnEndDrag(pointer);
            Expect(drops == 1, "simultaneous cancel and release never drops");
            Release(mouse);
            pointer.button = PointerEventData.InputButton.Right;
            card.OnBeginDrag(pointer);
            card.OnEndDrag(pointer);
            Expect(drops == 1, "right button cannot start or use a card");
            pointer.button = PointerEventData.InputButton.Left;
            card.OnBeginDrag(pointer);
            go.SetActive(false);
            // Edit-mode verification invokes the runtime lifecycle callback explicitly.
            Call(card, "OnDisable");
            go.SetActive(true);
            card.OnEndDrag(pointer);
            Expect(drops == 1, "pooled card clears drag state");
            card.SetRewardMode();
            card.OnBeginDrag(pointer);
            card.OnEndDrag(pointer);
            Expect(drops == 1, "reward cards remain non-draggable");
            Debug.Log("CARD_DRAG_CANCEL_VALIDATION_OK: 7 assertions passed.");
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            UnityEngine.Object.DestroyImmediate(parent);
            UnityEngine.Object.DestroyImmediate(events);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }

    private static void Release(Mouse mouse)
    {
        InputSystem.QueueStateEvent(mouse, new MouseState());
        InputSystem.Update();
    }
    private static void Call(CardUI card, string name) => typeof(CardUI)
        .GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(card, null);
    private static void Expect(bool value, string message)
    {
        if (!value) throw new InvalidOperationException("Card drag: " + message);
    }
    public static void Run()
    {
        try { Verify(); }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); return; }
        BattleValidation.Run();
    }
}
