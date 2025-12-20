using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// 턴 단계 정의
public enum TurnPhase
{
    None,           // 게임 시작 전 또는 종료
    StartPhase,     // 준비 단계: 드로우, 마나 회복
    ActionPhase,    // 행동 단계: 소환, 전투, 릴리스, 집중력 등
    EnemyPhase,     // 적 행동/정화 단계: 적 소환, 유혹, 정화
    ResolvePhase    // 결과 단계: 데미지/디버프 적용, EP 판정
}

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    // 턴 단계 변경 이벤트
    public event Action<TurnPhase> OnPhaseChanged;

    [Header("턴 단계 관리")]
    [SerializeField] private TurnPhase _currentPhase = TurnPhase.None;
    public TurnPhase CurrentPhase
    {
        get => _currentPhase;
        private set
        {
            if (_currentPhase != value)
            {
                _currentPhase = value;
                OnPhaseChanged?.Invoke(_currentPhase);
                MarkUIDirty();
            }
        }
    }

    [Header("플레이어 실시간 자원")]
    public int playerMaxMana = 1;
    public int playerCurrentMana = 1;
    public int playerMaxFocus = 1;
    public int playerCurrentFocus = 1;

    [Header("적 실시간 자원")]
    public int enemyMaxMana = 1;
    public int enemyCurrentMana = 1;

    // 외부 참조용 별명 (Property)
    public int currentMana => isEnemyTurn ? enemyCurrentMana : playerCurrentMana;
    public int maxMana => isEnemyTurn ? enemyMaxMana : playerMaxMana;
    public int currentFocus => playerCurrentFocus;

    [Header("상태 관리")]
    [SerializeField] private int manaLockTurnCount = 0;
    public int turnCount = 1;
    public bool isEnemyTurn = false;

    [Header("UI 연결")]
    public TMP_Text manaText;
    public TMP_Text focusText;
    public TMP_Text turnText;
    public TMP_Text phaseText;          // 현재 단계 표시
    public GameObject manaIconPrefab;
    public Transform manaContainer;
    private List<Image> manaIcons = new List<Image>();

    // 단계별 표시 이름
    private readonly Dictionary<TurnPhase, string> _phaseNames = new Dictionary<TurnPhase, string>
    {
        { TurnPhase.None, "" },
        { TurnPhase.StartPhase, "준비 단계" },
        { TurnPhase.ActionPhase, "행동 단계" },
        { TurnPhase.EnemyPhase, "적 행동 단계" },
        { TurnPhase.ResolvePhase, "결과 단계" }
    };

    [Header("구역 및 영웅 참조")]
    public Transform playerField;
    public Transform enemyField;
    public HeroPortrait playerHero;
    public HeroPortrait enemyHero;

    // 최적화: UI 업데이트 지연 처리
    private bool _uiDirty = false;

    // 최적화: 리스트 재사용 (GC 감소)
    private readonly List<CardData> _playableCards = new List<CardData>();

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. 마나 구슬 UI 초기화
        if (manaIconPrefab != null && manaContainer != null)
        {
            for (int i = 0; i < 10; i++)
            {
                GameObject icon = Instantiate(manaIconPrefab, manaContainer);
                Transform fullIconTransform = icon.transform.Find("FullIcon");
                if (fullIconTransform != null) manaIcons.Add(fullIconTransform.GetComponent<Image>());
                icon.SetActive(false);
            }
        }

        // 2. 참조 자동 연결
        if (playerField == null) playerField = GameObject.Find("PlayerArea")?.transform;
        if (enemyField == null) enemyField = GameObject.Find("EnemyArea")?.transform;
        if (playerHero == null) playerHero = HeroPortrait.playerHero;
        if (enemyHero == null) enemyHero = HeroPortrait.enemyHero;

        MarkUIDirty();

        // 3. 첫 턴 시작
        StartCoroutine(StartFirstTurn());
    }

    IEnumerator StartFirstTurn()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(ExecutePlayerTurn());
    }

    void LateUpdate()
    {
        // 최적화: 프레임당 한 번만 UI 업데이트
        if (_uiDirty)
        {
            _uiDirty = false;
            UpdateUIImmediate();
        }
    }

    // === 자원 관리 함수 ===
    public bool TrySpendMana(int amount)
    {
        if (isEnemyTurn)
        {
            if (enemyCurrentMana >= amount) { enemyCurrentMana -= amount; MarkUIDirty(); return true; }
        }
        else
        {
            if (playerCurrentMana >= amount) { playerCurrentMana -= amount; MarkUIDirty(); return true; }
        }
        return amount == 0;
    }

    public void GainMana(int amount)
    {
        if (isEnemyTurn) enemyCurrentMana = Mathf.Min(enemyCurrentMana + amount, enemyMaxMana);
        else if (manaLockTurnCount <= 0) playerCurrentMana = Mathf.Min(playerCurrentMana + amount, playerMaxMana);
        MarkUIDirty();
    }

    public bool TryUseFocus()
    {
        if (playerCurrentFocus > 0) { playerCurrentFocus--; MarkUIDirty(); return true; }
        return false;
    }

    public void GainFocus(int amount) { playerCurrentFocus += amount; MarkUIDirty(); }
    public void SetManaLock(int turns) { manaLockTurnCount = turns; playerCurrentMana = 0; MarkUIDirty(); }

    // === 게임 이벤트 ===
    public void CheckGameOver()
    {
        if (playerHero.currentHealth <= 0) Debug.Log("<color=red>플레이어 패배...</color>");
        else if (enemyHero.currentHealth <= 0) Debug.Log("<color=cyan>플레이어 승리!</color>");
    }

    public void TriggerClimax()
    {
        if (enemyHero != null && enemyHero.heroData.climax_data != null)
        {
            GameUIManager.instance.ShowClimaxEvent(enemyHero.heroData.climax_data);
            StopAllCoroutines();
            StartCoroutine(ReturnToPlayerTurnAfterEvent());
        }
    }

    IEnumerator ReturnToPlayerTurnAfterEvent()
    {
        yield return new WaitUntil(() => GameUIManager.instance.currentState == GameUIState.None);
        yield return new WaitForSeconds(0.5f);
        StartNewPlayerTurn();
    }

    // === 턴 관리 (4단계 시스템) ===

    /// <summary>
    /// 플레이어 턴 실행 (준비 → 행동 단계)
    /// </summary>
    IEnumerator ExecutePlayerTurn()
    {
        isEnemyTurn = false;

        // === 1. 준비 단계 (StartPhase) ===
        CurrentPhase = TurnPhase.StartPhase;
        yield return StartCoroutine(ExecuteStartPhase());

        // === 2. 행동 단계 (ActionPhase) ===
        CurrentPhase = TurnPhase.ActionPhase;
        // 플레이어가 EndTurn() 호출할 때까지 대기
        // (행동 단계에서 자유롭게 행동 가능)
    }

    /// <summary>
    /// 준비 단계: 드로우, 마나 회복
    /// </summary>
    IEnumerator ExecuteStartPhase()
    {
        // 턴 카운트 증가 (첫 턴 제외)
        if (turnCount > 0 || playerMaxMana > 1)
        {
            turnCount++;
        }

        // 마나 증가 및 회복
        if (playerMaxMana < 10) playerMaxMana++;
        if (manaLockTurnCount > 0)
        {
            playerCurrentMana = 0;
            manaLockTurnCount--;
        }
        else
        {
            playerCurrentMana = playerMaxMana;
        }

        // 집중력 회복
        playerCurrentFocus = playerMaxFocus;

        // 카드 드로우
        if (DeckManager.instance != null)
        {
            DeckManager.instance.DrawCard(true);
        }

        // 아군 카드 턴 시작 처리
        if (FieldSlotManager.instance != null)
        {
            // FieldSlotManager에서 플레이어 필드 카드들 가져오기
            CardDisplay[] playerCards = FieldSlotManager.instance.GetAllCardsOnField(true);
            foreach (CardDisplay card in playerCards)
            {
                if (card != null) card.OnTurnStart();
            }
        }
        else if (playerField != null)
        {
            // 하위 호환성: 기존 방식
            foreach (CardDisplay card in playerField.GetComponentsInChildren<CardDisplay>())
            {
                card.OnTurnStart();
            }
        }

        // 턴 시작 효과 트리거
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnStart, ZoneType.PlayerField);
        }

        MarkUIDirty();
        yield return new WaitForSeconds(0.3f);
    }

    /// <summary>
    /// 턴 종료 버튼 (행동 단계 → 적 행동 단계)
    /// </summary>
    public void EndTurn()
    {
        if (isEnemyTurn) return;
        if (CurrentPhase != TurnPhase.ActionPhase) return;

        // 턴 종료 효과 트리거
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnEnd, ZoneType.PlayerField);
        }

        StartCoroutine(ExecuteEnemyTurn());
    }

    /// <summary>
    /// 적 턴 실행 (적 행동 → 결과 단계)
    /// </summary>
    IEnumerator ExecuteEnemyTurn()
    {
        isEnemyTurn = true;

        // === 3. 적 행동/정화 단계 (EnemyPhase) ===
        CurrentPhase = TurnPhase.EnemyPhase;
        yield return StartCoroutine(ExecuteEnemyPhase());

        // 절정 체크
        if (playerHero.currentLust >= 100) yield break;

        // === 4. 결과 단계 (ResolvePhase) ===
        CurrentPhase = TurnPhase.ResolvePhase;
        yield return StartCoroutine(ExecuteResolvePhase());

        // 다음 플레이어 턴 시작
        yield return new WaitForSeconds(0.3f);
        StartCoroutine(ExecutePlayerTurn());
    }

    /// <summary>
    /// 적 행동 단계: 적 소환, 유혹, 전투
    /// </summary>
    IEnumerator ExecuteEnemyPhase()
    {
        // 적 마나 회복
        if (enemyMaxMana < 10) enemyMaxMana++;
        enemyCurrentMana = enemyMaxMana;

        if (enemyHero != null) enemyHero.OnTurnStart();
        MarkUIDirty();
        yield return new WaitForSeconds(0.5f);

        // 1. 적 카드 드로우
        if (DeckManager.instance != null)
        {
            DeckManager.instance.DrawCard(false);
            yield return new WaitForSeconds(0.8f);
        }

        // 2. 적 하수인 턴 시작 처리 (이미 필드에 있던 카드들만 공격 가능 상태로 변경)
        // 주의: 소환 전에 호출해야 새로 소환된 카드는 공격 불가 상태로 유지됨
        if (FieldSlotManager.instance != null)
        {
            // FieldSlotManager에서 적 필드 카드들 가져오기
            CardDisplay[] enemyCards = FieldSlotManager.instance.GetAllCardsOnField(false);
            foreach (CardDisplay card in enemyCards)
            {
                if (card != null) card.OnTurnStart();
            }
        }
        else if (enemyField != null)
        {
            // 하위 호환성: 기존 방식
            foreach (CardDisplay card in enemyField.GetComponentsInChildren<CardDisplay>())
            {
                card.OnTurnStart();
            }
        }
        yield return new WaitForSeconds(0.3f);

        // 2-1. 적 카드 소환 AI (소환 후에는 canAttack = false 상태)
        yield return StartCoroutine(EnemyPlayCardsRoutine());
        yield return new WaitForSeconds(0.3f);

        // 3. 적 영웅 유혹 공격 (플레이어가 마나로 정화 가능)
        if (enemyHero != null && enemyHero.heroData.canSeduceAttack)
        {
            bool isHeroAttackDone = false;
            enemyHero.ExecuteSeduceAttack(() => isHeroAttackDone = true);
            yield return new WaitUntil(() => isHeroAttackDone);
            if (playerHero.currentLust >= 100) yield break;
            yield return new WaitForSeconds(0.5f);
        }

        // 4. 적 하수인 공격/유혹
        CardDisplay[] enemies = null;
        if (FieldSlotManager.instance != null)
        {
            enemies = FieldSlotManager.instance.GetAllCardsOnField(false);
        }
        else if (enemyField != null)
        {
            enemies = enemyField.GetComponentsInChildren<CardDisplay>();
        }
        
        if (enemies != null)
        {
            foreach (CardDisplay enemy in enemies)
            {
                if (playerHero.currentLust >= 100) yield break;
                if (enemy.data != null && enemy.data.IsCharacter())
                {
                    // 공격 가능한 카드만 공격 (나온 턴에는 공격 불가)
                    if (!enemy.CanAttackNow())
                    {
                        continue;
                    }

                    // 아군 필드에 하수인이 없으면 유혹 공격
                    bool hasPlayerMinions = false;
                    if (FieldSlotManager.instance != null)
                    {
                        hasPlayerMinions = FieldSlotManager.instance.GetCardCount(true) > 0;
                    }
                    else if (playerField != null)
                    {
                        hasPlayerMinions = playerField.childCount > 0;
                    }
                    
                    if (!hasPlayerMinions)
                    {
                        bool done = false;
                        string mName = enemy.data.title;
                        Sprite mArt = enemy.data.seduce_event_art ?? (enemy.isArtRevealed ? enemy.data.art_full : enemy.data.art_censored);
                        int mLust = enemy.data.lust_attack;
                        int mManaDef = enemy.data.mana_defense;

                        GameUIManager.instance.ShowSeduceEvent(mName, mArt, mLust, mManaDef, () => done = true);
                        yield return new WaitUntil(() => done);
                        if (playerHero.currentLust >= 100) yield break;
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.6f);
                        ExecuteEnemyAttack(enemy, enemy.data);
                        if (playerHero.currentLust >= 100) yield break;
                    }
                }
            }
        }

        if (enemyHero != null) enemyHero.OnTurnEnd();
    }

    /// <summary>
    /// 결과 단계: 정화 실패 데미지, EP 판정 등
    /// </summary>
    IEnumerator ExecuteResolvePhase()
    {
        // 현재는 EnemyPhase에서 즉시 처리하므로 추가 로직 필요 시 여기에 구현
        // 예: 지연된 효과 처리, 상태이상 적용 등
        yield return new WaitForSeconds(0.2f);

        // 게임 오버 체크
        CheckGameOver();
    }

    // === 레거시 호환 (기존 코드와의 호환성) ===
    void StartNewPlayerTurn()
    {
        StartCoroutine(ExecutePlayerTurn());
    }

    IEnumerator EnemyPlayCardsRoutine()
    {
        // FieldSlotManager를 사용하여 슬롯에 배치
        if (FieldSlotManager.instance == null)
        {
            Debug.LogWarning("[GameManager] FieldSlotManager가 없습니다. 기존 방식으로 소환합니다.");
            yield break;
        }

        while (!FieldSlotManager.instance.IsFieldFull(false)) // 적 필드가 가득 차지 않았을 때
        {
            // 최적화: 리스트 재사용
            _playableCards.Clear();
            foreach (CardData card in DeckManager.instance.enemyHand)
                if (card.mana <= enemyCurrentMana && card.IsCharacter()) // 캐릭터 카드만
                    _playableCards.Add(card);

            if (_playableCards.Count > 0)
            {
                _playableCards.Sort((a, b) => b.mana.CompareTo(a.mana));
                CardData best = _playableCards[0];
                enemyCurrentMana -= best.mana;
                DeckManager.instance.enemyHand.Remove(best);

                // 첫 번째 빈 슬롯 찾기
                Transform targetSlot = FieldSlotManager.instance.GetFirstEmptySlot(false);
                if (targetSlot != null)
                {
                    // 필드용 프리팹으로 카드 생성 및 배치
                    CardDisplay cardDisplay = FieldSlotManager.instance.CreateAndPlaceCard(best, targetSlot, false);
                    
                    if (cardDisplay != null)
                    {
                        // 비주얼 업데이트 (PlaceCardInSlot에서 호출되지만 확실히 하기 위해)
                        cardDisplay.UpdateVisual();
                        
                        // 비주얼 이펙트
                        if (FieldVisualManager.instance != null)
                        {
                            FieldVisualManager.instance.OnCardSummoned(targetSlot.position, false);
                        }

                        // 소환 효과 트리거
                        if (EffectManager.instance != null)
                        {
                            StartCoroutine(TriggerEnemySummonEffect(cardDisplay));
                        }

                        MarkUIDirty();
                        yield return new WaitForSeconds(1.0f);
                    }
                    else
                    {
                        // 배치 실패 (CreateAndPlaceCard에서 이미 파괴됨)
                        Debug.LogWarning("[GameManager] 적 카드 배치 실패");
                        break;
                    }
                }
                else
                {
                    // 슬롯이 없으면 중단
                    break;
                }
            }
            else break;
        }
    }

    /// <summary>
    /// 적 카드 소환 효과 트리거
    /// </summary>
    System.Collections.IEnumerator TriggerEnemySummonEffect(CardDisplay card)
    {
        yield return new WaitForSeconds(0.1f);
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerEffects(card, EffectTiming.OnSummon);
        }
    }

    void ExecuteEnemyAttack(CardDisplay enemy, CardData monster)
    {
        // 공격 가능 여부 체크 (나온 턴에는 공격 불가)
        if (!enemy.CanAttackNow())
        {
            Debug.Log($"{enemy.data.title}은(는) 아직 공격할 수 없습니다.");
            return;
        }

        CardDisplay target = FindTauntTarget();
        if (target == null && playerField.childCount > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, playerField.childCount);
            target = playerField.GetChild(randomIndex).GetComponent<CardDisplay>();
        }

        if (target != null)
        {
            // 공격 실행 (공격 후 canAttack = false로 설정)
            enemy.OnAttack(target);
            
            // 공격자 정보 전달 (처치 이벤트용)
            target.TakeDamage(enemy.currentAttack, enemy);
            enemy.TakeDamage(target.currentAttack, target);
        }
    }

    CardDisplay FindTauntTarget()
    {
        if (playerField == null) return null;
        foreach (Transform child in playerField)
        {
            CardDisplay cd = child.GetComponent<CardDisplay>();
            if (cd != null && cd.data.HasKeyword(Keyword.Taunt)) return cd;
        }
        return null;
    }

    // 최적화: UI 업데이트 요청 (LateUpdate에서 일괄 처리)
    public void MarkUIDirty() => _uiDirty = true;

    // 즉시 UI 업데이트 (외부에서 필요시 호출)
    public void UpdateUI() => UpdateUIImmediate();

    private void UpdateUIImmediate()
    {
        if (manaText != null)
            manaText.text = (manaLockTurnCount > 0) ? "Locked" : $"{playerCurrentMana}/{playerMaxMana}";
        if (focusText != null) focusText.text = $"Focus: {playerCurrentFocus}";
        if (turnText != null) turnText.text = $"Turn {turnCount}";
        if (phaseText != null && _phaseNames.TryGetValue(_currentPhase, out string phaseName))
            phaseText.text = phaseName;

        for (int i = 0; i < manaIcons.Count; i++)
        {
            if (i < playerMaxMana)
            {
                manaIcons[i].transform.parent.gameObject.SetActive(true);
                manaIcons[i].enabled = (manaLockTurnCount <= 0 && i < playerCurrentMana);
            }
            else manaIcons[i].transform.parent.gameObject.SetActive(false);
        }
        if (playerHero != null) playerHero.UpdateUI();
        if (enemyHero != null) enemyHero.UpdateUI();
    }

    // === 행동 가능 여부 체크 ===

    /// <summary>
    /// 플레이어가 행동 가능한지 확인 (행동 단계에서만)
    /// </summary>
    public bool CanPlayerAct()
    {
        return !isEnemyTurn && CurrentPhase == TurnPhase.ActionPhase;
    }

    /// <summary>
    /// 카드 소환이 가능한지 확인
    /// </summary>
    public bool CanSummonCard(CardData card)
    {
        if (!CanPlayerAct()) return false;
        if (card == null) return false;
        if (playerCurrentMana < card.mana) return false;
        if (playerField.childCount >= 5) return false;
        return true;
    }

    /// <summary>
    /// 자위 커맨드 사용 가능 여부 (EP 70% 이상)
    /// </summary>
    public bool CanUseMasturbation()
    {
        if (!CanPlayerAct()) return false;
        if (playerHero == null) return false;
        return playerHero.currentLust >= 70;
    }

    /// <summary>
    /// 현재 단계 이름 가져오기
    /// </summary>
    public string GetCurrentPhaseName()
    {
        return _phaseNames.TryGetValue(_currentPhase, out string name) ? name : "";
    }
}