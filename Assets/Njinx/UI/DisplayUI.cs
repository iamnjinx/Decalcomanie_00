using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Njinx.UI
{
    public class InfoDisplayUI : BaseUI
    {
        [Header("Info Display UI")]
        public TextMeshProUGUI InfoName;
        public TextMeshProUGUI InfoDescription;

        public void DisplayOn(DisplayableInfo info)
        {
            ShowUI();

            InfoName.text = info.Name;
            InfoDescription.text = info.Description;
        }
        public void DisplayOff()
        {
            InfoName.text = "";
            InfoDescription.text = "";

            HideUI();
        }
    }

    [System.Serializable]
    public class DisplayableInfo
    {
        public string Name;
        public string Description;

        public DisplayableInfo(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public interface IDisplayable
    {
        public InfoDisplayUI displayUI { get; set; }

        public DisplayableInfo displayableInfo { get; set;}
        
        public void SetDisplayInfo(string name, string description)
        {
            displayableInfo = new DisplayableInfo(name, description);
            displayUI.DisplayOff();
        }
    }
}
