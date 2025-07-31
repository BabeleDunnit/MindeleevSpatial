#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.Linq;

[CustomEditor(typeof(LSystemEvolutor))]
public class LSystemEvolutorEditor : Editor
{
    private int editingRuleIndex = -1;
    private string editingKey = null;
    private string editingValue = null;
    private bool keyFieldFocusedLastFrame = false;
    private bool valueFieldFocusedLastFrame = false;

    // Default rules for reset
    private static readonly LSystemRule[] defaultRules = new LSystemRule[]
    {
        new LSystemRule { key = "tkO", value = "lC" },
        new LSystemRule { key = "tk", value = "n" },
        new LSystemRule { key = "t", value = "tk" },
        new LSystemRule { key = "k", value = "n" },
        new LSystemRule { key = "n", value = "a" },
        new LSystemRule { key = "a", value = "d" },
        new LSystemRule { key = "d", value = "" },
        new LSystemRule { key = "l", value = "tl" },
        new LSystemRule { key = "T", value = "T" },
        new LSystemRule { key = "C", value = "O" },
        new LSystemRule { key = "O", value = "tO" },
        new LSystemRule { key = "I", value = "I" },
        new LSystemRule { key = "D", value = "D" },
    };

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LSystemEvolutor evolutor = (LSystemEvolutor)target;
        var rules = evolutor.rules;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("L-System Rules", EditorStyles.boldLabel);

        for (int i = 0; i < rules.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            bool isEditing = (editingRuleIndex == i);

            // Key field
            GUI.SetNextControlName($"KeyField_{i}");
            string newKey = isEditing ? editingKey : rules[i].key;
            newKey = EditorGUILayout.TextField(newKey, GUILayout.Width(80));
            bool keyFocused = (GUI.GetNameOfFocusedControl() == $"KeyField_{i}");

            // Value field
            GUI.SetNextControlName($"ValueField_{i}");
            string newValue = isEditing ? editingValue : rules[i].value;
            newValue = EditorGUILayout.TextField(newValue, GUILayout.Width(80));
            bool valueFocused = (GUI.GetNameOfFocusedControl() == $"ValueField_{i}");

            // Start editing if focus enters either field
            if (!isEditing && (keyFocused || valueFocused))
            {
                editingRuleIndex = i;
                editingKey = rules[i].key;
                editingValue = rules[i].value;
            }

            // Update editing values live
            if (isEditing)
            {
                editingKey = newKey;
                editingValue = newValue;
            }

            // Commit edit if both fields lose focus (including Tab)
            if (isEditing && !keyFocused && !valueFocused && (keyFieldFocusedLastFrame || valueFieldFocusedLastFrame))
            {
                CommitEdit(evolutor, i, editingKey, editingValue);
            }

            // Commit edit on Enter key
            if (isEditing && Event.current.isKey && Event.current.keyCode == KeyCode.Return)
            {
                CommitEdit(evolutor, i, editingKey, editingValue);
                GUI.FocusControl(null); // Remove focus
                Event.current.Use();
            }

            // Per-rule delete button
            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                evolutor.rules.RemoveAt(i);
                if (editingRuleIndex == i)
                {
                    editingRuleIndex = -1;
                    editingKey = null;
                    editingValue = null;
                }
                EditorUtility.SetDirty(evolutor);
                EditorGUILayout.EndHorizontal();
                break; // List changed, break out of loop
            }

            EditorGUILayout.EndHorizontal();

            // Track focus for next frame
            if (isEditing)
            {
                keyFieldFocusedLastFrame = keyFocused;
                valueFieldFocusedLastFrame = valueFocused;
            }
        }

        EditorGUILayout.Space();

        // Add new rule
        if (GUILayout.Button("Add New Rule"))
        {
            // Commit any pending edit before adding
            if (editingRuleIndex >= 0)
            {
                CommitEdit(evolutor, editingRuleIndex, editingKey, editingValue);
            }
            GUI.FocusControl(null); // Force focus loss
            evolutor.rules.Add(new LSystemRule { key = "newKey", value = "newValue" });
            editingRuleIndex = evolutor.rules.Count - 1;
            editingKey = "newKey";
            editingValue = "newValue";
            EditorUtility.SetDirty(evolutor);
        }

        // Reset to default rules
        if (GUILayout.Button("Reset"))
        {
            evolutor.rules.Clear();
            evolutor.rules.AddRange(defaultRules.Select(r => new LSystemRule { key = r.key, value = r.value }));
            editingRuleIndex = -1;
            editingKey = null;
            editingValue = null;
            EditorUtility.SetDirty(evolutor);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Evolve"))
        {
            evolutor.Evolve();
        }
    }

    private void CommitEdit(LSystemEvolutor evolutor, int index, string newKey, string newValue)
    {
        if (index >= 0 && index < evolutor.rules.Count)
        {
            evolutor.rules[index].key = newKey;
            evolutor.rules[index].value = newValue;
            // After commit, sort by descending key length
            evolutor.rules = evolutor.rules.OrderByDescending(r => r.key.Length).ToList();
            EditorUtility.SetDirty(evolutor);
        }
        editingRuleIndex = -1;
        editingKey = null;
        editingValue = null;
        keyFieldFocusedLastFrame = false;
        valueFieldFocusedLastFrame = false;
    }
}

#endif
