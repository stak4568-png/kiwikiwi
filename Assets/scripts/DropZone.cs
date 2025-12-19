using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public ZoneType zoneType; // 인스펙터에서 설정 (Hand, PlayerField, EnemyField)
    public int maxCards = 5;  // 필드 최대 배치 수

    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag.GetComponent<Draggable>();
        if (d == null) return;

        // [규칙 1] 적 필드로는 내 카드를 던질 수 없음
        if (zoneType == ZoneType.EnemyField) return;

        // [규칙 2] 이미 필드에 나간 카드는 다시 손패로 가져올 수 없음
        if (zoneType == ZoneType.Hand && d.sourceZone == ZoneType.PlayerField)
        {
            Debug.Log("필드에 소환된 몬스터는 회수할 수 없습니다.");
            return;
        }

        // [규칙 3] 손패에서 필드로 낼 때만 마나 체크 및 소환 실행
        if (zoneType == ZoneType.PlayerField && d.sourceZone == ZoneType.Hand)
        {
            CardDisplay card = d.GetComponent<CardDisplay>();

            // 몬스터 소환 조건: 자리 있음 && 마나 충분
            if (this.transform.childCount < maxCards && GameManager.instance.TrySpendMana(card.cardData.manaCost))
            {
                d.parentToReturnTo = this.transform; // 필드로 소속 변경 허용
                Debug.Log($"{card.cardData.cardName} 소환!");
            }
            else
            {
                Debug.Log("소환 불가: 마나 부족 또는 필드 가득 참");
                // d.parentToReturnTo를 바꾸지 않으므로 자동으로 손패로 돌아감
            }
        }
        else
        {
            // 그 외의 경우 (손패 안에서 순서 바꾸기 등)는 자유롭게 허용
            d.parentToReturnTo = this.transform;
        }
    }
}