using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class EffectManager : MonoBehaviour
{
    public static EffectManager instance;

    private bool isWaitingForTarget = false;
    private CardEffect pendingEffect;
    private EffectContext pendingContext;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
    
    void Update()
    {
        // ESC 키로 타겟 선택 취소
        if (isWaitingForTarget)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CancelTargetSelection();
                Debug.Log("[타겟 선택] 취소되었습니다.");
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelTargetSelection();
                Debug.Log("[타겟 선택] 취소되었습니다.");
            }
        }
    }

    /// <summary>
    /// 특정 타이밍에 해당하는 카드의 모든 효과 발동
    /// </summary>
    public void TriggerEffects(CardDisplay card, EffectTiming timing, CardDisplay target = null)
    {
        // ★ 수정 포인트: cardData -> data ★
        if (card == null || card.data == null) return;

        // CardData 에 정의된 효과 리스트를 순회
        // (CardData.cs에 public List<CardEffect> effects 변수가 있어야 함)
        if (card.data.effects != null)
        {
            foreach (CardEffect effect in card.data.effects)
            {
                if (effect != null && effect.timing == timing)
                {
                    ExecuteEffect(effect, card, target);
                }
            }
        }
    }

    public void ExecuteEffect(CardEffect effect, CardDisplay source, CardDisplay target = null)
    {
        if (effect == null) return;

        EffectContext context = new EffectContext(source, effect.timing);
        context.targetCard = target;

        // 타겟 선택이 필요한지 체크
        if (NeedsTargetSelection(effect.targetType) && target == null)
        {
            StartTargetSelection(effect, context);
            return;
        }

        effect.Execute(context);
    }

    bool NeedsTargetSelection(EffectTarget targetType)
    {
        return targetType == EffectTarget.SingleEnemy || targetType == EffectTarget.SingleAlly;
    }

    void StartTargetSelection(CardEffect effect, EffectContext context)
    {
        isWaitingForTarget = true;
        pendingEffect = effect;
        pendingContext = context;
        Debug.Log($"[타겟 선택] {effect.effectName}의 대상을 선택하세요. (Target Type: {effect.targetType})");

        // 타겟 선택 모드 시작 - 가능한 타겟 하이라이트
        HighlightValidTargets(effect.targetType);

        // 디버그: 하이라이트된 카드 수 확인 (최적화: FieldSlotManager 사용)
        int highlightedCount = 0;
        foreach (var card in GetFieldCards())
        {
            if (IsValidTarget(card, effect.targetType))
                highlightedCount++;
        }
        Debug.Log($"[타겟 선택] {highlightedCount}개의 가능한 타겟이 하이라이트되었습니다.");
    }
    
    /// <summary>
    /// 타겟 선택 모드에서 가능한 타겟을 하이라이트
    /// </summary>
    void HighlightValidTargets(EffectTarget targetType)
    {
        // 최적화: FieldSlotManager에서 필드 카드만 가져옴 (FindObjectsByType 제거)
        var fieldCards = GetFieldCards();

        // 모든 필드 카드의 하이라이트 초기화
        foreach (var card in fieldCards)
        {
            SetCardHighlight(card, false);
        }

        // 가능한 타겟만 하이라이트
        foreach (var card in fieldCards)
        {
            bool isValid = false;

            if (targetType == EffectTarget.SingleEnemy)
            {
                isValid = !card.isMine; // 적 카드만
            }
            else if (targetType == EffectTarget.SingleAlly)
            {
                isValid = card.isMine; // 아군 카드만
            }

            if (isValid)
            {
                SetCardHighlight(card, true);
                Debug.Log($"[타겟 선택] 가능한 타겟: {card.data?.title} ({(card.isMine ? "아군" : "적")})");
            }
        }
    }
    
    /// <summary>
    /// 카드가 필드에 있는지 확인
    /// </summary>
    bool IsCardOnField(CardDisplay card)
    {
        if (card == null) return false;
        
        // FieldSlotManager로 확인
        if (FieldSlotManager.instance != null)
        {
            Transform parent = card.transform.parent;
            Transform[] playerSlots = FieldSlotManager.instance.playerSlots;
            Transform[] enemySlots = FieldSlotManager.instance.enemySlots;
            
            foreach (var slot in playerSlots)
            {
                if (slot == parent) return true;
            }
            foreach (var slot in enemySlots)
            {
                if (slot == parent) return true;
            }
        }
        
        // DropZone에 있는 경우 (하위 호환성)
        DropZone zone = card.GetComponentInParent<DropZone>();
        if (zone != null)
        {
            return zone.zoneType == ZoneType.PlayerField || zone.zoneType == ZoneType.EnemyField;
        }
        
        return false;
    }
    
    // 하이라이트된 카드의 원래 스케일 저장
    private Dictionary<CardDisplay, Vector3> _originalScales = new Dictionary<CardDisplay, Vector3>();
    
    /// <summary>
    /// 카드 하이라이트 설정
    /// </summary>
    void SetCardHighlight(CardDisplay card, bool highlight)
    {
        if (card == null) return;
        
        // 카드가 필드에 있는지 손패에 있는지 확인
        bool isOnBoard = IsCardOnField(card);
        
        Image targetImage = null;
        
        if (isOnBoard)
        {
            // 필드에 있는 경우 boardArt 사용
            targetImage = card.boardArt;
        }
        else
        {
            // 손패에 있는 경우 handArt 사용
            targetImage = card.handArt;
        }
        
        // Image를 찾지 못한 경우 GameObject의 Image 컴포넌트 시도
        if (targetImage == null)
        {
            targetImage = card.GetComponent<Image>();
        }
        
        // 하이라이트 적용
        if (targetImage != null)
        {
            if (highlight)
            {
                // 원래 스케일 저장
                if (!_originalScales.ContainsKey(card))
                {
                    _originalScales[card] = card.transform.localScale;
                }
                
                // 하이라이트: 밝은 빨간색
                targetImage.color = new Color(1f, 0.5f, 0.5f, 1f); // 더 진한 빨간색
                
                // 스케일 약간 키우기
                card.transform.localScale = _originalScales[card] * 1.15f;
            }
            else
            {
                // 하이라이트 제거
                targetImage.color = Color.white;
                
                // 스케일 복원
                if (_originalScales.ContainsKey(card))
                {
                    card.transform.localScale = _originalScales[card];
                    _originalScales.Remove(card);
                }
                else
                {
                    // 원래 스케일이 저장되지 않았으면 기본값으로
                    card.transform.localScale = Vector3.one;
                }
            }
        }
        else
        {
            Debug.LogWarning($"[EffectManager] {card.data?.title}의 Image를 찾을 수 없습니다. (OnBoard: {isOnBoard})");
        }
    }

    public void OnTargetSelected(CardDisplay target)
    {
        if (!isWaitingForTarget || pendingEffect == null) return;
        
        // 타겟 유효성 검사
        if (!IsValidTarget(target, pendingEffect.targetType))
        {
            Debug.LogWarning($"[타겟 선택] {target.data.title}은(는) 유효한 타겟이 아닙니다.");
            return;
        }
        
        pendingContext.targetCard = target;
        pendingEffect.Execute(pendingContext);
        CancelTargetSelection();
    }
    
    /// <summary>
    /// 타겟이 유효한지 검사
    /// </summary>
    bool IsValidTarget(CardDisplay target, EffectTarget targetType)
    {
        if (target == null) return false;
        
        // 필드에 있는 카드만 타겟으로 선택 가능
        if (!IsCardOnField(target))
        {
            Debug.LogWarning($"[타겟 선택] {target.data?.title}은(는) 필드에 없습니다.");
            return false;
        }
        
        if (targetType == EffectTarget.SingleEnemy)
        {
            return !target.isMine; // 적 카드만
        }
        else if (targetType == EffectTarget.SingleAlly)
        {
            return target.isMine; // 아군 카드만
        }
        
        return false;
    }

    public void OnHeroTargetSelected(HeroPortrait targetHero)
    {
        if (!isWaitingForTarget || pendingEffect == null) return;
        pendingContext.targetHero = targetHero;
        pendingEffect.Execute(pendingContext);
        CancelTargetSelection();
    }

    public void CancelTargetSelection()
    {
        // 하이라이트 제거 (최적화: FieldSlotManager 사용)
        foreach (var card in GetFieldCards())
        {
            SetCardHighlight(card, false);
        }

        // 원래 스케일 딕셔너리 초기화
        _originalScales.Clear();

        isWaitingForTarget = false;
        pendingEffect = null;
        pendingContext = null;

        Debug.Log("[타겟 선택] 취소됨");
    }

    public bool IsWaitingForTarget() => isWaitingForTarget;

    // 전역 타이밍 트리거 (턴 시작/종료 등)
    public void TriggerGlobalTiming(EffectTiming timing, ZoneType zone)
    {
        // 최적화: FieldSlotManager에서 직접 필드 카드 가져오기
        if (FieldSlotManager.instance == null) return;

        bool isPlayerZone = (zone == ZoneType.PlayerField);
        CardDisplay[] cards = FieldSlotManager.instance.GetAllCardsOnField(isPlayerZone);

        foreach (var card in cards)
        {
            if (card != null)
            {
                TriggerEffects(card, timing);
            }
        }
    }

    /// <summary>
    /// FieldSlotManager에서 모든 필드 카드를 가져오는 헬퍼 메서드 (성능 최적화)
    /// </summary>
    private List<CardDisplay> GetFieldCards()
    {
        List<CardDisplay> result = new List<CardDisplay>();
        if (FieldSlotManager.instance == null) return result;

        // 플레이어 필드 카드
        CardDisplay[] playerCards = FieldSlotManager.instance.GetAllCardsOnField(true);
        if (playerCards != null) result.AddRange(playerCards);

        // 적 필드 카드
        CardDisplay[] enemyCards = FieldSlotManager.instance.GetAllCardsOnField(false);
        if (enemyCards != null) result.AddRange(enemyCards);

        return result;
    }
}