using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DecalcomanieSceneManager : MonoBehaviour
{
    public void MoveSceneTo(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void MoveSceneTo(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }
}
