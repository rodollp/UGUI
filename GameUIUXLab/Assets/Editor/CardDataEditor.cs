using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CardData))]
public sealed class CardDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(CardData.CardID)));
        DrawVersion(serializedObject.FindProperty(nameof(CardData.Base)), "기본");

        var hasUpgrade = serializedObject.FindProperty(nameof(CardData.HasUpgrade));
        EditorGUILayout.PropertyField(hasUpgrade, new GUIContent("강화 가능"));
        if (hasUpgrade.boolValue)
            DrawVersion(serializedObject.FindProperty(nameof(CardData.Upgrade)), "강화");

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawVersion(SerializedProperty version, string label)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
        foreach (string name in new[] { nameof(CardVersion.CardName), nameof(CardVersion.Type),
            nameof(CardVersion.TargetType), nameof(CardVersion.EnergyCost), nameof(CardVersion.Artwork) })
        {
            EditorGUILayout.PropertyField(version.FindPropertyRelative(name));
        }

        var effects = version.FindPropertyRelative(nameof(CardVersion.Effects));
        EditorGUILayout.LabelField("효과", EditorStyles.boldLabel);
        for (int i = 0; i < effects.arraySize; i++)
        {
            var property = effects.GetArrayElementAtIndex(i);
            var effect = property.objectReferenceValue as CardEffect;
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                int move = 0;
                bool remove;
                using (new EditorGUILayout.HorizontalScope())
                {
                    string name = effect != null ? ObjectNames.NicifyVariableName(effect.GetType().Name) : "효과 누락";
                    EditorGUILayout.LabelField($"{i + 1}. {name}");
                    using (new EditorGUI.DisabledScope(i == 0))
                        if (GUILayout.Button("↑", GUILayout.Width(26))) move = -1;
                    using (new EditorGUI.DisabledScope(i == effects.arraySize - 1))
                        if (GUILayout.Button("↓", GUILayout.Width(26))) move = 1;
                    remove = GUILayout.Button("삭제", GUILayout.Width(42));
                }
                if (move != 0) effects.MoveArrayElement(i, i + move);
                if (remove)
                {
                    RemoveEffect(effects, i, effect);
                    break;
                }
                if (effect != null) DrawEffect(effect);
            }
        }

        using (new EditorGUI.DisabledScope(!EditorUtility.IsPersistent(target)))
            if (GUILayout.Button("효과 추가")) ShowEffectMenu(version.propertyPath == nameof(CardData.Upgrade));
    }

    private static void DrawEffect(CardEffect effect)
    {
        using var settings = new SerializedObject(effect);
        var property = settings.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.propertyPath != "m_Script") EditorGUILayout.PropertyField(property, true);
        }
        settings.ApplyModifiedProperties();
    }

    private void ShowEffectMenu(bool upgraded)
    {
        serializedObject.ApplyModifiedProperties();
        var card = (CardData)target;
        var menu = new GenericMenu();
        foreach (Type type in TypeCache.GetTypesDerivedFrom<CardEffect>()
            .Where(type => !type.IsAbstract && !type.ContainsGenericParameters).OrderBy(type => type.Name))
        {
            menu.AddItem(new GUIContent(ObjectNames.NicifyVariableName(type.Name)), false, () =>
            {
                if (card == null) return;
                int undoGroup = Undo.GetCurrentGroup();
                Undo.RegisterCompleteObjectUndo(card, "Add Card Effect");
                var effect = (CardEffect)ScriptableObject.CreateInstance(type);
                EmbedEffect(card, upgraded ? card.Upgrade : card.Base, effect);
                Undo.RegisterCreatedObjectUndo(effect, "Add Card Effect");
                Undo.CollapseUndoOperations(undoGroup);
                AssetDatabase.SaveAssetIfDirty(card);
            });
        }
        menu.ShowAsContext();
    }

    private void RemoveEffect(SerializedProperty effects, int index, CardEffect effect)
    {
        var card = (CardData)target;
        int undoGroup = Undo.GetCurrentGroup();
        effects.GetArrayElementAtIndex(index).objectReferenceValue = null;
        effects.DeleteArrayElementAtIndex(index);
        serializedObject.ApplyModifiedProperties();

        if (effect != null && AssetDatabase.IsSubAsset(effect)
            && AssetDatabase.GetAssetPath(effect) == AssetDatabase.GetAssetPath(card)
            && !card.Base.Effects.Contains(effect) && !card.Upgrade.Effects.Contains(effect))
        {
            Undo.DestroyObjectImmediate(effect);
        }
        Undo.CollapseUndoOperations(undoGroup);
        AssetDatabase.SaveAssetIfDirty(card);
    }

    internal static void EmbedEffect(CardData card, CardVersion version, CardEffect effect)
    {
        effect.name = ObjectNames.NicifyVariableName(effect.GetType().Name);
        AssetDatabase.AddObjectToAsset(effect, card);
        version.Effects.Add(effect);
        EditorUtility.SetDirty(card);
    }
}

