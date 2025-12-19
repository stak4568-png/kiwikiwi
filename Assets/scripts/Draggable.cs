using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parentToReturnTo = null;
    [HideInInspector] public ZoneType sourceZone;
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // 1. 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 현재 내가 어디 소속인지 확인
        parentToReturnTo = this.transform.parent;
        DropZone dz = parentToReturnTo.GetComponent<DropZone>();
        if (dz != null) sourceZone = dz.zoneType;

        // [A] 필드에 있는 카드라면: 공격 화살표 모드
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null)
            {
                CombatArrow.instance.Show(transform.position);
            }
        }
        // [B] 손패에 있는 카드라면: 카드 소환 이동 모드
        else if (sourceZone == ZoneType.Hand)
        {
            this.transform.SetParent(this.transform.parent.parent);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

    // 2. 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        // 손패일 때만 카드 이미지가 마우스를 따라다님
        if (sourceZone == ZoneType.Hand)
        {
            this.transform.position = eventData.position;
        }
        // 화살표 모드일 때는 CombatArrow 스크립트가 스스로 Update에서 마우스를 쫓아갑니다.
    }

    // 3. 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        // [A] 필드 공격 모드 종료
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null) CombatArrow.instance.Hide();

            // 마우스를 놓은 지점에 적이 있는지 확인
            CheckCombatTarget(eventData);
        }
        // [B] 손패 소환 모드 종료
        else
        {
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            // 비주얼 업데이트 (Hand -> Field 변신)
            CardDisplay cd = GetComponent<CardDisplay>();
            if (cd != null)
            {
                cd.UpdateSourceZone();
                cd.UpdateCardUI();
            }
        }
    }

    // --- 전투 타겟팅 로직 ---

    void CheckCombatTarget(PointerEventData eventData)
    {
        // 마우스 위치 아래에 있는 모든 UI를 레이캐스트로 검사
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // 카드 컴포넌트가 있는지 확인
            CardDisplay targetCard = result.gameObject.GetComponentInParent<CardDisplay>();

            if (targetCard != null)
            {
                // 그 카드가 적 필드(EnemyField)에 있는지 확인
                DropZone targetZone = targetCard.transform.parent.GetComponent<DropZone>();
                if (targetZone != null && targetZone.zoneType == ZoneType.EnemyField)
                {
                    // 전투 실행!
                    ExecuteCombat(GetComponent<CardDisplay>(), targetCard);
                    break; // 타겟을 찾았으니 검사 중단
                }
            }
        }
    }

    void ExecuteCombat(CardDisplay attacker, CardDisplay defender)
    {
        // 공격자와 방어자 모두 몬스터 데이터여야 전투 가능
        if (attacker.cardData is MonsterCardData atkData && defender.cardData is MonsterCardData defData)
        {
            Debug.Log($"<color=red>{attacker.cardData.cardName}</color>이(가) <color=blue>{defender.cardData.cardName}</color>을(를) 공격합니다!");

            // 1. 방어자에게 공격자의 공격력만큼 데미지
            defender.TakeDamage(attacker.currentAttack);

            // 2. 공격자에게 방어자의 공격력만큼 데미지 (반격)
            attacker.TakeDamage(defender.currentAttack);
        }
    }
}