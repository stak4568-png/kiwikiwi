using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DropZone : MonoBehaviour, IDropHandler
{
    public ZoneType zoneType;
    public int maxCards = 5;

    // 최적화: 도발 카드 캐싱
    private readonly HashSet<CardDisplay> _tauntCards = new HashSet<CardDisplay>();

    /// <summary>
    /// 도발 카드 등록 (카드 배치 시 호출)
    /// </summary>
    public void RegisterTaunt(CardDisplay card)
    {
        if (card != null && card.data != null && card.data.HasKeyword(Keyword.Taunt))
            _tauntCards.Add(card);
    }

    /// <summary>
    /// 도발 카드 해제 (카드 제거 시 호출)
    /// </summary>
    public void UnregisterTaunt(CardDisplay card)
    {
        _tauntCards.Remove(card);
    }

    public void OnDrop(PointerEventData eventData)
    {
        Draggable d = eventData.pointerDrag?.GetComponent<Draggable>();
        if (d == null) return;

        // [규칙 1] 적 필드로는 직접 드롭 불가
        if (zoneType == ZoneType.EnemyField) return;

        // [규칙 2] 필드 카드는 손패로 회수 불가
        if (zoneType == ZoneType.Hand && d.sourceZone == ZoneType.PlayerField)
        {
            Debug.Log("이미 소환된 카드는 회수할 수 없습니다.");
            return;
        }

        // [규칙 3] 손패 → 필드 소환 로직
        if (zoneType == ZoneType.PlayerField && d.sourceZone == ZoneType.Hand)
        {
            CardDisplay card = d.GetComponent<CardDisplay>();
            if (card == null || card.data == null) return;

            // 자리 부족 체크
            if (transform.childCount >= maxCards)
            {
                Debug.Log("필드가 가득 찼습니다.");
                return;
            }

            // ★ 수정 포인트: manaCost -> mana ★
            if (!GameManager.instance.TrySpendMana(card.data.mana))
            {
                Debug.Log("마나가 부족합니다.");
                return;
            }

            // 소환 성공!
            d.parentToReturnTo = this.transform;

            // ★ 수정 포인트: cardName -> title ★
            Debug.Log($"{card.data.title} 소환!");

            // 소환 시 효과 발동 (EffectManager 연동)
            if (EffectManager.instance != null)
            {
                // 소환 후 약간의 딜레이를 주어 배치가 끝난 뒤 효과가 발동되게 합니다.
                StartCoroutine(TriggerSummonEffect(card));
            }
        }
        else
        {
            // 단순 구역 이동 (손패 내 정렬 등)
            d.parentToReturnTo = this.transform;
        }
    }

    private System.Collections.IEnumerator TriggerSummonEffect(CardDisplay card)
    {
        yield return new WaitForSeconds(0.1f);
        EffectManager.instance.TriggerEffects(card, EffectTiming.OnSummon);
    }

    /// <summary>
    /// 필드에 도발 카드가 있는지 확인 (최적화: O(1) 조회)
    /// </summary>
    public bool HasTaunt()
    {
        // 최적화: 캐시된 HashSet 사용 (죽은 카드 정리)
        _tauntCards.RemoveWhere(card => card == null);
        return _tauntCards.Count > 0;
    }
}