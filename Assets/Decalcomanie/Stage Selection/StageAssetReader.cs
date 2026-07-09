using UnityEngine;

public class StageAssetReader : MonoBehaviour
{
    public TextAsset LoadStageAsset(int stageIndex)
    {
        StageData stageData = GameManager.Instance.GetStageData(stageIndex);
        if (stageData == null || stageData.stageJson == null)
        {
            Debug.LogWarning($"Stage asset not found for stage index: {stageIndex}");
            return null;
        }
        return stageData.stageJson;
    }
}
