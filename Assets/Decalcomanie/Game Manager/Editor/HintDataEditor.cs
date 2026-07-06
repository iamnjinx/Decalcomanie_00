using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(HintData))]
public class HintDataEditor : Editor
{
    private const string Hint2Dir = "Assets/Resources/Stage Hint/Hint2";
    private const string Hint3Dir = "Assets/Resources/Stage Hint/Hint3";

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        if (GUILayout.Button("Auto Fill Hint2 / Hint3 Sprites"))
            AutoFillSprites((HintData)target);
    }

    private static void AutoFillSprites(HintData hintData)
    {
        int maxStage = Mathf.Max(FindMaxStageNumber(Hint2Dir, "hint2_"), FindMaxStageNumber(Hint3Dir, "hint3_"));

        while (hintData.hintElements.Count < maxStage)
            hintData.hintElements.Add(new HintElement());

        for (int i = 0; i < hintData.hintElements.Count; i++)
        {
            int stageNum = i + 1;
            var hint2 = AssetDatabase.LoadAssetAtPath<Sprite>($"{Hint2Dir}/hint2_{stageNum}.jpg");
            var hint3 = AssetDatabase.LoadAssetAtPath<Sprite>($"{Hint3Dir}/hint3_{stageNum}.jpg");

            if (hint2 != null) hintData.hintElements[i].Hint2Sprite = hint2;
            if (hint3 != null) hintData.hintElements[i].Hint3Sprite = hint3;
        }

        EditorUtility.SetDirty(hintData);
        AssetDatabase.SaveAssets();
        Debug.Log($"[HintData] Auto-filled hint sprites up to stage {maxStage}.");
    }

    private static int FindMaxStageNumber(string folder, string prefix)
    {
        int max = 0;
        if (!Directory.Exists(folder)) return max;

        foreach (var file in Directory.GetFiles(folder))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            if (!name.StartsWith(prefix)) continue;

            if (int.TryParse(name.Substring(prefix.Length), out int num))
                max = Mathf.Max(max, num);
        }
        return max;
    }
}
