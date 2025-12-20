using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class CardDisplay : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Data Source")]
    public CardData data;
    public bool isMine = false;

    [Header("Runtime Stats")]
    public int currentAttack;
    public int currentHp;
    public int currentLust;

    [Header("Combat State")]
    public bool canAttack = false;

    // [제거됨] currentSlot - FieldSlotManager로 대체됨

    [Header("Gaze System State")]
    public bool isArtRevealed = false;
    public bool isInfoRevealed = false;

    [Header("Visual Groups (가변 비주얼)")]
    [Tooltip("손패 비주얼 프리팹 (필드에 있을 때는 파괴됨). 없으면 기존 handVisual 사용")]
    public GameObject handVisualPrefab;
    [Tooltip("필드 비주얼 프리팹 (손패에 있을 때는 파괴됨). 없으면 기존 boardVisual 사용")]
    public GameObject boardVisualPrefab;
    
    // 기존 방식 호환성 (프리팹이 없을 때 사용)
    [Tooltip("기존 방식: 손패 비주얼 GameObject (프리팹 방식 사용 시 무시됨)")]
    public GameObject handVisual;
    [Tooltip("기존 방식: 필드 비주얼 GameObject (프리팹 방식 사용 시 무시됨)")]
    public GameObject boardVisual;
    
    // 현재 인스턴스화된 비주얼
    private GameObject _currentHandVisual;
    private GameObject _currentBoardVisual;

    [Header("Hand UI References")]
    public Image handArt;
    public TMP_Text handTitle;
    public TMP_Text handMana;
    public TMP_Text handAttack;
    public TMP_Text handHp;
    public TMP_Text handLust;
    public GameObject handLustIcon;

    [Header("Board UI References")]
    public Image boardArt;
    public TMP_Text boardAttack;
    public TMP_Text boardHp;
    public GameObject boardTauntOverlay;
    public GameObject boardStealthOverlay; // ★ 은신 오버레이 추가

    private Vector3 originalScale;

    // 최적화: 캐싱
    private DropZone _cachedDropZone;
    private bool _dropZoneCached = false;
    
    // UI 참조 캐싱 (GetComponentsInChildren 호출 최소화)
    private TMP_Text _cachedHandManaText;
    private TMP_Text _cachedHandAttackText;
    private TMP_Text _cachedHandHpText;
    private TMP_Text _cachedHandLustText;
    private TMP_Text _cachedBoardAttackText;
    private TMP_Text _cachedBoardHpText;
    private bool _uiReferencesCached = false;
    
    // 슬롯 검색 캐싱
    private bool? _cachedIsOnBoard = null;
    private Transform _cachedParent = null;
    
    // 디버그 로그 플래그 (개발 중에만 활성화)
    private static bool _enableDebugLogs = false;

    void Awake() 
    { 
        originalScale = transform.localScale;
        
        // 기존 방식 호환성: handVisual, boardVisual이 있으면 프리팹으로 변환
        if (handVisual != null && handVisualPrefab == null)
        {
            // 기존 handVisual을 프리팹으로 사용할 수 없으므로 경고
            Debug.LogWarning($"[CardDisplay] handVisual이 설정되어 있지만 handVisualPrefab이 없습니다. 프리팹 방식으로 전환해주세요.");
        }
        if (boardVisual != null && boardVisualPrefab == null)
        {
            // 기존 boardVisual을 프리팹으로 사용할 수 없으므로 경고
            Debug.LogWarning($"[CardDisplay] boardVisual이 설정되어 있지만 boardVisualPrefab이 없습니다. 프리팹 방식으로 전환해주세요.");
        }
    }
    
    /// <summary>
    /// 손패 비주얼의 UI 참조 업데이트 및 캐싱
    /// </summary>
    void UpdateHandVisualReferences()
    {
        if (_currentHandVisual == null) return;
        
        // 한 번만 찾고 캐싱
        if (!_uiReferencesCached || handArt == null)
        {
            if (handArt == null) handArt = _currentHandVisual.GetComponentInChildren<Image>();
            if (handTitle == null) handTitle = _currentHandVisual.GetComponentInChildren<TMP_Text>();
            
            // TMP_Text 컴포넌트들을 한 번에 찾아서 캐싱
            TMP_Text[] texts = _currentHandVisual.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                string name = text.name;
                if (name.Contains("Mana") || name.Contains("Cost"))
                    _cachedHandManaText = text;
                else if (name.Contains("Attack") || name.Contains("ATK"))
                    _cachedHandAttackText = text;
                else if (name.Contains("HP") || name.Contains("Health"))
                    _cachedHandHpText = text;
                else if (name.Contains("Lust"))
                    _cachedHandLustText = text;
            }
            
            _uiReferencesCached = true;
        }
    }
    
    /// <summary>
    /// 필드 비주얼의 UI 참조 업데이트 및 캐싱
    /// </summary>
    void UpdateBoardVisualReferences()
    {
        if (_currentBoardVisual == null) return;
        
        // 한 번만 찾고 캐싱
        if (!_uiReferencesCached || boardArt == null)
        {
            if (boardArt == null) boardArt = _currentBoardVisual.GetComponentInChildren<Image>();
            
            // TMP_Text 컴포넌트들을 한 번에 찾아서 캐싱
            TMP_Text[] texts = _currentBoardVisual.GetComponentsInChildren<TMP_Text>();
            foreach (var text in texts)
            {
                string name = text.name;
                if (name.Contains("Attack") || name.Contains("ATK"))
                    _cachedBoardAttackText = text;
                else if (name.Contains("HP") || name.Contains("Health"))
                    _cachedBoardHpText = text;
            }
            
            _uiReferencesCached = true;
        }
    }

    // 최적화: 부모가 변경되면 캐시 무효화 및 도발 재등록
    void OnTransformParentChanged()
    {
        // 이전 구역에서 도발 해제
        if (_cachedDropZone != null)
            _cachedDropZone.UnregisterTaunt(this);

        _dropZoneCached = false;

        // 새 구역에 도발 등록
        DropZone newZone = GetComponentInParent<DropZone>();
        if (newZone != null && data != null && data.HasKeyword(Keyword.Taunt))
            newZone.RegisterTaunt(this);

        _cachedDropZone = newZone;
        _dropZoneCached = true;
        
        // 부모 변경 시 캐시 무효화
        _cachedIsOnBoard = null;
        _cachedParent = null;
        
        // 부모 변경 시 비주얼 업데이트 (필드/손패 전환)
        // 코루틴 대신 직접 호출 (GameObject 활성화 상태와 무관하게 안전)
        // 한 프레임 후 업데이트가 필요하면 호출하는 쪽에서 처리
        // 여기서는 캐시만 무효화하고, 실제 업데이트는 PlaceCardInSlot 등에서 처리
    }
    
    void OnEnable()
    {
        // GameObject가 활성화될 때 비주얼 업데이트 (부모 변경 후 비활성화되었다가 다시 활성화된 경우)
        if (data != null)
        {
            _cachedIsOnBoard = null;
            _cachedParent = null;
            UpdateVisual();
        }
    }

    public void Init(CardData cardData, bool ownedByPlayer)
    {
        if (cardData == null) return;
        this.data = cardData;
        this.isMine = ownedByPlayer;

        // 실시간 스탯 초기화
        currentAttack = data.attack;
        currentHp = data.hp;
        currentLust = data.lust_attack;

        if (isMine)
        {
            isArtRevealed = true;
            isInfoRevealed = true;
            // ★ [추가] 돌진 키워드가 있다면 소환 즉시 공격 가능, 아니면 대기
            canAttack = data.HasKeyword(Keyword.Charge);
        }
        else
        {
            // 적 카드도 돌진 키워드가 있으면 소환 즉시 공격 가능
            canAttack = data.HasKeyword(Keyword.Charge);
        }

        UpdateVisual();
    }

    public void UpdateVisual()
    {
        if (data == null) return;

        // 1. 구역 확인 (FieldSlotManager 또는 DropZone에 있는 경우) - 캐싱 최적화
        bool isOnBoard = false;
        Transform currentParent = transform.parent;
        
        // 부모가 변경되지 않았고 캐시가 있으면 재사용
        if (_cachedIsOnBoard.HasValue && currentParent == _cachedParent)
        {
            isOnBoard = _cachedIsOnBoard.Value;
        }
        else
        {
            // 부모가 변경되었거나 캐시가 없으면 새로 확인
            _cachedParent = currentParent;
            
            // FieldSlotManager를 통해 필드에 있는지 확인
            if (FieldSlotManager.instance != null && currentParent != null)
            {
                Transform[] playerSlots = FieldSlotManager.instance.playerSlots;
                Transform[] enemySlots = FieldSlotManager.instance.enemySlots;
                
                // 빠른 검색: 배열 길이 확인 후 직접 비교
                int playerSlotCount = playerSlots != null ? playerSlots.Length : 0;
                int enemySlotCount = enemySlots != null ? enemySlots.Length : 0;
                
                for (int i = 0; i < playerSlotCount && !isOnBoard; i++)
                {
                    if (playerSlots[i] == currentParent)
                    {
                        isOnBoard = true;
                        break;
                    }
                }
                
                if (!isOnBoard)
                {
                    for (int i = 0; i < enemySlotCount; i++)
                    {
                        if (enemySlots[i] == currentParent)
                        {
                            isOnBoard = true;
                            break;
                        }
                    }
                }
            }
            
            // FieldSlotManager로 확인되지 않으면 DropZone 확인 (하위 호환성)
            if (!isOnBoard)
            {
                if (!_dropZoneCached || transform.hasChanged)
                {
                    _cachedDropZone = GetComponentInParent<DropZone>();
                    _dropZoneCached = true;
                    transform.hasChanged = false;
                }
                isOnBoard = (_cachedDropZone != null && (_cachedDropZone.zoneType == ZoneType.PlayerField || _cachedDropZone.zoneType == ZoneType.EnemyField));
            }
            
            // 캐시 저장
            _cachedIsOnBoard = isOnBoard;
        }

        // 2. 비주얼 전환 (프리팹 인스턴스화 방식 또는 기존 방식)
        if (isOnBoard)
        {
            // 필드에 있을 때: 필드 비주얼 표시, 손패 비주얼 제거
            
            // 손패 비주얼 제거
            if (_currentHandVisual != null)
            {
                Destroy(_currentHandVisual);
                _currentHandVisual = null;
                _uiReferencesCached = false; // 캐시 무효화
            }
            if (handVisual != null) handVisual.SetActive(false);
            
            // 필드 비주얼 표시
            if (boardVisualPrefab != null)
            {
                // 프리팹 방식
                if (_currentBoardVisual == null)
                {
                    _currentBoardVisual = Instantiate(boardVisualPrefab, transform);
                    _currentBoardVisual.name = "BoardVisual";
                    
                    // RectTransform 설정 (카드와 동일하게)
                    RectTransform visualRect = _currentBoardVisual.GetComponent<RectTransform>();
                    RectTransform cardRect = GetComponent<RectTransform>();
                    if (visualRect != null && cardRect != null)
                    {
                        visualRect.anchorMin = Vector2.zero;
                        visualRect.anchorMax = Vector2.one;
                        visualRect.sizeDelta = Vector2.zero;
                        visualRect.anchoredPosition = Vector2.zero;
                    }
                    
                    // UI 참조 캐시 무효화 (새 비주얼이 생성되었으므로)
                    _uiReferencesCached = false;
                    UpdateBoardVisualReferences();
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[CardDisplay] 필드 비주얼 인스턴스화: {data?.title ?? "Unknown"}");
                }
                else
                {
                    _currentBoardVisual.SetActive(true);
                }
            }
            else if (boardVisual != null)
            {
                // 기존 방식 (하위 호환성)
                boardVisual.SetActive(true);
                _currentBoardVisual = boardVisual;
                _uiReferencesCached = false; // 캐시 무효화
            }
            else if (_enableDebugLogs)
            {
                Debug.LogWarning($"[CardDisplay] 필드 비주얼이 없습니다! (boardVisualPrefab: {boardVisualPrefab}, boardVisual: {boardVisual})");
            }
        }
        else
        {
            // 손패에 있을 때: 손패 비주얼 표시, 필드 비주얼 제거
            
            // 필드 비주얼 제거
            if (_currentBoardVisual != null && _currentBoardVisual != boardVisual)
            {
                Destroy(_currentBoardVisual);
                _currentBoardVisual = null;
                _uiReferencesCached = false; // 캐시 무효화
            }
            if (boardVisual != null) boardVisual.SetActive(false);
            
            // 손패 비주얼 표시
            if (handVisualPrefab != null)
            {
                // 프리팹 방식
                if (_currentHandVisual == null)
                {
                    _currentHandVisual = Instantiate(handVisualPrefab, transform);
                    _currentHandVisual.name = "HandVisual";
                    
                    // UI 참조 캐시 무효화 (새 비주얼이 생성되었으므로)
                    _uiReferencesCached = false;
                    UpdateHandVisualReferences();
                    
                    if (_enableDebugLogs)
                        Debug.Log($"[CardDisplay] 손패 비주얼 인스턴스화: {data?.title ?? "Unknown"}");
                }
            }
            else if (handVisual != null)
            {
                // 기존 방식 (하위 호환성)
                handVisual.SetActive(true);
                _currentHandVisual = handVisual;
            }
        }

        // 3. 일러스트 결정
        Sprite targetArt = isArtRevealed ? data.art_full : (data.art_censored ?? data.art_full);

        // 4. 손패 UI 갱신 (캐싱된 참조 사용)
        if (!isOnBoard && _currentHandVisual != null)
        {
            // UI 참조가 없으면 찾기 (한 번만)
            if (!_uiReferencesCached)
            {
                UpdateHandVisualReferences();
            }
            
            // 캐싱된 참조 사용
            if (handArt != null) handArt.sprite = targetArt;
            if (handTitle != null) handTitle.text = data.title;
            
            // 캐싱된 TMP_Text 참조로 업데이트 (GetComponentsInChildren 호출 없음)
            if (_cachedHandManaText != null) _cachedHandManaText.text = data.mana.ToString();
            if (_cachedHandAttackText != null) _cachedHandAttackText.text = currentAttack.ToString();
            if (_cachedHandHpText != null) _cachedHandHpText.text = currentHp.ToString();
            if (_cachedHandLustText != null) _cachedHandLustText.text = currentLust > 0 ? currentLust.ToString() : "";
        }
        // 5. 필드 UI 갱신 (캐싱된 참조 사용)
        else if (isOnBoard)
        {
            // 프리팹 방식 또는 기존 방식 모두 처리
            GameObject targetBoardVisual = _currentBoardVisual ?? boardVisual;
            
            if (targetBoardVisual != null)
            {
                // UI 참조가 없으면 찾기 (한 번만)
                if (!_uiReferencesCached)
                {
                    UpdateBoardVisualReferences();
                }
                
                // 캐싱된 참조 사용
                if (boardArt != null)
                {
                    // ★ 검열 상태 적용: 검열 해제 시에만 art_board 사용
                    if (isArtRevealed)
                    {
                        boardArt.sprite = data.art_board ?? data.art_full;
                    }
                    else
                    {
                        boardArt.sprite = data.art_censored ?? data.art_full;
                    }
                    // ★ [추가] 공격 가능 여부 시각화 (녹색 테두리 등)
                    boardArt.color = (isMine && canAttack) ? Color.green : Color.white;
                }
                
                // 캐싱된 TMP_Text 참조로 업데이트 (GetComponentsInChildren 호출 없음)
                if (_cachedBoardAttackText != null) _cachedBoardAttackText.text = currentAttack.ToString();
                if (_cachedBoardHpText != null) _cachedBoardHpText.text = currentHp.ToString();
                
                // ★ [추가] 키워드 오버레이 처리 (도발/은신) - 한 번만 찾기
                if (boardTauntOverlay == null)
                    boardTauntOverlay = targetBoardVisual.transform.Find("TauntOverlay")?.gameObject;
                if (boardStealthOverlay == null)
                    boardStealthOverlay = targetBoardVisual.transform.Find("StealthOverlay")?.gameObject;
                    
                if (boardTauntOverlay != null) boardTauntOverlay.SetActive(data.HasKeyword(Keyword.Taunt));
                if (boardStealthOverlay != null) boardStealthOverlay.SetActive(data.HasKeyword(Keyword.Stealth));
            }
            else if (_enableDebugLogs)
            {
                Debug.LogWarning($"[CardDisplay] 필드 비주얼이 null입니다! (카드: {data?.title ?? "Unknown"})");
            }
        }
    }

    // --- 전투 및 턴 로직 ---
    public void OnTurnStart()
    {
        // 플레이어와 적 모두 턴 시작 시 공격 가능 상태로 변경
        // (단, Charge 키워드가 있으면 소환 즉시 공격 가능하므로 이미 true일 수 있음)
        canAttack = true;

        // ★ [추가] 턴 시작 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnTurnStart);

        UpdateVisual();
    }

    public bool CanAttackNow() => canAttack && currentAttack > 0 && data.IsCharacter();

    public void OnAttack(CardDisplay target)
    {
        canAttack = false;

        // ★ [추가] 공격 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnAttack, target);

        UpdateVisual();
    }

    // 마지막으로 데미지를 준 카드 (처치 이벤트용)
    private CardDisplay _lastAttacker;

    public void TakeDamage(int amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(int amount, CardDisplay attacker)
    {
        if (amount <= 0) return;

        _lastAttacker = attacker;
        currentHp -= amount;
        Debug.Log($"{data.title}이(가) {amount} 피해를 입음.");

        // 피해를 입었을 때 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDamaged);

        if (currentHp <= 0)
        {
            Die();
        }
        else UpdateVisual();
    }

    private void Die()
    {
        // 죽음 시 발동하는 효과 트리거
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerEffects(this, EffectTiming.OnDeath);

        // ★ 처치 이벤트 체크 (적이 아군에게 처치당한 경우)
        if (!isMine && _lastAttacker != null && _lastAttacker.isMine)
        {
            if (KillEventManager.instance != null)
            {
                KillEventManager.instance.OnEnemyKilled(_lastAttacker, this);
            }
        }

        // 필드 비주얼 업데이트
        if (FieldVisualManager.instance != null)
        {
            FieldVisualManager.instance.OnCardDestroyed(transform.position, isMine);
        }

        // 슬롯에서 제거
        if (FieldSlotManager.instance != null)
        {
            FieldSlotManager.instance.RemoveCardFromSlot(this, isMine);
        }

        // 도발 해제
        if (_cachedDropZone != null)
            _cachedDropZone.UnregisterTaunt(this);
        
        // 비주얼 정리
        if (_currentHandVisual != null)
        {
            Destroy(_currentHandVisual);
            _currentHandVisual = null;
        }
        if (_currentBoardVisual != null)
        {
            Destroy(_currentBoardVisual);
            _currentBoardVisual = null;
        }
        
        // 캐시 초기화
        _uiReferencesCached = false;
        _cachedIsOnBoard = null;
        _cachedParent = null;

        Destroy(gameObject);
    }

    // --- 마우스 상호작용 ---
    public void OnPointerClick(PointerEventData eventData)
    {
        // 타겟 선택 모드 우선 처리
        if (EffectManager.instance != null && EffectManager.instance.IsWaitingForTarget())
        {
            EffectManager.instance.OnTargetSelected(this);
            return;
        }

        // ★ 자위 대상 선택 모드 처리
        if (MasturbationManager.instance != null && MasturbationManager.instance.isSelectingTarget)
        {
            if (!isMine) // 적 카드만 선택 가능
            {
                MasturbationManager.instance.OnEnemyCardSelected(this);
                return;
            }
        }

        // ★ 릴리스 모드 처리
        if (ReleaseManager.instance != null && ReleaseManager.instance.IsInReleaseMode())
        {
            if (ReleaseManager.instance.TryReleaseCard(this))
                return;
        }

        // ★ 스펠 카드는 드래그로 사용하므로 클릭 시에는 카드 확대만

        // 일반 클릭: 카드 확대
        if (GameUIManager.instance != null)
            GameUIManager.instance.ShowCardZoom(this);
    }

    public void OnPointerEnter(PointerEventData eventData) { transform.localScale = originalScale * 1.05f; }
    public void OnPointerExit(PointerEventData eventData) { transform.localScale = originalScale; }
}