using UnityEngine;
using UnityEngine.EventSystems;

public class MouseBlockUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MouseUI mouseUI;

    void Start()
    {
        mouseUI = FindAnyObjectByType<MouseUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (mouseUI == null) return;
        mouseUI.SetBlocked(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (mouseUI == null) return;
        mouseUI.SetBlocked(false);
    }
}
