using System;
using Cysharp.Threading.Tasks;
using Njinx.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DecalcomanieSceneManager : MonoBehaviour
{
    [SerializeField] BaseUI fadeUI;

    void Awake()
    {
        fadeUI.ShowUI();
    }

    void Start()
    {
        fadeUI.HideUI(.2f).Forget();
    }

    public void MoveSceneTo(string sceneName)
    {
        LoadSceneAsync(() => SceneManager.LoadSceneAsync(sceneName)).Forget();
    }

    public void MoveSceneTo(int sceneIndex)
    {
        LoadSceneAsync(() => SceneManager.LoadSceneAsync(sceneIndex)).Forget();
    }

    private async UniTask LoadSceneAsync(Func<AsyncOperation> loadOperation)
    {
        // 페이드가 시작되는 순간부터 새 씬이 완전히 드러날 때까지 조작을 막습니다.
        // 중간에 예외가 나도 잠금이 남지 않도록 finally에서 반드시 풉니다.
        GameInput.IsLocked = true;

        try
        {
            await fadeUI.ShowUI(.4f);
            await UniTask.Yield();
            var op = loadOperation();
            op.allowSceneActivation = false;
            while (op.progress < 0.9f) await UniTask.Yield();
            op.allowSceneActivation = true;
            await UniTask.WaitUntil(() => op.isDone);
            await UniTask.Yield();
            await fadeUI.HideUI(.4f);
        }
        finally
        {
            GameInput.IsLocked = false;
        }
    }

    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }
}
