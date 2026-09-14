using System.Collections;
using System.Collections.Generic;
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

    // SetVisible용. 비활성 자식(Dino 스킨)까지 포함해 Init에서 한 번만 모아둔다.
    private readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();

    // 스테이지 로드 시 1회만 호출. 각 마커를 보드 상 해당 타일 위치로 옮기고 켠다.
    public void Init(Board board)
    {
        GetComponentsInChildren(true, renderers);

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

    // Platformer 모드에서 마커를 감출 때 쓴다. GameObject를 끄는 대신 렌더러만 끄는 이유:
    // 1) 인트로가 끝나길 기다리는 아래 코루틴이 끊겨 Animator가 영원히 꺼지지 않고,
    //    그러면 Paint 모드로 돌아올 때마다 인트로가 처음부터 다시 재생된다.
    // 2) Dino 인트로 클립 끝에 스킨을 입히는 Apply 애니메이션 이벤트가 있어서,
    //    중간에 끊으면 공룡 스킨이 나타나지 않는다.
    // 따라서 인트로는 숨어 있는 동안에도 그대로 끝까지 재생시킨다.
    public void SetVisible(bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = visible;
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

    // 인트로 애니메이션 재생이 끝나면 Animator를 꺼서, 이후 Paint<->Platformer를 몇 번 전환해도
    // 인트로가 딱 한 번만 재생되게 한다. 한 번 끈 Animator는 다시 켜지지 않는다.
    private IEnumerator DisableAnimatorAfterIntro(Animator animator)
    {
        yield return null;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        animator.enabled = false;
    }
}
