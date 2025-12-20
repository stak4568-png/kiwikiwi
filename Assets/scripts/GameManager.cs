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

    [Header("플레이어 상태 (HP/Lust)")]
    public int playerHealth = 30;
    public int playerLust = 0;
    public int turnCount = 1;
    public bool isEnemyTurn = false;

    [Header("적 영웅 상태")]
    public int enemyHealth = 30;

    [Header("UI 연결 (텍스트)")]
    public TMP_Text manaText;
    public TMP_Text focusText;
    public TMP_Text healthText;
    public TMP_Text lustText;
    public TMP_Text enemyHealthText;
    public TMP_Text turnText;

    [Header("UI 연결 (마나 아이콘)")]
    public GameObject manaIconPrefab;
    public Transform manaContainer;
    private List<Image> manaIcons = new List<Image>();

    [Header("Zone References")]
    public Transform playerField;
    public Transform enemyField;

    [Header("Hero References")]
    public HeroPortrait playerHeroPortrait;
    public HeroPortrait enemyHeroPortrait;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 마나 아이콘 초기화 (최대 10개 기준)
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

        // Zone 참조 자동 찾기 (씬에 해당 이름의 오브젝트가 있어야 함)
        if (playerField == null) playerField = GameObject.Find("PlayerArea")?.transform;
        if (enemyField == null) enemyField = GameObject.Find("EnemyArea")?.transform;

        // 영웅 초상화 참조 자동 찾기
        if (playerHeroPortrait == null) playerHeroPortrait = HeroPortrait.playerHero;
        if (enemyHeroPortrait == null) enemyHeroPortrait = HeroPortrait.enemyHero;

        UpdateUI();
    }

    // === 자원 관리 함수 ===

    public bool TrySpendMana(int amount)
    {
        if (currentMana >= amount)
        {
            currentMana -= amount;
            UpdateUI();
            return true;
        }
        return false;
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

    public void GainMana(int amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
        UpdateUI();
    }

    public void GainFocus(int amount)
    {
        currentFocus += amount;
        UpdateUI();
    }

    // === 데미지 및 흥분도 처리 ===

    /// <summary>
    /// 일반적인 유혹 공격 데미지 (현재 마나 수치만큼 방어)
    /// </summary>
    public void TakeLustDamage(int monsterLustAtk)
    {
        int finalDamage = Mathf.Max(0, monsterLustAtk - currentMana);
        AddLustDirectly(finalDamage);
    }

    /// <summary>
    /// 흥분도를 직접 증가 (방어 무시 혹은 계산 완료된 데미지)
    /// </summary>
    public void AddLustDirectly(int amount)
    {
        playerLust += amount;
        Debug.Log($"흥분도 증가: +{amount} (현재: {playerLust})");

        if (playerLust >= 100)
        {
            playerLust = 100;
            OnClimax();
        }
        UpdateUI();
    }

    public void TakePlayerDamage(int amount)
    {
        playerHealth -= amount;
        if (playerHealth <= 0)
        {
            playerHealth = 0;
            OnPlayerDefeat();
        }
        UpdateUI();
    }

    public void HealPlayer(int amount)
    {
        playerHealth += amount;
        UpdateUI();
    }

    public void DamageEnemyHero(int amount)
    {
        enemyHealth -= amount;
        if (enemyHealth <= 0)
        {
            enemyHealth = 0;
            OnEnemyDefeat();
        }
        UpdateUI();
    }

    // === 게임 결과 처리 ===

    void OnClimax()
    {
        Debug.Log("<color=magenta>★★★ CLIMAX 이벤트 발생! ★★★</color>");
        // TODO: 클라이막스 컷신이나 특수 연출 연결
    }

    void OnPlayerDefeat()
    {
        Debug.Log("플레이어 패배...");
    }

    void OnEnemyDefeat()
    {
        Debug.Log("플레이어 승리!");
    }

    // === 턴 관리 및 적 AI ===

    public void EndTurn()
    {
        if (isEnemyTurn) return;

        // 플레이어 턴 종료 시 효과 발동
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnEnd, ZoneType.PlayerField);

        StartCoroutine(EnemyPhase());
    }

    IEnumerator EnemyPhase()
    {
        isEnemyTurn = true;
        Debug.Log("── 적 턴 시작 ──");

        // 적 영웅 턴 시작 처리
        if (enemyHeroPortrait != null)
            enemyHeroPortrait.OnTurnStart();

        // 적 턴 시작 효과 발동
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnStart, ZoneType.EnemyField);

        yield return new WaitForSeconds(0.5f);

        // [적 영웅 유혹 공격] 필드 상황과 관계없이 매 턴 유혹 공격 가능
        if (enemyHeroPortrait != null && enemyHeroPortrait.CanSeduceAttack())
        {
            bool heroSeduceDone = false;
            if (SeduceEventManager.instance != null)
            {
                SeduceEventManager.instance.StartSeduceEvent(enemyHeroPortrait, () => {
                    heroSeduceDone = true;
                });
                // 플레이어가 선택을 마칠 때까지 대기
                yield return new WaitUntil(() => heroSeduceDone);
            }
            else
            {
                // SeduceEventManager가 없으면 직접 실행
                yield return new WaitForSeconds(0.5f);
                enemyHeroPortrait.ExecuteSeduceAttack();
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (enemyField != null)
        {
            CardDisplay[] enemies = enemyField.GetComponentsInChildren<CardDisplay>();

            foreach (CardDisplay enemy in enemies)
            {
                if (enemy.cardData is MonsterCardData monster)
                {
                    // [핵심] 플레이어 필드에 하수인이 없으면 유혹 공격 이벤트 발생
                    bool isSeduceAttack = (playerField.childCount == 0);

                    if (isSeduceAttack)
                    {
                        bool eventDone = false;
                        if (SeduceEventManager.instance != null)
                        {
                            SeduceEventManager.instance.StartSeduceEvent(enemy, () => {
                                eventDone = true;
                            });
                            // 플레이어가 선택을 마칠 때까지 대기
                            yield return new WaitUntil(() => eventDone);
                        }
                    }
                    else
                    {
                        // 필드에 하수인이 있으면 일반 공격 (도발 체크 포함)
                        yield return new WaitForSeconds(0.8f);
                        ExecuteEnemyAttack(enemy, monster);
                    }
                }
            }
        }

        // 적 영웅 턴 종료 처리
        if (enemyHeroPortrait != null)
            enemyHeroPortrait.OnTurnEnd();

        // 적 턴 종료 효과 발동
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnEnd, ZoneType.EnemyField);

        yield return new WaitForSeconds(0.5f);
        StartNewPlayerTurn();
    }

    void ExecuteEnemyAttack(CardDisplay enemy, MonsterCardData monster)
    {
        // 도발 카드 먼저 찾기
        CardDisplay target = FindTauntTarget();

        // 도발 없으면 필드 위 랜덤 하수인 공격
        if (target == null && playerField.childCount > 0)
        {
            int randomIndex = Random.Range(0, playerField.childCount);
            target = playerField.GetChild(randomIndex).GetComponent<CardDisplay>();
        }

        if (target != null)
        {
            Debug.Log($"{monster.cardName}이(가) {target.cardData.cardName}을(를) 공격!");
            ExecuteCombat(enemy, target);
        }
    }

    CardDisplay FindTauntTarget()
    {
        if (playerField == null) return null;
        CardDisplay[] allies = playerField.GetComponentsInChildren<CardDisplay>();
        foreach (var card in allies)
        {
            if (card.HasKeyword(Keyword.Taunt)) return card;
        }
        return null;
    }

    void ExecuteCombat(CardDisplay attacker, CardDisplay defender)
    {
        attacker.OnAttack(defender);
        defender.TakeDamage(attacker.currentAttack);
        attacker.TakeDamage(defender.currentAttack);
    }

    void StartNewPlayerTurn()
    {
        isEnemyTurn = false;
        turnCount++;

        // 마나 및 집중력 회복
        if (maxMana < 10) maxMana++;
        currentMana = maxMana;
        currentFocus = maxFocus;

        Debug.Log($"── 플레이어 턴 {turnCount} 시작 ──");

        // 카드 드로우
        if (DeckManager.instance != null) DeckManager.instance.DrawCard();

        // 플레이어 영웅 턴 시작 처리
        if (playerHeroPortrait != null)
            playerHeroPortrait.OnTurnStart();

        // 아군 카드들 상태 갱신
        if (playerField != null)
        {
            foreach (CardDisplay card in playerField.GetComponentsInChildren<CardDisplay>())
                card.OnTurnStart();
        }

        // 플레이어 턴 시작 효과 발동
        if (EffectManager.instance != null)
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnStart, ZoneType.PlayerField);

        UpdateUI();
    }

    // === UI 업데이트 ===

    public void UpdateUI()
    {
        if (manaText != null) manaText.text = $"{currentMana} / {maxMana}";
        if (focusText != null) focusText.text = $"Focus: {currentFocus} / {maxFocus}";
        if (healthText != null) healthText.text = $"HP: {playerHealth}";
        if (lustText != null) lustText.text = $"Lust: {playerLust}%";
        if (enemyHealthText != null) enemyHealthText.text = $"Enemy HP: {enemyHealth}";
        if (turnText != null) turnText.text = $"Turn {turnCount}";

        // 마나 구슬 UI 업데이트
        for (int i = 0; i < manaIcons.Count; i++)
        {
            if (i < maxMana)
            {
                manaIcons[i].transform.parent.gameObject.SetActive(true);
                manaIcons[i].enabled = (i < currentMana);
            }
            else
            {
                manaIcons[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }

    public List<CardDisplay> GetCardsInZone(ZoneType zone)
    {
        List<CardDisplay> cards = new List<CardDisplay>();
        Transform targetZone = (zone == ZoneType.PlayerField) ? playerField : enemyField;
        if (targetZone != null) cards.AddRange(targetZone.GetComponentsInChildren<CardDisplay>());
        return cards;
    }
}