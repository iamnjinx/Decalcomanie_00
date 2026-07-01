using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StageAssetReader : MonoBehaviour
{
    public List<TextAsset> LoadStageAssets(int maxStageIndex)
    {
        List<TextAsset> stageAssets = new List<TextAsset>();

        for (int i = 0; i <= maxStageIndex; i++)
        {
            string path = $"Stage/stage_{i}";
            TextAsset data = Resources.Load<TextAsset>(path);
            if (data != null)
            {
                stageAssets.Add(data);
            }
            else
            {
                Debug.LogWarning($"Stage asset not found at path: {path}");
            }
        }
        return stageAssets;
    }

    public TextAsset LoadStageAsset(int stageIndex)
    {
        string path = $"Stage/stage_{stageIndex+1}";
        TextAsset data = Resources.Load<TextAsset>(path);
        if (data == null)
        {
            Debug.LogWarning($"Stage asset not found at path: {path}");
        }
        return data;
    }
}
