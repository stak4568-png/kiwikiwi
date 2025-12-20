using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("자원 관리 (마나/집중력)")]
    public int maxMana = 1;
    public int currentMana = 1;
    public int maxFocus = 1;
    public int currentFocus = 1;

    [Header("마나 잠금 페널티")]
    [SerializeField] private int manaLockTurnCount = 0; // 마나가 잠긴 남은 턴 수

    [Header("턴 상태")]
    public int turnCount = 1;
    public bool isEnemyTurn = false;

    [Header("UI 연결 (텍스트)")]
    public TMP_Text manaText;
    public TMP_Text focusText;
    public TMP_Text turnText;

    [Header("UI 연결 (마나 구슬)")]
    public GameObject manaIconPrefab;
    public Transform manaContainer;
    private List<Image> manaIcons = new List<Image>();

    [Header("Zone 참조")]
    public Transform playerField;
    public Transform enemyField;

    [Header("Hero 참조")]
    public HeroPortrait playerHero;
    public HeroPortrait enemyHero;

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
                if (fullIconTransform != null)
                {
                    manaIcons.Add(fullIconTransform.GetComponent<Image>());
                }
                icon.SetActive(false);
            }
        }

        // 2. 필드 및 영웅 참조 자동 연결
        if (playerField == null) playerField = GameObject.Find("PlayerArea")?.transform;
        if (enemyField == null) enemyField = GameObject.Find("EnemyArea")?.transform;
        if (playerHero == null) playerHero = HeroPortrait.playerHero;
        if (enemyHero == null) enemyHero = HeroPortrait.enemyHero;

        UpdateUI();
    }

    // === 자원 관리 함수 ===

    public bool TrySpendMana(int amount)
    {
        // 마나 잠금 상태일 때는 항상 0이므로 소비 불가
        if (currentMana >= amount && amount > 0)
        {
            currentMana -= amount;
            UpdateUI();
            return true;
        }
        return amount == 0; // 비용이 0인 카드는 낼 수 있음
    }

    public void GainMana(int amount)
    {
        // 마나 잠금 상태가 아닐 때만 마나 획득 가능
        if (manaLockTurnCount <= 0)
        {
            currentMana = Mathf.Min(currentMana + amount, maxMana);
            UpdateUI();
        }
    }

    public bool TryUseFocus()
    {
        if (currentFocus > 0)
        {
            currentFocus--;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void GainFocus(int amount)
    {
        currentFocus += amount;
        UpdateUI();
    }

    /// <summary>
    /// 마나 잠금 페널티 설정 (수락 시 호출)
    /// </summary>
    public void SetManaLock(int turns)
    {
        manaLockTurnCount = turns;
        currentMana = 0; // 즉시 마나를 0으로 만듦
        Debug.Log($"<color=red>마나 잠금 페널티 발생: {turns}턴 동안 마나 사용 불가</color>");
        UpdateUI();
    }

    // === 게임 이벤트 제어 ===

    public void CheckGameOver()
    {
        if (playerHero.currentHealth <= 0)
            Debug.Log("<color=red>플레이어 패배...</color>");
        else if (enemyHero.currentHealth <= 0)
            Debug.Log("<color=cyan>플레이어 승리!</color>");
    }

    public void TriggerClimax()
    {
        if (enemyHero != null && enemyHero.heroData != null && enemyHero.heroData.climaxEvent != null)
        {
            if (GameUIManager.instance != null)
            {
                Debug.Log("GameManager: 클라이맥스 시작");
                GameUIManager.instance.ShowClimaxEvent(enemyHero.heroData.climaxEvent);

                // 클라이맥스 발생 시 적의 현재 공격 사이클 중단
                StopAllCoroutines();
                StartCoroutine(ReturnToPlayerTurnAfterEvent());
            }
        }
    }

    // 이벤트 종료 후 플레이어 턴으로 안전하게 복귀하는 코루틴
    IEnumerator ReturnToPlayerTurnAfterEvent()
    {
        // UI가 완전히 닫힐 때까지 대기
        yield return new WaitUntil(() => GameUIManager.instance.currentState == GameUIState.None);
        yield return new WaitForSeconds(0.5f);

        Debug.Log("이벤트 연출 종료. 플레이어 턴을 시작합니다.");
        StartNewPlayerTurn();
    }

    // === 턴 관리 및 적 AI ===

    public void EndTurn()
    {
        if (isEnemyTurn) return;

        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnEnd, ZoneType.PlayerField);

        StartCoroutine(EnemyPhase());
    }

    IEnumerator EnemyPhase()
    {
        isEnemyTurn = true;
        Debug.Log("── 적 턴 시작 ──");

        if (enemyHero != null) enemyHero.OnTurnStart();

        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnStart, ZoneType.EnemyField);

        yield return new WaitForSeconds(0.5f);

        // 1. 적 영웅 유혹 공격
        if (enemyHero != null && enemyHero.heroData != null && enemyHero.heroData.canSeduceAttack)
        {
            bool isHeroAttackDone = false;
            enemyHero.ExecuteSeduceAttack(() => isHeroAttackDone = true);
            yield return new WaitUntil(() => isHeroAttackDone);

            if (playerHero.currentLust >= 100) yield break;
            yield return new WaitForSeconds(0.5f);
        }

        // 2. 적 하수인들 공격
        if (enemyField != null)
        {
            CardDisplay[] enemies = enemyField.GetComponentsInChildren<CardDisplay>();
            foreach (CardDisplay enemy in enemies)
            {
                if (playerHero.currentLust >= 100) yield break;

                if (enemy.cardData is MonsterCardData monster)
                {
                    bool isSeduceAttack = (playerField.childCount == 0);

                    if (isSeduceAttack)
                    {
                        bool monsterAttackDone = false;
                        if (GameUIManager.instance != null)
                        {
                            string mName = monster.cardName;
                            Sprite mArt = monster.seduceEventArt ?? (enemy.isArtRevealed ? (monster.originalArt ?? monster.censoredArt) : monster.censoredArt);
                            int mLust = monster.lustAttack;

                            GameUIManager.instance.ShowSeduceEvent(mName, mArt, mLust, () => monsterAttackDone = true);
                            yield return new WaitUntil(() => monsterAttackDone);

                            if (playerHero.currentLust >= 100) yield break;
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.6f);
                        ExecuteEnemyAttack(enemy, monster);
                        if (playerHero.currentLust >= 100) yield break;
                    }
                }
            }
        }

        if (enemyHero != null) enemyHero.OnTurnEnd();

        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnEnd, ZoneType.EnemyField);

        yield return new WaitForSeconds(0.5f);
        StartNewPlayerTurn();
    }

    void ExecuteEnemyAttack(CardDisplay enemy, MonsterCardData monster)
    {
        CardDisplay target = FindTauntTarget();
        if (target == null && playerField.childCount > 0)
        {
            int randomIndex = Random.Range(0, playerField.childCount);
            target = playerField.GetChild(randomIndex).GetComponent<CardDisplay>();
        }

        if (target != null)
        {
            enemy.OnAttack(target);
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
            if (cd != null && cd.HasKeyword(Keyword.Taunt)) return cd;
        }
        return null;
    }

    void StartNewPlayerTurn()
    {
        isEnemyTurn = false;
        turnCount++;

        // 최대 마나 증가
        if (maxMana < 10) maxMana++;

        // ★ 마나 잠금 체크 및 마나 회복 로직 ★
        if (manaLockTurnCount > 0)
        {
            currentMana = 0;
            manaLockTurnCount--;
            Debug.Log($"마나 잠금 지속 중... 남은 턴: {manaLockTurnCount}");
        }
        else
        {
            currentMana = maxMana;
        }

        currentFocus = maxFocus;

        if (DeckManager.instance != null) DeckManager.instance.DrawCard();
        if (playerHero != null) playerHero.OnTurnStart();

        foreach (CardDisplay card in playerField.GetComponentsInChildren<CardDisplay>())
            card.OnTurnStart();

        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnStart, ZoneType.PlayerField);

        UpdateUI();
    }

    public void UpdateUI()
    {
        // 마나 텍스트 표시 (잠금 상태일 때 시각적 피드백 추가 가능)
        if (manaText != null)
        {
            manaText.text = (manaLockTurnCount > 0) ? $"0 / {maxMana} (잠김)" : $"{currentMana} / {maxMana}";
            manaText.color = (manaLockTurnCount > 0) ? Color.red : Color.white;
        }

        if (focusText != null) focusText.text = $"Focus: {currentFocus}";
        if (turnText != null) turnText.text = $"Turn {turnCount}";

        // 마나 구슬 업데이트
        for (int i = 0; i < manaIcons.Count; i++)
        {
            if (i < maxMana)
            {
                manaIcons[i].transform.parent.gameObject.SetActive(true);
                // 잠금 상태면 구슬을 다 끔, 아니면 현재 마나만큼 켬
                manaIcons[i].enabled = (manaLockTurnCount <= 0 && i < currentMana);
                // 잠금 상태일 때 구슬 색상을 어둡게 변경
                manaIcons[i].color = (manaLockTurnCount > 0) ? new Color(0.3f, 0.3f, 0.3f) : Color.white;
            }
            else
            {
                manaIcons[i].transform.parent.gameObject.SetActive(false);
            }
        }

        if (playerHero != null) playerHero.UpdateUI();
        if (enemyHero != null) enemyHero.UpdateUI();
    }

    public List<CardDisplay> GetCardsInZone(ZoneType zone)
    {
        List<CardDisplay> cards = new List<CardDisplay>();
        Transform targetZone = (zone == ZoneType.PlayerField) ? playerField : enemyField;
        if (targetZone != null) cards.AddRange(targetZone.GetComponentsInChildren<CardDisplay>());
        return cards;
    }
}