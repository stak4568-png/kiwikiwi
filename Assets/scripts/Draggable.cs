using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parentToReturnTo = null;
    [HideInInspector] public ZoneType sourceZone;

    private CanvasGroup canvasGroup;
    private CardDisplay cardDisplay;

    // 최적화: 리스트 재사용 (GC 감소)
    private static readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        cardDisplay = GetComponent<CardDisplay>();
    }

    // 1. 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 내 턴이 아니면 드래그 불가
        if (GameManager.instance.isEnemyTurn) return;

        parentToReturnTo = this.transform.parent;
        DropZone dz = parentToReturnTo.GetComponent<DropZone>();
        if (dz != null) sourceZone = dz.zoneType;

        // [A] 필드에 있는 아군 카드: 공격 화살표 표시
        if (sourceZone == ZoneType.PlayerField)
        {
            if (cardDisplay != null && cardDisplay.CanAttackNow() && cardDisplay.isMine)
            {
                if (CombatArrow.instance != null)
                {
                    CombatArrow.instance.Show(transform.position);
                }
            }
        }
        // [B] 손패에 있는 카드: 소환을 위한 드래그
        else if (sourceZone == ZoneType.Hand)
        {
            // 드래그 중에는 다른 UI 레이캐스트를 방해하지 않도록 설정
            this.transform.SetParent(this.transform.parent.parent);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

    // 2. 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (GameManager.instance.isEnemyTurn) return;

        // 손패 카드일 때만 마우스를 따라 움직임 (필드 카드는 화살표가 움직임)
        if (sourceZone == ZoneType.Hand)
        {
            this.transform.position = eventData.position;
        }
    }

    // 3. 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (GameManager.instance.isEnemyTurn) return;

        // [A] 필드 공격 드래그 종료
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null) CombatArrow.instance.Hide();

            // 타겟 체크 및 전투 실행
            CheckCombatTarget(eventData);
        }
        // [B] 손패 소환 드래그 종료
        else
        {
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            // 드롭 후 비주얼 갱신 (모양 변경 등)
            if (cardDisplay != null) cardDisplay.UpdateVisual();
        }
    }

    // --- 전투 타겟팅 로직 ---

    void CheckCombatTarget(PointerEventData eventData)
    {
        if (cardDisplay == null || !cardDisplay.CanAttackNow()) return;

        // 마우스 아래의 모든 UI 검사 (최적화: 리스트 재사용)
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);

        foreach (RaycastResult result in _raycastResults)
        {
            // 1. 적 영웅 타겟팅
            HeroPortrait targetHero = result.gameObject.GetComponentInParent<HeroPortrait>();
            if (targetHero != null && !targetHero.isPlayerHero)
            {
                // 도발 하수인 체크
                if (HasTauntOnEnemyField())
                {
                    Debug.Log("도발 하수인이 앞을 가로막고 있습니다!");
                    return;
                }

                ExecuteHeroAttack(cardDisplay, targetHero);
                return;
            }

            // 2. 적 하수인 타겟팅
            CardDisplay targetCard = result.gameObject.GetComponentInParent<CardDisplay>();
            if (targetCard != null && !targetCard.isMine)
            {
                // 타겟이 도발이 아닌데 적 필드에 다른 도발이 있다면 차단
                if (!targetCard.data.HasKeyword(Keyword.Taunt) && HasTauntOnEnemyField())
                {
                    Debug.Log("도발 하수인을 먼저 공격해야 합니다!");
                    return;
                }

                ExecuteCombat(cardDisplay, targetCard);
                return;
            }
        }
    }

    bool HasTauntOnEnemyField()
    {
        if (GameManager.instance.enemyField == null) return false;
        DropZone enemyZone = GameManager.instance.enemyField.GetComponent<DropZone>();
        return enemyZone != null && enemyZone.HasTaunt();
    }

    void ExecuteHeroAttack(CardDisplay attacker, HeroPortrait targetHero)
    {
        Debug.Log($"<color=orange>{attacker.data.title}</color> -> <color=red>{targetHero.heroData.title}</color> 공격!");

        attacker.OnAttack(null);
        targetHero.TakeDamage(attacker.currentAttack);

        attacker.UpdateVisual();
    }

    void ExecuteCombat(CardDisplay attacker, CardDisplay defender)
    {
        Debug.Log($"<color=orange>{attacker.data.title}</color> vs <color=yellow>{defender.data.title}</color> 전투!");

        attacker.OnAttack(defender);

        // 서로에게 데미지 교환
        int attackerDmg = attacker.currentAttack;
        int defenderDmg = defender.currentAttack;

        defender.TakeDamage(attackerDmg);
        attacker.TakeDamage(defenderDmg);
    }
}