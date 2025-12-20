using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

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
    public GameObject manaIconPrefab;
    public Transform manaContainer;
    private List<Image> manaIcons = new List<Image>();

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

    // === 턴 관리 및 적 AI ===
    public void EndTurn()
    {
        if (isEnemyTurn) return;
        if (EffectManager.instance != null) EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnEnd, ZoneType.PlayerField);
        StartCoroutine(EnemyPhase());
    }

    IEnumerator EnemyPhase()
    {
        isEnemyTurn = true;
        // 적 턴 시작 시 마나 회복
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

        // 2. 적 카드 소환 AI
        yield return StartCoroutine(EnemyPlayCardsRoutine());

        // 3. 적 영웅 유혹 공격 (방어 마나 시스템 적용)
        if (enemyHero != null && enemyHero.heroData.canSeduceAttack)
        {
            bool isHeroAttackDone = false;
            enemyHero.ExecuteSeduceAttack(() => isHeroAttackDone = true);
            yield return new WaitUntil(() => isHeroAttackDone);
            if (playerHero.currentLust >= 100) yield break;
            yield return new WaitForSeconds(0.5f);
        }

        // 4. 적 하수인 공격
        if (enemyField != null)
        {
            CardDisplay[] enemies = enemyField.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay enemy in enemies)
            {
                if (playerHero.currentLust >= 100) yield break;
                if (enemy.data != null && enemy.data.IsCharacter())
                {
                    // 아군 필드에 하수인이 없으면 유혹 공격 이벤트 발생
                    if (playerField.childCount == 0)
                    {
                        bool done = false;

                        // ★ 세분화된 유혹 공격 정보 추출 ★
                        string mName = enemy.data.title;
                        Sprite mArt = enemy.data.seduce_event_art ?? (enemy.isArtRevealed ? enemy.data.art_full : enemy.data.art_censored);
                        int mLust = enemy.data.lust_attack;
                        int mManaDef = enemy.data.mana_defense; // 추가된 방어 마나

                        // GameUIManager에 5개의 인자 전달
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
        yield return new WaitForSeconds(0.5f);
        StartNewPlayerTurn();
    }

    IEnumerator EnemyPlayCardsRoutine()
    {
        int currentMinions = enemyField.childCount;
        while (currentMinions < 5)
        {
            // 최적화: 리스트 재사용
            _playableCards.Clear();
            foreach (CardData card in DeckManager.instance.enemyHand)
                if (card.mana <= enemyCurrentMana) _playableCards.Add(card);

            if (_playableCards.Count > 0)
            {
                _playableCards.Sort((a, b) => b.mana.CompareTo(a.mana));
                CardData best = _playableCards[0];
                enemyCurrentMana -= best.mana;
                DeckManager.instance.enemyHand.Remove(best);

                GameObject newCard = Instantiate(DeckManager.instance.cardPrefab, enemyField);
                newCard.GetComponent<CardDisplay>().Init(best, false);
                currentMinions++;
                MarkUIDirty();
                yield return new WaitForSeconds(1.0f);
            }
            else break;
        }
    }

    void ExecuteEnemyAttack(CardDisplay enemy, CardData monster)
    {
        CardDisplay target = FindTauntTarget();
        if (target == null && playerField.childCount > 0)
        {
            int randomIndex = Random.Range(0, playerField.childCount);
            target = playerField.GetChild(randomIndex).GetComponent<CardDisplay>();
        }

        if (target != null)
        {
            target.TakeDamage(enemy.currentAttack);
            enemy.TakeDamage(target.currentAttack);
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

    void StartNewPlayerTurn()
    {
        isEnemyTurn = false;
        turnCount++;
        if (playerMaxMana < 10) playerMaxMana++;

        if (manaLockTurnCount > 0) { playerCurrentMana = 0; manaLockTurnCount--; }
        else playerCurrentMana = playerMaxMana;

        playerCurrentFocus = playerMaxFocus;
        if (DeckManager.instance != null) DeckManager.instance.DrawCard(true);
        foreach (CardDisplay card in playerField.GetComponentsInChildren<CardDisplay>()) card.OnTurnStart();

        if (EffectManager.instance != null) EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnStart, ZoneType.PlayerField);
        MarkUIDirty();
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
}