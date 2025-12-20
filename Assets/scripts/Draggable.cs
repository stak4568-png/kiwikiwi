using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parentToReturnTo = null;
    [HideInInspector] public ZoneType sourceZone;

    private CanvasGroup canvasGroup;
    private CardDisplay cardDisplay;

    // 최적화: 리스트 재사용 (GC 감소)
    private static readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    
    // 드래그 중인 카드 추적 (FieldSlotManager 하이라이트용)
    public static Draggable currentDragging = null;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        cardDisplay = GetComponent<CardDisplay>();
    }

    // 1. 드래그 시작
    public void OnBeginDrag(PointerEventData eventData)
    {
        // 행동 단계가 아니면 드래그 불가
        if (!GameManager.instance.CanPlayerAct()) return;
        
        // 드래그 시작 표시
        currentDragging = this;

        parentToReturnTo = this.transform.parent;
        
        // 구역 확인 (FieldSlotManager 또는 DropZone)
        sourceZone = ZoneType.Hand; // 기본값
        
        // FieldSlotManager의 슬롯인지 확인
        if (FieldSlotManager.instance != null)
        {
            Transform parent = parentToReturnTo;
            Transform[] playerSlots = FieldSlotManager.instance.playerSlots;
            Transform[] enemySlots = FieldSlotManager.instance.enemySlots;
            
            foreach (var slot in playerSlots)
            {
                if (slot == parent)
                {
                    sourceZone = ZoneType.PlayerField;
                    break;
                }
            }
            
            if (sourceZone == ZoneType.Hand)
            {
                foreach (var slot in enemySlots)
                {
                    if (slot == parent)
                    {
                        sourceZone = ZoneType.EnemyField;
                        break;
                    }
                }
            }
        }
        
        // FieldSlotManager로 확인되지 않으면 DropZone 확인 (하위 호환성)
        if (sourceZone == ZoneType.Hand)
        {
            DropZone dz = parentToReturnTo.GetComponent<DropZone>();
            if (dz != null) sourceZone = dz.zoneType;
        }

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
        // [B] 손패에 있는 카드
        else if (sourceZone == ZoneType.Hand)
        {
            // [B-1] 스펠 카드: 타겟팅 화살표 표시
            if (cardDisplay != null && cardDisplay.data != null && cardDisplay.data.IsSpell())
            {
                // 스펠 카드는 필드 소환 체크 없이 마나만 확인
                if (GameManager.instance != null && 
                    GameManager.instance.CanPlayerAct() && 
                    GameManager.instance.playerCurrentMana >= cardDisplay.data.mana)
                {
                    if (CombatArrow.instance != null)
                    {
                        CombatArrow.instance.Show(transform.position);
                    }
                    // 드래그 중에는 다른 UI 레이캐스트를 방해하지 않도록 설정
                    this.transform.SetParent(this.transform.parent.parent);
                    if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
                }
                else
                {
                    Debug.Log($"[스펠 카드] 마나가 부족하거나 사용할 수 없습니다. (필요: {cardDisplay.data.mana}, 보유: {GameManager.instance?.playerCurrentMana ?? 0})");
                    return; // 드래그 취소
                }
            }
            // [B-2] 일반 카드: 소환을 위한 드래그
            else
            {
                // 드래그 중에는 다른 UI 레이캐스트를 방해하지 않도록 설정
                this.transform.SetParent(this.transform.parent.parent);
                if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
            }
        }
    }

    // 2. 드래그 중
    public void OnDrag(PointerEventData eventData)
    {
        if (!GameManager.instance.CanPlayerAct()) return;

        // 손패 카드일 때만 마우스를 따라 움직임 (필드 카드와 스펠 카드는 화살표가 움직임)
        if (sourceZone == ZoneType.Hand)
        {
            // 스펠 카드가 아니면 카드가 마우스를 따라 움직임
            if (cardDisplay == null || cardDisplay.data == null || !cardDisplay.data.IsSpell())
            {
                this.transform.position = eventData.position;
            }
            // 스펠 카드는 카드는 그대로 두고 화살표만 움직임
        }
    }

    // 3. 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!GameManager.instance.CanPlayerAct()) return;

        // [A] 필드 공격 드래그 종료
        if (sourceZone == ZoneType.PlayerField)
        {
            if (CombatArrow.instance != null) CombatArrow.instance.Hide();

            // 타겟 체크 및 전투 실행
            CheckCombatTarget(eventData);
        }
        // [B] 손패 드래그 종료
        else if (sourceZone == ZoneType.Hand)
        {
            // [B-1] 스펠 카드: 타겟팅 처리 (우선 처리)
            if (cardDisplay != null && cardDisplay.data != null && cardDisplay.data.IsSpell())
            {
                if (CombatArrow.instance != null) CombatArrow.instance.Hide();
                
                // 스펠 카드 타겟팅 처리
                CheckSpellTarget(eventData);
                // 스펠 카드는 여기서 처리 완료, 슬롯 체크를 하지 않음
                return;
            }
            // [B-2] 일반 카드: 소환 처리
            else
            {
                // blocksRaycasts 복구
                if (canvasGroup != null) canvasGroup.blocksRaycasts = true;

                // 필드 영역에 드롭되었는지 확인
                bool isOverField = IsOverFieldArea(eventData.position);
                
                if (isOverField)
                {
                    // 슬롯에 배치 시도
                    Transform targetSlot = CheckSlotDrop(eventData);
                    
                    if (targetSlot != null)
                    {
                        // 슬롯에 배치 성공
                        Debug.Log($"[Draggable] 슬롯에 배치 성공");
                    }
                    else
                    {
                        // 슬롯 배치 실패 - 원래 위치로 복귀
                        this.transform.SetParent(parentToReturnTo);
                        Debug.Log("[Draggable] 슬롯 배치 실패, 원래 위치로 복귀");
                    }
                }
                else
                {
                    // 필드 영역이 아니면 원래 위치로 복귀
                    this.transform.SetParent(parentToReturnTo);
                    Debug.Log("[Draggable] 필드 영역이 아니어서 원래 위치로 복귀");
                }

                // 드롭 후 비주얼 갱신 (모양 변경 등)
                if (cardDisplay != null) cardDisplay.UpdateVisual();
            }
        }
        else
        {
            // 기타 경우 원래 위치로 복귀
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            if (cardDisplay != null) cardDisplay.UpdateVisual();
        }
        
        // 드래그 종료 표시
        if (currentDragging == this)
        {
            currentDragging = null;
        }
    }

    /// <summary>
    /// 슬롯에 드롭되었는지 확인하고 처리 (간소화된 버전)
    /// </summary>
    Transform CheckSlotDrop(PointerEventData eventData)
    {
        if (cardDisplay == null || cardDisplay.data == null) return null;
        if (FieldSlotManager.instance == null) return null;

        // 마나 체크
        if (!GameManager.instance.TrySpendMana(cardDisplay.data.mana))
        {
            Debug.Log("[Draggable] 마나가 부족합니다.");
            return null;
        }

        // 필드가 가득 찼는지 확인
        if (FieldSlotManager.instance.IsFieldFull(true))
        {
            Debug.Log("[Draggable] 필드가 가득 찼습니다.");
            return null;
        }

        // 가장 가까운 빈 슬롯 찾기
        Transform targetSlot = FieldSlotManager.instance.GetNearestEmptySlot(eventData.position, true);
        
        if (targetSlot != null)
        {
            // 슬롯에 카드 배치
            if (FieldSlotManager.instance.PlaceCardInSlot(cardDisplay, targetSlot, true))
            {
                parentToReturnTo = targetSlot;
                
                // 비주얼 이펙트
                if (FieldVisualManager.instance != null)
                {
                    FieldVisualManager.instance.OnCardSummoned(targetSlot.position, true);
                }

                // 소환 효과 트리거
                if (EffectManager.instance != null)
                {
                    StartCoroutine(TriggerSummonEffect(cardDisplay));
                }

                return targetSlot;
            }
        }

        return null;
    }

    /// <summary>
    /// 소환 효과 트리거
    /// </summary>
    System.Collections.IEnumerator TriggerSummonEffect(CardDisplay card)
    {
        yield return new WaitForSeconds(0.1f);
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnSummon);
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

        // 서로에게 데미지 교환 (공격자 정보 전달)
        int attackerDmg = attacker.currentAttack;
        int defenderDmg = defender.currentAttack;

        defender.TakeDamage(attackerDmg, attacker);  // 공격자 정보 전달
        attacker.TakeDamage(defenderDmg, defender);  // 방어자 정보 전달
    }

    // --- 스펠 카드 타겟팅 로직 ---

    void CheckSpellTarget(PointerEventData eventData)
    {
        if (cardDisplay == null || cardDisplay.data == null || !cardDisplay.data.IsSpell())
        {
            Debug.LogWarning("[스펠 카드] cardDisplay 또는 data가 null이거나 스펠 카드가 아닙니다.");
            return;
        }

        Debug.Log($"[스펠 카드] {cardDisplay.data.title} 타겟 확인 시작");

        // 마우스 아래의 모든 UI 검사
        _raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, _raycastResults);

        CardDisplay targetCard = null;
        HeroPortrait targetHero = null;

        foreach (RaycastResult result in _raycastResults)
        {
            // 1. 적 영웅 타겟팅
            HeroPortrait hero = result.gameObject.GetComponentInParent<HeroPortrait>();
            if (hero != null && !hero.isPlayerHero)
            {
                targetHero = hero;
                Debug.Log($"[스펠 카드] 적 영웅 타겟 발견: {hero.heroData.title}");
                break;
            }

            // 2. 카드 타겟팅
            CardDisplay card = result.gameObject.GetComponentInParent<CardDisplay>();
            if (card != null && card != cardDisplay)
            {
                // 필드에 있는 카드만 타겟으로 선택 가능
                bool isOnField = false;
                
                // FieldSlotManager로 확인
                if (FieldSlotManager.instance != null)
                {
                    Transform parent = card.transform.parent;
                    Transform[] playerSlots = FieldSlotManager.instance.playerSlots;
                    Transform[] enemySlots = FieldSlotManager.instance.enemySlots;
                    
                    foreach (var slot in playerSlots)
                    {
                        if (slot == parent)
                        {
                            isOnField = true;
                            break;
                        }
                    }
                    if (!isOnField)
                    {
                        foreach (var slot in enemySlots)
                        {
                            if (slot == parent)
                            {
                                isOnField = true;
                                break;
                            }
                        }
                    }
                }
                
                // DropZone 확인 (하위 호환성)
                if (!isOnField)
                {
                    DropZone dz = card.GetComponentInParent<DropZone>();
                    isOnField = (dz != null && (dz.zoneType == ZoneType.PlayerField || dz.zoneType == ZoneType.EnemyField));
                }
                
                if (isOnField)
                {
                    targetCard = card;
                    Debug.Log($"[스펠 카드] 타겟 카드 발견: {card.data.title} ({(card.isMine ? "아군" : "적")})");
                    break;
                }
            }
        }

        // 효과 발동
        if (EffectManager.instance == null)
        {
            // EffectManager가 없으면 자동 생성 시도
            EffectManager manager = FindFirstObjectByType<EffectManager>();
            if (manager == null)
            {
                GameObject managerObj = new GameObject("EffectManager");
                manager = managerObj.AddComponent<EffectManager>();
                Debug.Log("[스펠 카드] EffectManager를 자동 생성했습니다.");
            }
            
            if (EffectManager.instance == null)
            {
                Debug.LogError("[스펠 카드] EffectManager.instance가 여전히 null입니다! 씬에 EffectManager를 추가해주세요.");
                this.transform.SetParent(parentToReturnTo);
                if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
                return;
            }
        }

        if (cardDisplay.data.effects == null || cardDisplay.data.effects.Count == 0)
        {
            Debug.LogWarning($"[스펠 카드] {cardDisplay.data.title}에 효과가 없습니다.");
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            return;
        }

        bool effectTriggered = false;
        
        foreach (var effect in cardDisplay.data.effects)
        {
            if (effect == null)
            {
                Debug.LogWarning("[스펠 카드] 효과가 null입니다.");
                continue;
            }

            if (effect.timing != EffectTiming.Manual)
            {
                Debug.Log($"[스펠 카드] 효과 타이밍이 Manual이 아닙니다: {effect.timing}");
                continue;
            }

            Debug.Log($"[스펠 카드] 효과 처리: {effect.effectName}, TargetType: {effect.targetType}");

            // 타겟이 필요한 효과인지 확인
            bool needsTarget = (effect.targetType == EffectTarget.SingleEnemy || 
                               effect.targetType == EffectTarget.SingleAlly);
            
            if (needsTarget)
            {
                // 타겟 유효성 검사
                if (effect.targetType == EffectTarget.SingleEnemy)
                {
                    // 영웅 타겟팅 우선
                    if (targetHero != null && effect is DamageEffect damageEffect && damageEffect.canTargetHero)
                    {
                        Debug.Log("[스펠 카드] 영웅 타겟팅 시도");
                        // 마나 소모
                        if (GameManager.instance.TrySpendMana(cardDisplay.data.mana))
                        {
                            EffectContext context = new EffectContext(cardDisplay, EffectTiming.Manual);
                            context.targetHero = targetHero;
                            effect.Execute(context);
                            effectTriggered = true;
                            Debug.Log("[스펠 카드] 영웅 타겟팅 성공!");
                        }
                        else
                        {
                            Debug.LogWarning("[스펠 카드] 마나가 부족합니다.");
                        }
                    }
                    // 카드 타겟팅
                    else if (targetCard != null && !targetCard.isMine)
                    {
                        Debug.Log($"[스펠 카드] 적 카드 타겟팅 시도: {targetCard.data.title}");
                        // 마나 소모
                        if (GameManager.instance.TrySpendMana(cardDisplay.data.mana))
                        {
                            EffectManager.instance.ExecuteEffect(effect, cardDisplay, targetCard);
                            effectTriggered = true;
                            Debug.Log("[스펠 카드] 적 카드 타겟팅 성공!");
                        }
                        else
                        {
                            Debug.LogWarning("[스펠 카드] 마나가 부족합니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[스펠 카드] 유효한 적 타겟이 없습니다. (targetHero: {targetHero != null}, targetCard: {targetCard != null})");
                    }
                }
                else if (effect.targetType == EffectTarget.SingleAlly)
                {
                    if (targetCard != null && targetCard.isMine)
                    {
                        Debug.Log($"[스펠 카드] 아군 카드 타겟팅 시도: {targetCard.data.title}");
                        // 마나 소모
                        if (GameManager.instance.TrySpendMana(cardDisplay.data.mana))
                        {
                            EffectManager.instance.ExecuteEffect(effect, cardDisplay, targetCard);
                            effectTriggered = true;
                            Debug.Log("[스펠 카드] 아군 카드 타겟팅 성공!");
                        }
                        else
                        {
                            Debug.LogWarning("[스펠 카드] 마나가 부족합니다.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[스펠 카드] 유효한 아군 타겟이 없습니다.");
                    }
                }
            }
            else
            {
                // 타겟이 필요 없는 효과 (AllEnemies, RandomEnemy 등)
                Debug.Log("[스펠 카드] 타겟 불필요 효과 발동");
                if (GameManager.instance.TrySpendMana(cardDisplay.data.mana))
                {
                    EffectManager.instance.ExecuteEffect(effect, cardDisplay, null);
                    effectTriggered = true;
                    Debug.Log("[스펠 카드] 효과 발동 성공!");
                }
                else
                {
                    Debug.LogWarning("[스펠 카드] 마나가 부족합니다.");
                }
            }
        }

        // 효과가 발동되었으면 카드 제거
        if (effectTriggered)
        {
            Debug.Log($"[스펠 카드] {cardDisplay.data.title} 효과 발동 완료, 카드 제거 예정");
            StartCoroutine(RemoveSpellCardAfterEffect());
        }
        else
        {
            // 타겟이 없거나 유효하지 않으면 원래 위치로 복귀
            this.transform.SetParent(parentToReturnTo);
            if (canvasGroup != null) canvasGroup.blocksRaycasts = true;
            Debug.LogWarning("[스펠 카드] 효과가 발동되지 않아 원래 위치로 복귀");
        }
    }

    IEnumerator RemoveSpellCardAfterEffect()
    {
        // 효과 발동 후 약간의 딜레이
        yield return new WaitForSeconds(0.2f);
        
        // 손패에서 제거
        if (cardDisplay != null)
        {
            Debug.Log($"[스펠 카드] {cardDisplay.data.title} 사용 완료, 제거");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 마우스가 필드 영역 위에 있는지 확인
    /// </summary>
    bool IsOverFieldArea(Vector2 screenPosition)
    {
        if (GameManager.instance == null || GameManager.instance.playerField == null)
            return false;

        RectTransform fieldRect = GameManager.instance.playerField.GetComponent<RectTransform>();
        if (fieldRect == null) return false;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        Vector2 localPoint;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            fieldRect, screenPosition, uiCamera, out localPoint);
    }
}