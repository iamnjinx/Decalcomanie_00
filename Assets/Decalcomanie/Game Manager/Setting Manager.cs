using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [SerializeField] private SettingUI settingUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void OpenSetting()
    {
        settingUI.ShowUI();
    }

    public void CloseSetting()
    {
        settingUI.HideUI();
    }
}
