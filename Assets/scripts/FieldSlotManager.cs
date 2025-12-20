using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 필드 슬롯 관리자
/// 필드에 미리 정의된 슬롯 위치를 관리하고, 카드를 배치합니다.
/// Slot 좌표 시스템을 사용하여 거리 계산 및 슬롯 관리 기능을 제공합니다.
/// </summary>
public class FieldSlotManager : MonoBehaviour
{
    public static FieldSlotManager instance;

    [Header("플레이어 필드 슬롯")]
    [Tooltip("플레이어 필드의 슬롯 위치들 (Inspector에서 미리 설정)")]
    public Transform[] playerSlots = new Transform[5];

    [Header("적 필드 슬롯")]
    [Tooltip("적 필드의 슬롯 위치들 (Inspector에서 미리 설정)")]
    public Transform[] enemySlots = new Transform[5];

    [Header("슬롯 하이라이트")]
    [Tooltip("드래그 중 슬롯 하이라이트 활성화")]
    public bool enableSlotHighlight = true;
    public Color highlightColor = new Color(1f, 1f, 0.5f, 0.5f);

    [Header("필드 카드 프리팹")]
    [Tooltip("필드에 소환되는 카드 프리팹 (DeckManager에서 가져오거나 여기서 설정)")]
    public GameObject fieldCardPrefab;

    // 슬롯 점유 상태 추적
    private CardDisplay[] _playerSlotCards;
    private CardDisplay[] _enemySlotCards;

    // Transform과 Slot 좌표 매핑
    private Dictionary<Transform, Slot> _transformToSlot = new Dictionary<Transform, Slot>();
    private Dictionary<Slot, Transform> _slotToTransform = new Dictionary<Slot, Transform>();
    private Dictionary<CardDisplay, Slot> _cardToSlot = new Dictionary<CardDisplay, Slot>();

    // 슬롯 하이라이트 컴포넌트 캐시
    private Dictionary<Transform, SlotHighlight> _slotHighlights = new Dictionary<Transform, SlotHighlight>();
    private Dictionary<Transform, bool> _slotIsPlayerSlot = new Dictionary<Transform, bool>(); // 슬롯이 플레이어 슬롯인지 캐싱
    
    // 드래그 중인 카드 추적
    private CardDisplay _draggingCard = null;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            _playerSlotCards = new CardDisplay[playerSlots.Length];
            _enemySlotCards = new CardDisplay[enemySlots.Length];
            InitializeSlotMapping();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Transform과 Slot 좌표 매핑 초기화
    /// </summary>
    void InitializeSlotMapping()
    {
        _transformToSlot.Clear();
        _slotToTransform.Clear();

        // 플레이어 슬롯 매핑 (p=0, x=1~5)
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] != null)
            {
                Slot slot = new Slot(i + 1, 1, 0); // x는 1부터 시작
                _transformToSlot[playerSlots[i]] = slot;
                _slotToTransform[slot] = playerSlots[i];
            }
        }

        // 적 슬롯 매핑 (p=1, x=1~5)
        for (int i = 0; i < enemySlots.Length; i++)
        {
            if (enemySlots[i] != null)
            {
                Slot slot = new Slot(i + 1, 1, 1); // x는 1부터 시작
                _transformToSlot[enemySlots[i]] = slot;
                _slotToTransform[slot] = enemySlots[i];
            }
        }
    }

    void Start()
    {
        // 슬롯 하이라이트 컴포넌트 초기화
        if (enableSlotHighlight)
        {
            InitializeSlotHighlights();
        }
    }

    void Update()
    {
        // 드래그 중인 카드 확인 및 하이라이트 업데이트
        // 최적화: 드래그 중일 때만 업데이트
        if (enableSlotHighlight && Draggable.currentDragging != null)
        {
            UpdateSlotHighlights();
        }
        else if (enableSlotHighlight)
        {
            // 드래그가 끝났을 때 하이라이트 제거 (한 번만)
            if (_draggingCard != null)
            {
                _draggingCard = null;
                // 모든 하이라이트 제거
                foreach (var kvp in _slotHighlights)
                {
                    if (kvp.Value != null)
                        kvp.Value.SetAlpha(0f);
                }
            }
        }
    }

    /// <summary>
    /// 슬롯 하이라이트 컴포넌트 초기화
    /// </summary>
    void InitializeSlotHighlights()
    {
        _slotHighlights.Clear();
        _slotIsPlayerSlot.Clear();

        // 플레이어 슬롯 하이라이트 초기화
        foreach (var slot in playerSlots)
        {
            if (slot != null)
            {
                SlotHighlight highlight = slot.GetComponent<SlotHighlight>();
                if (highlight == null)
                {
                    highlight = slot.gameObject.AddComponent<SlotHighlight>();
                }
                highlight.highlightColor = highlightColor;
                _slotHighlights[slot] = highlight;
                _slotIsPlayerSlot[slot] = true; // 플레이어 슬롯으로 캐싱
            }
        }

        // 적 슬롯 하이라이트 초기화
        foreach (var slot in enemySlots)
        {
            if (slot != null)
            {
                SlotHighlight highlight = slot.GetComponent<SlotHighlight>();
                if (highlight == null)
                {
                    highlight = slot.gameObject.AddComponent<SlotHighlight>();
                }
                highlight.highlightColor = highlightColor;
                _slotHighlights[slot] = highlight;
                _slotIsPlayerSlot[slot] = false; // 적 슬롯으로 캐싱
            }
        }
    }

    /// <summary>
    /// 슬롯 하이라이트 업데이트 (드래그 중인 카드에 따라)
    /// </summary>
    void UpdateSlotHighlights()
    {
        // 드래그 중인 카드 확인
        _draggingCard = GetDraggingCard();

        // 모든 슬롯 하이라이트 업데이트
        foreach (var kvp in _slotHighlights)
        {
            Transform slot = kvp.Key;
            SlotHighlight highlight = kvp.Value;
            
            if (slot == null || highlight == null) continue;

            // 캐싱된 플레이어 슬롯 여부 사용 (최적화)
            bool isPlayerSlot = _slotIsPlayerSlot.TryGetValue(slot, out bool cached) ? cached : false;

            // 하이라이트 알파값 계산
            float alpha = CalculateHighlightAlpha(slot, isPlayerSlot);
            highlight.SetAlpha(alpha);
        }
    }

    /// <summary>
    /// 드래그 중인 카드 가져오기
    /// </summary>
    CardDisplay GetDraggingCard()
    {
        // Draggable의 정적 변수를 통해 드래그 중인 카드 확인
        if (Draggable.currentDragging != null)
        {
            Draggable draggable = Draggable.currentDragging;
            CardDisplay card = draggable.GetComponent<CardDisplay>();
            
            // 손패에서 나온 캐릭터 카드만 하이라이트
            if (card != null && card.data != null && 
                !card.data.IsSpell() && 
                draggable.sourceZone == ZoneType.Hand)
            {
                return card;
            }
        }

        return null;
    }

    /// <summary>
    /// 슬롯의 하이라이트 알파값 계산
    /// </summary>
    float CalculateHighlightAlpha(Transform slot, bool isPlayerSlot)
    {
        // 드래그 중인 카드가 없으면 하이라이트 없음
        if (_draggingCard == null || _draggingCard.data == null)
        {
            return 0f;
        }

        // 플레이어 턴이 아니면 하이라이트 없음
        if (GameManager.instance == null || !GameManager.instance.CanPlayerAct())
        {
            return 0f;
        }

        // 플레이어 슬롯이 아니면 하이라이트 없음
        if (!isPlayerSlot)
        {
            return 0f;
        }

        // 슬롯이 이미 점유되어 있으면 하이라이트 없음
        int slotIndex = -1;
        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i] == slot)
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex >= 0 && _playerSlotCards[slotIndex] != null)
        {
            return 0f; // 슬롯이 이미 점유됨
        }

        // 마나 체크
        if (!GameManager.instance.CanPlayerAct() || 
            GameManager.instance.playerCurrentMana < _draggingCard.data.mana)
        {
            return 0f; // 마나 부족
        }

        // 필드가 가득 찼는지 확인
        if (IsFieldFull(true))
        {
            return 0f; // 필드 가득 참
        }

        // 모든 조건을 만족하면 하이라이트
        return 1f;
    }

    /// <summary>
    /// 가장 가까운 빈 슬롯 찾기
    /// </summary>
    public Transform GetNearestEmptySlot(Vector2 screenPosition, bool isPlayer)
    {
        Transform[] slots = isPlayer ? playerSlots : enemySlots;
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;

        if (slots == null || slots.Length == 0) return null;

        Canvas canvas = GetComponentInParent<Canvas>();
        Camera uiCamera = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            ? canvas.worldCamera : null;

        Transform nearestSlot = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (cards[i] != null) continue; // 이미 카드가 있으면 스킵

            RectTransform slotRect = slots[i].GetComponent<RectTransform>();
            if (slotRect == null) continue;

            // 스크린 좌표를 로컬 좌표로 변환
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                slotRect, screenPosition, uiCamera, out localPoint))
            {
                float distance = Vector2.Distance(localPoint, Vector2.zero);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestSlot = slots[i];
                }
            }
        }

        // 슬롯 내부에 정확히 있지 않아도, 가장 가까운 슬롯 반환
        if (nearestSlot != null)
        {
            return nearestSlot;
        }

        // 거리 기반으로 가장 가까운 슬롯 찾기 (스크린 좌표 기준)
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) continue;
            if (cards[i] != null) continue;

            Vector3 slotScreenPos = RectTransformUtility.WorldToScreenPoint(
                uiCamera ?? Camera.main, slots[i].position);
            
            float distance = Vector2.Distance(screenPosition, slotScreenPos);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestSlot = slots[i];
            }
        }

        return nearestSlot;
    }

    /// <summary>
    /// 첫 번째 빈 슬롯 찾기 (순서대로)
    /// </summary>
    public Transform GetFirstEmptySlot(bool isPlayer)
    {
        Transform[] slots = isPlayer ? playerSlots : enemySlots;
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;

        if (slots == null) return null;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && cards[i] == null)
            {
                return slots[i];
            }
        }

        return null;
    }

    /// <summary>
    /// 필드용 카드 프리팹 가져오기
    /// </summary>
    GameObject GetFieldCardPrefab()
    {
        // FieldSlotManager에 설정되어 있으면 우선 사용
        if (fieldCardPrefab != null)
            return fieldCardPrefab;
        
        // DeckManager에서 가져오기
        if (DeckManager.instance != null && DeckManager.instance.cardFieldPrefab != null)
            return DeckManager.instance.cardFieldPrefab;
        
        // 없으면 기존 프리팹 사용 (하위 호환성)
        if (DeckManager.instance != null && DeckManager.instance.cardPrefab != null)
            return DeckManager.instance.cardPrefab;
        
        Debug.LogWarning("[FieldSlotManager] 필드용 카드 프리팹이 설정되지 않았습니다.");
        return null;
    }

    /// <summary>
    /// 필드에 카드 생성 및 배치 (필드용 프리팹 사용)
    /// </summary>
    public CardDisplay CreateAndPlaceCard(CardData cardData, Transform slot, bool isPlayer)
    {
        if (cardData == null)
        {
            Debug.LogError("[FieldSlotManager] CreateAndPlaceCard: cardData가 null입니다.");
            return null;
        }
        
        if (slot == null)
        {
            Debug.LogError("[FieldSlotManager] CreateAndPlaceCard: slot이 null입니다.");
            return null;
        }

        GameObject prefab = GetFieldCardPrefab();
        if (prefab == null)
        {
            Debug.LogError($"[FieldSlotManager] 필드용 카드 프리팹이 없습니다. (cardFieldPrefab: {fieldCardPrefab}, DeckManager.cardFieldPrefab: {DeckManager.instance?.cardFieldPrefab})");
            return null;
        }

        // 필드용 프리팹으로 카드 생성
        GameObject cardObj = Instantiate(prefab);
        CardDisplay cardDisplay = cardObj.GetComponent<CardDisplay>();
        
        if (cardDisplay == null)
        {
            Debug.LogError("[FieldSlotManager] 필드용 카드 프리팹에 CardDisplay 컴포넌트가 없습니다.");
            Destroy(cardObj);
            return null;
        }

        // 카드 초기화
        cardDisplay.Init(cardData, isPlayer);

        // 슬롯에 배치
        if (PlaceCardInSlot(cardDisplay, slot, isPlayer))
        {
            return cardDisplay;
        }
        else
        {
            Debug.LogError($"[FieldSlotManager] 카드 배치 실패: {cardData.title}");
            Destroy(cardObj);
            return null;
        }
    }

    /// <summary>
    /// 슬롯에 카드 배치
    /// </summary>
    public bool PlaceCardInSlot(CardDisplay card, Transform slot, bool isPlayer)
    {
        if (card == null || slot == null) return false;

        Transform[] slots = isPlayer ? playerSlots : enemySlots;
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;

        // 슬롯 인덱스 찾기
        int slotIndex = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == slot)
            {
                slotIndex = i;
                break;
            }
        }

        if (slotIndex == -1) return false;
        if (cards[slotIndex] != null) return false; // 이미 카드가 있음

        // 카드 배치 - 슬롯과 완전히 겹치도록 설정
        RectTransform cardRect = card.GetComponent<RectTransform>();
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        
        if (cardRect == null || slotRect == null)
        {
            Debug.LogError("[FieldSlotManager] 카드 또는 슬롯에 RectTransform이 없습니다.");
            return false;
        }

        // 1. 부모 설정 (worldPositionStays = false로 로컬 좌표 유지)
        card.transform.SetParent(slot, false);
        
        // 2. 필드용 프리팹이면 이미 올바른 크기로 설정되어 있을 수 있음
        // 하지만 확실하게 하기 위해 슬롯의 모든 속성을 카드에 정확히 복사
        cardRect.anchorMin = slotRect.anchorMin;
        cardRect.anchorMax = slotRect.anchorMax;
        cardRect.pivot = slotRect.pivot;
        cardRect.sizeDelta = slotRect.sizeDelta;
        
        // 3. 위치와 회전, 스케일 완전히 초기화
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localPosition = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
        
        // 4. offsetMin, offsetMax도 초기화 (레이아웃 그룹 등에 영향받지 않도록)
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        
        // 5. RectTransform 강제 업데이트
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
        
        // 6. 필드용 프리팹을 사용하면 위치가 더 정확할 수 있지만, 한 프레임 후 재확인
        StartCoroutine(FixCardPositionAfterFrame(cardRect, slotRect));

        cards[slotIndex] = card;

        // Slot 좌표 매핑 저장
        if (_transformToSlot.ContainsKey(slot))
        {
            _cardToSlot[card] = _transformToSlot[slot];
        }

        // 비주얼 업데이트 (필드 비주얼로 전환)
        // 부모 변경 후 한 프레임 대기하여 UpdateVisual 호출
        if (this != null && gameObject != null && enabled && gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedVisualUpdate(card));
        }
        else
        {
            // FieldSlotManager가 비활성화된 경우 직접 호출
            if (card != null) card.UpdateVisual();
        }

        return true;
    }
    
    /// <summary>
    /// 지연된 비주얼 업데이트 (부모 변경 후 확실하게)
    /// </summary>
    System.Collections.IEnumerator DelayedVisualUpdate(CardDisplay card)
    {
        yield return null; // 한 프레임 대기
        
        if (card != null)
        {
            card.UpdateVisual();
        }
    }

    /// <summary>
    /// 한 프레임 후 카드 위치 재확인 및 수정
    /// </summary>
    System.Collections.IEnumerator FixCardPositionAfterFrame(RectTransform cardRect, RectTransform slotRect)
    {
        yield return null; // 한 프레임 대기
        
        if (cardRect == null || slotRect == null) yield break;
        
        // 모든 RectTransform 속성을 다시 한 번 정확히 설정
        // (레이아웃 그룹이나 다른 컴포넌트가 변경했을 수 있음)
        
        // 1. 앵커와 피벗
        cardRect.anchorMin = slotRect.anchorMin;
        cardRect.anchorMax = slotRect.anchorMax;
        cardRect.pivot = slotRect.pivot;
        
        // 2. 크기
        cardRect.sizeDelta = slotRect.sizeDelta;
        
        // 3. 위치 완전히 초기화
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.localPosition = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one;
        
        // 4. offset도 초기화
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;
        
        // 5. 최종 업데이트
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
        
        // 6. 한 프레임 더 대기 후 최종 확인
        yield return null;
        
        if (cardRect != null && slotRect != null)
        {
            // 위치가 여전히 0이 아니면 강제로 0으로 설정
            if (cardRect.anchoredPosition.magnitude > 0.01f)
            {
                cardRect.anchoredPosition = Vector2.zero;
            }
            
            if (cardRect.localPosition.magnitude > 0.01f)
            {
                cardRect.localPosition = Vector3.zero;
            }
            
            Canvas.ForceUpdateCanvases();
        }
    }

    /// <summary>
    /// 슬롯에서 카드 제거
    /// </summary>
    public void RemoveCardFromSlot(CardDisplay card, bool isPlayer)
    {
        if (card == null) return;

        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;
        Transform[] slots = isPlayer ? playerSlots : enemySlots;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == card)
            {
                cards[i] = null;
                
                // Slot 좌표 매핑 제거
                if (_cardToSlot.ContainsKey(card))
                {
                    _cardToSlot.Remove(card);
                }
                break;
            }
        }
    }

    /// <summary>
    /// 필드에 카드가 몇 개 있는지 반환
    /// </summary>
    public int GetCardCount(bool isPlayer)
    {
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;
        int count = 0;
        foreach (var card in cards)
        {
            if (card != null) count++;
        }
        return count;
    }

    /// <summary>
    /// 필드가 가득 찼는지 확인
    /// </summary>
    public bool IsFieldFull(bool isPlayer)
    {
        Transform[] slots = isPlayer ? playerSlots : enemySlots;
        return GetCardCount(isPlayer) >= slots.Length;
    }

    // ===== Slot 좌표 시스템 메서드 =====

    /// <summary>
    /// Transform에서 Slot 좌표 가져오기
    /// </summary>
    public Slot GetSlotFromTransform(Transform slotTransform)
    {
        if (_transformToSlot.ContainsKey(slotTransform))
            return _transformToSlot[slotTransform];
        return Slot.None;
    }

    /// <summary>
    /// Slot 좌표에서 Transform 가져오기
    /// </summary>
    public Transform GetTransformFromSlot(Slot slot)
    {
        if (_slotToTransform.ContainsKey(slot))
            return _slotToTransform[slot];
        return null;
    }

    /// <summary>
    /// 카드의 Slot 좌표 가져오기
    /// </summary>
    public Slot GetCardSlot(CardDisplay card)
    {
        if (_cardToSlot.ContainsKey(card))
            return _cardToSlot[card];
        return Slot.None;
    }

    /// <summary>
    /// 두 카드 간의 거리 계산 (Slot 좌표 기반)
    /// </summary>
    public int GetDistanceBetweenCards(CardDisplay card1, CardDisplay card2)
    {
        Slot slot1 = GetCardSlot(card1);
        Slot slot2 = GetCardSlot(card2);

        if (slot1 == Slot.None || slot2 == Slot.None)
            return -1; // 무효

        return Mathf.Abs(slot1.x - slot2.x) + Mathf.Abs(slot1.y - slot2.y) + Mathf.Abs(slot1.p - slot2.p);
    }

    /// <summary>
    /// 특정 거리 내의 카드들 찾기
    /// </summary>
    public List<CardDisplay> GetCardsInRange(CardDisplay centerCard, int range, bool includeSelf = false)
    {
        List<CardDisplay> result = new List<CardDisplay>();
        Slot centerSlot = GetCardSlot(centerCard);

        if (centerSlot == Slot.None) return result;

        // 플레이어 필드 카드들 확인
        foreach (var card in _playerSlotCards)
        {
            if (card == null) continue;
            if (!includeSelf && card == centerCard) continue;

            Slot cardSlot = GetCardSlot(card);
            if (cardSlot != Slot.None && centerSlot.IsInDistanceStraight(cardSlot, range))
            {
                result.Add(card);
            }
        }

        // 적 필드 카드들 확인
        foreach (var card in _enemySlotCards)
        {
            if (card == null) continue;
            if (!includeSelf && card == centerCard) continue;

            Slot cardSlot = GetCardSlot(card);
            if (cardSlot != Slot.None && centerSlot.IsInDistanceStraight(cardSlot, range))
            {
                result.Add(card);
            }
        }

        return result;
    }

    /// <summary>
    /// 특정 거리 내의 빈 슬롯들 찾기
    /// </summary>
    public List<Transform> GetEmptySlotsInRange(Transform centerSlot, int range)
    {
        List<Transform> result = new List<Transform>();
        Slot center = GetSlotFromTransform(centerSlot);

        if (center == Slot.None) return result;

        // 모든 슬롯 확인
        foreach (var kvp in _transformToSlot)
        {
            Transform slotTransform = kvp.Key;
            Slot slot = kvp.Value;

            // 빈 슬롯인지 확인
            bool isEmpty = true;
            if (_playerSlotCards != null)
            {
                for (int i = 0; i < playerSlots.Length; i++)
                {
                    if (playerSlots[i] == slotTransform && _playerSlotCards[i] != null)
                    {
                        isEmpty = false;
                        break;
                    }
                }
            }
            if (isEmpty && _enemySlotCards != null)
            {
                for (int i = 0; i < enemySlots.Length; i++)
                {
                    if (enemySlots[i] == slotTransform && _enemySlotCards[i] != null)
                    {
                        isEmpty = false;
                        break;
                    }
                }
            }

            if (isEmpty && center.IsInDistanceStraight(slot, range))
            {
                result.Add(slotTransform);
            }
        }

        return result;
    }

    /// <summary>
    /// 랜덤 빈 슬롯 가져오기 (플레이어 측)
    /// </summary>
    public Transform GetRandomEmptySlot(bool isPlayer, System.Random rand = null)
    {
        Transform[] slots = isPlayer ? playerSlots : enemySlots;
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;
        List<Transform> emptySlots = new List<Transform>();

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && cards[i] == null)
            {
                emptySlots.Add(slots[i]);
            }
        }

        if (emptySlots.Count == 0) return null;

        if (rand == null) rand = new System.Random();
        return emptySlots[rand.Next(emptySlots.Count)];
    }

    /// <summary>
    /// 필드의 모든 카드 가져오기
    /// </summary>
    public CardDisplay[] GetAllCardsOnField(bool isPlayer)
    {
        CardDisplay[] cards = isPlayer ? _playerSlotCards : _enemySlotCards;
        List<CardDisplay> result = new List<CardDisplay>();
        
        foreach (var card in cards)
        {
            if (card != null)
            {
                result.Add(card);
            }
        }
        
        return result.ToArray();
    }
}

