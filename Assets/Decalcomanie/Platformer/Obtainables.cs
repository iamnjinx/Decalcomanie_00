using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObtainable
{
    string obtainSFX { get; }
    void OnObtained();
}

public class Obtainables : MonoBehaviour, IObtainable
{
    [SerializeField] private string _obtainSFX;
    public string obtainSFX => _obtainSFX;
    public virtual void OnObtained()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(obtainSFX);
        gameObject.SetActive(false);
    }
}
