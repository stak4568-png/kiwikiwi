// GameManager.cs
// 게임 상태 관리 - 효과 시스템 통합 버전

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

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 마나 아이콘 초기화
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

        // Zone 참조 자동 찾기
        if (playerField == null)
            playerField = GameObject.Find("PlayerArea")?.transform;
        if (enemyField == null)
            enemyField = GameObject.Find("EnemyArea")?.transform;

        UpdateUI();
    }

    // === 자원 관리 ===

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

    // === 데미지 처리 ===

    public void TakeLustDamage(int monsterLustAtk)
    {
        int finalDamage = Mathf.Max(0, monsterLustAtk - currentMana);
        playerLust += finalDamage;

        if (playerLust >= 100)
        {
            playerLust = 100;
            Debug.Log("★ CLIMAX! 흥분도가 최대치입니다.");
            OnClimax();
        }
        UpdateUI();
    }

    public void TakePlayerDamage(int amount)
    {
        playerHealth -= amount;
        Debug.Log($"플레이어가 {amount}의 피해를 입음! 남은 HP: {playerHealth}");
        
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
        Debug.Log($"플레이어 체력 {amount} 회복! 현재: {playerHealth}");
        UpdateUI();
    }

    public void DamageEnemyHero(int amount)
    {
        enemyHealth -= amount;
        Debug.Log($"적 영웅에게 {amount}의 피해! 남은 HP: {enemyHealth}");
        
        if (enemyHealth <= 0)
        {
            enemyHealth = 0;
            OnEnemyDefeat();
        }
        UpdateUI();
    }

    // === 게임 종료 처리 ===

    void OnClimax()
    {
        // TODO: 클라이막스 연출, 게임 오버 처리
        Debug.Log("★★★ CLIMAX - 특수 이벤트 발동! ★★★");
    }

    void OnPlayerDefeat()
    {
        Debug.Log("패배...");
        // TODO: 게임 오버 화면
    }

    void OnEnemyDefeat()
    {
        Debug.Log("승리! 적 영웅을 격파했습니다.");
        // TODO: 승리 화면
    }

    // === 턴 관리 ===

    public void EndTurn()
    {
        if (isEnemyTurn) return;

        // 플레이어 턴 종료 효과 발동
        TriggerTurnEndEffects();

        StartCoroutine(EnemyPhase());
    }

    void TriggerTurnEndEffects()
    {
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnEnd, ZoneType.PlayerField);
        }
    }

    IEnumerator EnemyPhase()
    {
        isEnemyTurn = true;
        Debug.Log("── 적 턴 시작 ──");

        // 적 턴 시작 효과
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnStart, ZoneType.EnemyField);
        }

        yield return new WaitForSeconds(0.5f);

        // 적 필드의 모든 카드가 공격
        if (enemyField != null)
        {
            CardDisplay[] enemies = enemyField.GetComponentsInChildren<CardDisplay>();
            
            foreach (CardDisplay enemy in enemies)
            {
                if (enemy.cardData is MonsterCardData monster)
                {
                    yield return new WaitForSeconds(0.8f);
                    ExecuteEnemyAttack(enemy, monster);
                }
            }
        }

        // 적 턴 종료 효과
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnEnemyTurnEnd, ZoneType.EnemyField);
        }

        yield return new WaitForSeconds(0.5f);
        StartNewPlayerTurn();
    }

    void ExecuteEnemyAttack(CardDisplay enemy, MonsterCardData monster)
    {
        // 플레이어 필드에 도발 카드가 있는지 확인
        CardDisplay tauntTarget = FindTauntTarget();
        
        if (tauntTarget != null)
        {
            // 도발 카드 공격
            Debug.Log($"{monster.cardName}이(가) 도발 카드 {tauntTarget.cardData.cardName}을(를) 공격!");
            ExecuteCombat(enemy, tauntTarget);
        }
        else if (playerField != null && playerField.childCount > 0)
        {
            // 아무 카드나 공격 (AI 개선 가능)
            CardDisplay target = playerField.GetChild(0).GetComponent<CardDisplay>();
            if (target != null)
            {
                Debug.Log($"{monster.cardName}이(가) {target.cardData.cardName}을(를) 공격!");
                ExecuteCombat(enemy, target);
            }
        }
        else
        {
            // 필드가 비었으면 유혹 공격
            TakeLustDamage(monster.lustAttack);
            Debug.Log($"{monster.cardName}의 유혹 공격! 흥분도 +{monster.lustAttack}");
        }
    }

    CardDisplay FindTauntTarget()
    {
        if (playerField == null) return null;

        CardDisplay[] allies = playerField.GetComponentsInChildren<CardDisplay>();
        foreach (var card in allies)
        {
            if (card.HasKeyword(Keyword.Taunt))
                return card;
        }
        return null;
    }

    void ExecuteCombat(CardDisplay attacker, CardDisplay defender)
    {
        // 공격 효과 발동
        attacker.OnAttack(defender);

        // 상호 데미지
        defender.TakeDamage(attacker.currentAttack);
        attacker.TakeDamage(defender.currentAttack);

        // 생명력 흡수 체크
        if (attacker.HasKeyword(Keyword.Lifesteal))
        {
            HealPlayer(attacker.currentAttack);
            Debug.Log($"{attacker.cardData.cardName}의 생명력 흡수로 {attacker.currentAttack} 회복!");
        }

        // 독 체크
        if (attacker.HasKeyword(Keyword.Poison) && defender.currentHealth > 0)
        {
            Debug.Log($"{attacker.cardData.cardName}의 독으로 {defender.cardData.cardName} 파괴!");
            defender.TakeDamage(9999); // 즉사
        }
        if (defender.HasKeyword(Keyword.Poison) && attacker.currentHealth > 0)
        {
            Debug.Log($"{defender.cardData.cardName}의 독으로 {attacker.cardData.cardName} 파괴!");
            attacker.TakeDamage(9999);
        }
    }

    void StartNewPlayerTurn()
    {
        isEnemyTurn = false;
        turnCount++;

        // 마나/집중력 회복
        if (maxMana < 10) maxMana++;
        currentMana = maxMana;
        currentFocus = maxFocus;

        Debug.Log($"── 플레이어 턴 {turnCount} 시작 ──");

        // 카드 드로우
        if (DeckManager.instance != null)
        {
            DeckManager.instance.DrawCard();
        }

        // 플레이어 필드 카드들 턴 시작 처리
        if (playerField != null)
        {
            CardDisplay[] allies = playerField.GetComponentsInChildren<CardDisplay>();
            foreach (var card in allies)
            {
                card.OnTurnStart();
            }
        }

        // 턴 시작 효과 발동
        if (EffectManager.instance != null)
        {
            EffectManager.instance.TriggerGlobalTiming(EffectTiming.OnTurnStart, ZoneType.PlayerField);
        }

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

        // 마나 아이콘 업데이트
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

    // === 유틸리티 ===

    /// <summary>
    /// 특정 존의 모든 카드 가져오기
    /// </summary>
    public List<CardDisplay> GetCardsInZone(ZoneType zone)
    {
        List<CardDisplay> cards = new List<CardDisplay>();
        Transform targetZone = zone switch
        {
            ZoneType.PlayerField => playerField,
            ZoneType.EnemyField => enemyField,
            _ => null
        };

        if (targetZone != null)
        {
            cards.AddRange(targetZone.GetComponentsInChildren<CardDisplay>());
        }
        return cards;
    }
}
