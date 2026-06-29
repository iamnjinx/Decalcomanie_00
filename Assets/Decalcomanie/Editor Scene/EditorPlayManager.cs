using Njinx.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EditorPlayManager : MonoBehaviour
{
    public ButtonUI ExitButton;

    void Start()
    {
        ExitButton.OnSingleClick += Exit;
    }

    private void Exit()
    {
        SceneManager.LoadScene("Editor Scene");
    }
}
