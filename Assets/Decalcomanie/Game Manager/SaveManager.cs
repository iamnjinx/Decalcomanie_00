using BayatGames.SaveGameFree;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [SerializeField] private bool useEncryption = false;
    [SerializeField] private string encryptionPassword = "changeme";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SaveGame.Encode = useEncryption;
        if (useEncryption)
            SaveGame.EncodePassword = encryptionPassword;

#if UNITY_WEBGL && !UNITY_EDITOR
        SaveGame.UsePlayerPrefs = true;
#endif
    }

    public void Save<T>(string key, T data) => SaveGame.Save(key, data);

    public T Load<T>(string key, T defaultValue = default) => SaveGame.Load(key, defaultValue);

    public bool Exists(string key) => SaveGame.Exists(key);

    public void Delete(string key) => SaveGame.Delete(key);

    public void DeleteAll() => SaveGame.DeleteAll();
}
