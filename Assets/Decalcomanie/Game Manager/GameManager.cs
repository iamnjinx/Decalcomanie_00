using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance { get; private set; }
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    public StageAssetReader StageAssetReader;
    public DecalcomanieSceneManager SceneManager;
    public AudioManager AudioManager;

    public int CurrentStageIndex { get; private set; } = 0;

    public void LoadStage(int stageIndex)
    {
        CurrentStageIndex = stageIndex;
        TextAsset stageData = StageAssetReader.LoadStageAsset(stageIndex);
        if (stageData != null)
        {
            // Pass stageData to the game scene for initialization
            SceneManager.MoveSceneTo("Game Scene");
        }
        else
        {
            Debug.LogError($"Failed to load stage data for stage index: {stageIndex}");
        }
    }

    public TextAsset GetCurrentStageAsset()
    {
        return StageAssetReader.LoadStageAsset(CurrentStageIndex);
    }
}
