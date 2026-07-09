using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildVariantConfig", menuName = "ScriptableObjects/BuildVariantConfig", order = 1)]
public class BuildVariantConfig : ScriptableObject
{
    public List<StageData> stages = new List<StageData>();
    public HintData hintData;
}

[System.Serializable]
public class StageData
{
    public TextAsset stageJson;
    public Sprite screenshot;
}
