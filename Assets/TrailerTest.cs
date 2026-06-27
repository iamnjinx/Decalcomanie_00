using System.Collections;
using System.Collections.Generic;
using Njinx.UI;
using UnityEngine;

public class TrailerTest : MonoBehaviour
{
    public BaseUI switchButton;

    public void Update()
    {
        if (Input.GetKeyDown("l"))
        {
            switchButton.SetUI(!switchButton.is_shown, 1f);
        }
    }
}
