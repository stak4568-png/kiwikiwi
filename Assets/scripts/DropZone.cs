// DropZone.cs
// 카드 드롭 영역 - 효과 시스템 통합 버전

using UnityEngine;
using UnityEngine.EventSystems;

public class DropZone : MonoBehaviour, IDropHandler
{
    public ZoneType zoneType;
    public int maxCards = 5;

    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag?.GetComponent<Draggable>();
        if (d == null) return;

        // [규칙 1] 적 필드로는 드롭 불가
        if (zoneType == ZoneType.EnemyField) return;

        // [규칙 2] 필드 카드는 손패로 회수 불가
        if (zoneType == ZoneType.Hand && d.sourceZone == ZoneType.PlayerField)
        {
            Debug.Log("필드에 소환된 몬스터는 회수할 수 없습니다.");
            return;
        }

        // [규칙 3] 손패 → 필드 소환
        if (zoneType == ZoneType.PlayerField && d.sourceZone == ZoneType.Hand)
        {
            CardDisplay card = d.GetComponent<CardDisplay>();
            if (card == null) return;

            // 조건 체크: 자리 + 마나
            if (transform.childCount >= maxCards)
            {
                Debug.Log("소환 불가: 필드 가득 참");
                return;
            }

            if (!GameManager.instance.TrySpendMana(card.cardData.manaCost))
            {
                Debug.Log("소환 불가: 마나 부족");
                return;
            }

            // 소환 성공!
            d.parentToReturnTo = this.transform;
            Debug.Log($"{card.cardData.cardName} 소환!");

            // ★ 소환 효과 발동 ★
            // Draggable.OnEndDrag에서 UpdateSourceZone 후 호출되도록
            // 여기서는 플래그만 설정하고, 실제 효과는 CardDisplay에서 처리
            card.Invoke(nameof(card.OnSummoned), 0.1f);
        }
        else
        {
            // 손패 내 재정렬 등
            d.parentToReturnTo = this.transform;
        }
    }

    /// <summary>
    /// 도발 카드가 있는지 확인
    /// </summary>
    public bool HasTaunt()
    {
        CardDisplay[] cards = GetComponentsInChildren<CardDisplay>();
        foreach (var card in cards)
        {
            if (card.HasKeyword(Keyword.Taunt))
                return true;
        }
        return false;
    }

    /// <summary>
    /// 은신이 아닌 카드만 반환 (타겟팅용)
    /// </summary>
    public CardDisplay[] GetTargetableCards()
    {
        var cards = GetComponentsInChildren<CardDisplay>();
        return System.Array.FindAll(cards, c => !c.isStealthed && !c.HasKeyword(Keyword.Immune));
    }
}
