using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IObtainable
{
    void OnObtained();
}

public class Obtainables : MonoBehaviour, IObtainable
{
    public virtual void OnObtained()
    {
        gameObject.SetActive(false);
    }
}
