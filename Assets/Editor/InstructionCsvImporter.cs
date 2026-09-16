#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class InstructionCsvImporter : EditorWindow
{
    private FloatingInstructions targetInstructions;
    private TextAsset csvFile;
    private bool clearExisting = true;

    [MenuItem("Tools/Instructions/Import Instructions CSV")]
    public static void ShowWindow()
    {
        GetWindow<InstructionCsvImporter>("Instruction CSV Importer");
    }

    private void OnGUI()
    {
        targetInstructions = (FloatingInstructions)EditorGUILayout.ObjectField(
            "Floating Instructions",
            targetInstructions,
            typeof(FloatingInstructions),
            true
        );

        csvFile = (TextAsset)EditorGUILayout.ObjectField(
            "CSV File",
            csvFile,
            typeof(TextAsset),
            false
        );

        clearExisting = EditorGUILayout.Toggle(
            "Clear Existing Groups",
            clearExisting
        );

        EditorGUILayout.Space();

        if (GUILayout.Button("Import CSV"))
            ImportCsv();
    }

    private void ImportCsv()
    {
        if (targetInstructions == null || csvFile == null)
        {
            Debug.LogError("Assign both FloatingInstructions and a CSV file.");
            return;
        }

        Undo.RecordObject(targetInstructions, "Import Instructions CSV");

        if (clearExisting)
            targetInstructions.instructionGroups.Clear();

        List<InstructionValidationIssue> validationIssues =
            new List<InstructionValidationIssue>();

        string[] lines = csvFile.text.Split(
            new[] { "\r\n", "\n" },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (lines.Length == 0)
        {
            Debug.LogError("The CSV file is empty.");
            return;
        }

        string[] headers = ParseCsvLine(lines[0]);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = ParseCsvLine(lines[i]);
            Dictionary<string, string> row = MakeRow(headers, values);

            string groupName = Get(row, "GroupName", "Default");
            bool groupEnabled = GetBool(row, "GroupEnabled", true);

            InstructionGroup group = targetInstructions.instructionGroups
                .FirstOrDefault(g => g.groupName == groupName);

            if (group == null)
            {
                group = new InstructionGroup
                {
                    groupName = groupName,
                    isEnabled = groupEnabled,
                    steps = new List<InstructionStep>()
                };

                targetInstructions.instructionGroups.Add(group);
            }

            string stepName = Get(row, "StepName");

            ActionType actionType = GetEnum(
                Get(row, "ActionToComplete"),
                ActionType.SimpleClick
            );

            InstructionStep step = new InstructionStep
            {
                stepName = stepName,
                englishText = Get(row, "EnglishText"),
                frenchText = Get(row, "FrenchText"),
                actionType = actionType,
                actionParam1 = Get(row, "ActionParam1"),
                actionParam2 = Get(row, "ActionParam2"),
                postActions = BuildPostActions(
                    Get(row, "PostActions"),
                    i + 1,
                    groupName,
                    stepName,
                    validationIssues
                )
            };

            group.steps.Add(step);

            ValidateInstructionAction(
                step,
                i + 1,
                groupName,
                validationIssues
            );
        }

        EditorUtility.SetDirty(targetInstructions);
        PrefabUtility.RecordPrefabInstancePropertyModifications(targetInstructions);

        Debug.Log(
            $"Instruction CSV import complete. " +
            $"{validationIssues.Count} validation issue(s) found."
        );

        InstructionValidationWindow.ShowResults(validationIssues);
    }

    private static void ValidateInstructionAction(
        InstructionStep step,
        int csvRow,
        string groupName,
        List<InstructionValidationIssue> issues
    )
    {
        switch (step.actionType)
        {
            case ActionType.ClickButton:
                ValidateSceneComponent<Button>(
                    step.actionParam1,
                    csvRow,
                    groupName,
                    step.stepName,
                    "ActionParam1",
                    "Button",
                    issues
                );
                break;

            case ActionType.ClosePopup:
                ValidateSceneObject(
                    step.actionParam1,
                    csvRow,
                    groupName,
                    step.stepName,
                    "ActionParam1",
                    "GameObject",
                    issues
                );
                break;
        }
    }

    private static List<PostAction> BuildPostActions(
        string cell,
        int csvRow,
        string groupName,
        string stepName,
        List<InstructionValidationIssue> issues
    )
    {
        List<PostAction> actions = new List<PostAction>();

        if (string.IsNullOrWhiteSpace(cell))
            return actions;

        string[] actionChunks = cell.Split(',');

        foreach (string chunk in actionChunks)
        {
            if (string.IsNullOrWhiteSpace(chunk))
                continue;

            string[] parts = chunk.Split('|');
            string actionName = parts.Length > 0 ? parts[0].Trim() : "";
            string param1 = parts.Length > 1 ? parts[1].Trim() : "";
            string param2 = parts.Length > 2 ? parts[2].Trim() : "";

            if (!Enum.TryParse(actionName, true, out PostActionType type))
            {
                AddIssue(
                    issues, csvRow, groupName, stepName,
                    "PostActions", actionName, "Valid PostActionType",
                    "Unknown post-action type."
                );
                continue;
            }

            PostAction action = new PostAction
            {
                type = type,
                param1 = param1,
                param2 = param2
            };

            actions.Add(action);
            ValidatePostAction(action, csvRow, groupName, stepName, issues);
        }

        return actions;
    }

    private static void ValidatePostAction(
        PostAction action,
        int csvRow,
        string groupName,
        string stepName,
        List<InstructionValidationIssue> issues
    )
    {
        switch (action.type)
        {
            case PostActionType.ActivateObject:
            case PostActionType.OpenPopup:
            case PostActionType.DeactivateObject:
                ValidateSceneObject(
                    action.param1, csvRow, groupName, stepName,
                    "PostActions Param1", action.type.ToString(), issues
                );
                break;

            case PostActionType.LERPObject:
            case PostActionType.MoveObjectImmediate:
                ValidateSceneObject(
                    action.param1, csvRow, groupName, stepName,
                    "PostActions Param1", "target GameObject", issues
                );
                ValidateSceneObject(
                    action.param2, csvRow, groupName, stepName,
                    "PostActions Param2", "destination Transform", issues
                );
                break;

            case PostActionType.ChangeCamera:
                ValidateSceneComponent<Camera>(
                    action.param1, csvRow, groupName, stepName,
                    "PostActions Param1", "Camera", issues
                );
                break;
        }
    }

    private static void ValidateSceneObject(
        string objectName,
        int csvRow,
        string groupName,
        string stepName,
        string field,
        string expected,
        List<InstructionValidationIssue> issues
    )
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            AddIssue(
                issues, csvRow, groupName, stepName,
                field, objectName, expected,
                "Required object name is empty."
            );
            return;
        }

        if (FindSceneObject(objectName) == null)
        {
            AddIssue(
                issues, csvRow, groupName, stepName,
                field, objectName, expected,
                "No scene object with this name was found."
            );
        }
    }

    private static void ValidateSceneComponent<T>(
        string objectName,
        int csvRow,
        string groupName,
        string stepName,
        string field,
        string expected,
        List<InstructionValidationIssue> issues
    ) where T : Component
    {
        GameObject found = FindSceneObject(objectName);

        if (found == null)
        {
            AddIssue(
                issues, csvRow, groupName, stepName,
                field, objectName, expected,
                "No scene object with this name was found."
            );
            return;
        }

        if (found.GetComponent<T>() == null)
        {
            AddIssue(
                issues, csvRow, groupName, stepName,
                field, objectName, expected,
                $"The object exists but has no {typeof(T).Name} component."
            );
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return null;

        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
            return activeObject;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform transform in allTransforms)
        {
            if (transform == null || !transform.gameObject.scene.IsValid())
                continue;

            if (transform.name == objectName)
                return transform.gameObject;
        }

        return null;
    }

    private static void AddIssue(
        List<InstructionValidationIssue> issues,
        int csvRow,
        string groupName,
        string stepName,
        string field,
        string suppliedValue,
        string expected,
        string message
    )
    {
        issues.Add(new InstructionValidationIssue
        {
            csvRow = csvRow,
            groupName = groupName,
            stepName = stepName,
            field = field,
            suppliedValue = suppliedValue,
            expected = expected,
            message = message
        });
    }

    private static Dictionary<string, string> MakeRow(
        string[] headers,
        string[] values
    )
    {
        Dictionary<string, string> row = new Dictionary<string, string>();

        for (int i = 0; i < headers.Length; i++)
        {
            string value = i < values.Length ? values[i] : "";
            row[headers[i].Trim()] = value.Trim();
        }

        return row;
    }

    private static string Get(
        Dictionary<string, string> row,
        string key,
        string fallback = ""
    )
    {
        return row.ContainsKey(key) ? row[key] : fallback;
    }

    private static bool GetBool(
        Dictionary<string, string> row,
        string key,
        bool fallback
    )
    {
        return bool.TryParse(Get(row, key), out bool result)
            ? result
            : fallback;
    }

    private static ActionType GetEnum(
        string value,
        ActionType fallback
    )
    {
        return Enum.TryParse(value, true, out ActionType result)
            ? result
            : fallback;
    }

    private static string[] ParseCsvLine(string line)
    {
        List<string> values = new List<string>();
        bool inQuotes = false;
        string current = "";

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current += '"';
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(current);
                current = "";
            }
            else
            {
                current += c;
            }
        }

        values.Add(current);
        return values.ToArray();
    }
}

[Serializable]
public class InstructionValidationIssue
{
    public int csvRow;
    public string groupName;
    public string stepName;
    public string field;
    public string suppliedValue;
    public string expected;
    public string message;
}

public class InstructionValidationWindow : EditorWindow
{
    private List<InstructionValidationIssue> issues =
        new List<InstructionValidationIssue>();

    private Vector2 scrollPosition;

    public static void ShowResults(
        List<InstructionValidationIssue> validationIssues
    )
    {
        InstructionValidationWindow window =
            GetWindow<InstructionValidationWindow>("Instruction Validation");

        window.issues = validationIssues ??
            new List<InstructionValidationIssue>();

        window.minSize = new Vector2(700f, 350f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "Instruction Import Validation",
            EditorStyles.boldLabel
        );

        EditorGUILayout.Space();

        if (issues == null || issues.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Import completed successfully. No missing scene objects were found.",
                MessageType.Info
            );
            return;
        }

        EditorGUILayout.HelpBox(
            $"The import completed, but {issues.Count} possible problem(s) were found.",
            MessageType.Warning
        );

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (InstructionValidationIssue issue in issues)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(
                $"CSV row {issue.csvRow}: {issue.groupName} / {issue.stepName}",
                EditorStyles.boldLabel
            );
            EditorGUILayout.LabelField("Field", issue.field);
            EditorGUILayout.LabelField(
                "Value",
                string.IsNullOrEmpty(issue.suppliedValue)
                    ? "(empty)"
                    : issue.suppliedValue
            );
            EditorGUILayout.LabelField("Expected", issue.expected);
            EditorGUILayout.HelpBox(issue.message, MessageType.Warning);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3f);
        }

        EditorGUILayout.EndScrollView();
    }
}
#endif
