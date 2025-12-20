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

    // 1. �巡�� ����
    public void OnBeginDrag(PointerEventData eventData)
    {
        // ���� ���� ��� �Ҽ����� Ȯ��
        parentToReturnTo = this.transform.parent;
        DropZone dz = parentToReturnTo.GetComponent<DropZone>();
        if (dz != null) sourceZone = dz.zoneType;

        // [A] �ʵ忡 �ִ� ī����: ���� ȭ��ǥ ���
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null)
            {
                CombatArrow.instance.Show(transform.position);
            }
        }
        // [B] ���п� �ִ� ī����: ī�� ��ȯ �̵� ���
        else if (sourceZone == ZoneType.Hand)
        {
            this.transform.SetParent(this.transform.parent.parent);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
        }
    }

    // 2. �巡�� ��
    public void OnDrag(PointerEventData eventData)
    {
        // ������ ���� ī�� �̹����� ���콺�� ����ٴ�
        if (sourceZone == ZoneType.Hand)
        {
            this.transform.position = eventData.position;
        }
        // ȭ��ǥ ����� ���� CombatArrow ��ũ��Ʈ�� ������ Update���� ���콺�� �Ѿư��ϴ�.
    }

    // 3. �巡�� ����
    public void OnEndDrag(PointerEventData eventData)
    {
        // [A] �ʵ� ���� ��� ����
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null) CombatArrow.instance.Hide();

            // ���콺�� ���� ������ ���� �ִ��� Ȯ��
            CheckCombatTarget(eventData);
        }
        // [B] ���� ��ȯ ��� ����
        else
        {
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

            // ���־� ������Ʈ (Hand -> Field ����)
            CardDisplay cd = GetComponent<CardDisplay>();
            if (cd != null)
            {
                cd.UpdateSourceZone();
                cd.UpdateCardUI();
            }
        }
    }

    // --- 전투 타겟 체크 ---

    void CheckCombatTarget(PointerEventData eventData)
    {
        CardDisplay attacker = GetComponent<CardDisplay>();
        if (attacker == null) return;

        // 공격 가능 체크
        if (!attacker.CanAttackNow())
        {
            Debug.Log("이 하수인은 지금 공격할 수 없습니다!");
            return;
        }

        // 마우스 위치 아래의 모든 UI를 레이캐스트로 검사
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            // 1. 적 영웅 초상화 체크
            HeroPortrait targetHero = result.gameObject.GetComponentInParent<HeroPortrait>();
            if (targetHero != null && !targetHero.isPlayerHero)
            {
                // 도발 체크
                if (HasTauntOnEnemyField())
                {
                    Debug.Log("도발 하수인을 먼저 처치해야 합니다!");
                    return;
                }

                // 영웅 공격 실행
                ExecuteHeroAttack(attacker, targetHero);
                return;
            }

            // 2. 적 카드 체크
            CardDisplay targetCard = result.gameObject.GetComponentInParent<CardDisplay>();
            if (targetCard != null)
            {
                // 이 카드가 적 필드(EnemyField)에 있는지 확인
                DropZone targetZone = targetCard.transform.parent.GetComponent<DropZone>();
                if (targetZone != null && targetZone.zoneType == ZoneType.EnemyField)
                {
                    // 도발 체크 (타겟이 도발이 아닌데 도발 하수인이 있으면 차단)
                    if (!targetCard.HasKeyword(Keyword.Taunt) && HasTauntOnEnemyField())
                    {
                        Debug.Log("도발 하수인을 먼저 처치해야 합니다!");
                        return;
                    }

                    // 은신 체크
                    if (targetCard.isStealthed)
                    {
                        Debug.Log("은신 상태인 하수인은 공격할 수 없습니다!");
                        return;
                    }

                    // 전투 실행!
                    ExecuteCombat(attacker, targetCard);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 적 필드에 도발 하수인이 있는지 확인
    /// </summary>
    bool HasTauntOnEnemyField()
    {
        if (GameManager.instance == null || GameManager.instance.enemyField == null)
            return false;

        DropZone enemyZone = GameManager.instance.enemyField.GetComponent<DropZone>();
        return enemyZone != null && enemyZone.HasTaunt();
    }

    /// <summary>
    /// 하수인이 적 영웅 공격
    /// </summary>
    void ExecuteHeroAttack(CardDisplay attacker, HeroPortrait targetHero)
    {
        if (attacker.cardData is MonsterCardData atkData)
        {
            Debug.Log($"<color=red>{attacker.cardData.cardName}</color>이(가) <color=blue>{targetHero.heroData.heroName}</color>을(를) 공격!");

            // 공격 처리
            attacker.OnAttack(null);

            // 영웅에게 데미지
            targetHero.TakeDamage(attacker.currentAttack);

            attacker.UpdateCardUI();
        }
    }

    /// <summary>
    /// 하수인 간 전투
    /// </summary>
    void ExecuteCombat(CardDisplay attacker, CardDisplay defender)
    {
        if (attacker.cardData is MonsterCardData atkData && defender.cardData is MonsterCardData defData)
        {
            Debug.Log($"<color=red>{attacker.cardData.cardName}</color>이(가) <color=blue>{defender.cardData.cardName}</color>을(를) 공격!");

            // 공격 처리
            attacker.OnAttack(defender);

            // 1. 방어자에게 공격자의 공격력만큼 데미지
            defender.TakeDamage(attacker.currentAttack);

            // 2. 공격자에게 방어자의 공격력만큼 데미지 (반격)
            attacker.TakeDamage(defender.currentAttack);
        }
    }
}