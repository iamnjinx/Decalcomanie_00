using System.Collections;
using UnityEngine;

// Paint 모드에서만 보이는, Star/Door/Player/Key 위치 표시용 마커.
// 실제 게임플레이 오브젝트(Obtainables/DoorController/PlayerControllerD)는 Platformer 모드에서만 쓰이고,
// Paint 모드에서는 이 마커들이 대신 위치를 알려준다.
public class PaintObjectMarkers : MonoBehaviour
{
    [SerializeField] private Transform starIcon;
    [SerializeField] private Transform doorIcon;
    [SerializeField] private Transform playerIcon;
    [SerializeField] private Transform keyIcon;

    // 스테이지 로드 시 1회만 호출. 각 마커를 보드 상 해당 타일 위치로 옮기고 켠다.
    public void Init(Board board)
    {
        for (int i = 0; i < board.allTiles.Length; i++)
        {
            TileType type = board.allTiles[i].type;
            Vector3 pos = board.GetWorldPosition(i);

            if (type == TileType.Star)       PlaceIcon(starIcon, pos);
            else if (type == TileType.Key)   PlaceIcon(keyIcon, pos);
            else if (type == TileType.End)   PlaceIcon(doorIcon, pos);
            else if (type == TileType.Start) PlaceIcon(playerIcon, pos);
        }
    }

    private void PlaceIcon(Transform icon, Vector3 pos)
    {
        icon.position = pos;
        icon.gameObject.SetActive(true);

        Animator animator = icon.GetComponent<Animator>();
        if (animator != null)
            StartCoroutine(DisableAnimatorAfterIntro(animator));
    }

    // 인트로 애니메이션 재생이 끝나면 Animator를 꺼서, 이후 Paint<->Platformer 전환으로
    // 이 오브젝트가 다시 SetActive(true)되어도 애니메이션이 처음부터 다시 재생되지 않게 한다.
    private IEnumerator DisableAnimatorAfterIntro(Animator animator)
    {
        yield return null;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        animator.enabled = false;
    }
}
