using UnityEngine;
using UnityEngine.EventSystems;

public class MouseBlockUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private MouseUI mouseUI;

    // GameManager 싱글턴 중복 인스턴스가 같은 프레임에 파괴되기 전에 캐싱될 수 있어
    // Start()에서 한 번만 캐싱하지 않고, 죽은 참조(파괴된 오브젝트)면 매번 다시 찾는다.
    private MouseUI MouseUIInstance
    {
        get
        {
            if (mouseUI == null) mouseUI = FindAnyObjectByType<MouseUI>();
            return mouseUI;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        MouseUI instance = MouseUIInstance;
        if (instance != null) instance.SetBlocked(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        MouseUI instance = MouseUIInstance;
        if (instance != null) instance.SetBlocked(false);
    }
}
