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
        LoadSceneAsync(sceneName).Forget();
    }

    public void MoveSceneTo(int sceneIndex)
    {
        LoadSceneAsync(sceneIndex).Forget();
    }

    private async UniTask LoadSceneAsync(string sceneName)
    {
        await fadeUI.ShowUI(.4f);
        await UniTask.Yield();
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) await UniTask.Yield();
        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);
        await UniTask.Yield();
        await fadeUI.HideUI(.4f);
    }

    private async UniTask LoadSceneAsync(int sceneIndex)
    {
        await fadeUI.ShowUI(.4f);
        var op = SceneManager.LoadSceneAsync(sceneIndex);
        op.allowSceneActivation = false;
        while (op.progress < 0.9f) await UniTask.Yield();
        op.allowSceneActivation = true;
        await UniTask.WaitUntil(() => op.isDone);
        await UniTask.Yield();
        await fadeUI.HideUI(.4f);
    }

    public int GetCurrentSceneIndex()
    {
        return SceneManager.GetActiveScene().buildIndex;
    }
}
