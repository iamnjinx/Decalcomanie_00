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
    [SerializeField] private ParticleSystem _obtainParticlePrefab;
    public string obtainSFX => _obtainSFX;
    public virtual void OnObtained()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(obtainSFX);

        if (_obtainParticlePrefab != null)
            Instantiate(_obtainParticlePrefab, transform.position, transform.rotation);

        gameObject.SetActive(false);
    }

    public void ResetObtainable()
    {
        gameObject.SetActive(true);
    }
}
