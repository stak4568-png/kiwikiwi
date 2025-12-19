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
    public GameObject manaIconPrefab;    // 마나 보석 프리팹
    public Transform manaContainer;      // 보석이 담길 곳 (Horizontal Layout Group 필요)
    private List<Image> manaIcons = new List<Image>(); // 보석 내 'FullIcon' 이미지들

    void Awake()
    {
        // 싱글톤 설정
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // 1. 10개의 마나 아이콘 미리 생성 (오브젝트 풀링)
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
        UpdateUI();
    }

    // 마나 사용 시도
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

    // 집중력 사용 시도
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

    // 플레이어 유혹 데미지 처리 (마나 방어 포함)
    public void TakeLustDamage(int monsterLustAtk)
    {
        // [기획 5-A] 받는 흥분도 = 적 유혹 공격력 - 내 남은 마나
        int finalDamage = Mathf.Max(0, monsterLustAtk - currentMana);
        playerLust += finalDamage;

        if (playerLust >= 100)
        {
            playerLust = 100;
            Debug.Log("★ CLIMAX! 흥분도가 최대치입니다.");
        }
        UpdateUI();
    }

    // 적 영웅에게 데미지 입히기
    public void DamageEnemyHero(int amount)
    {
        enemyHealth -= amount;
        Debug.Log($"적 영웅에게 {amount}의 피해! 남은 HP: {enemyHealth}");
        if (enemyHealth <= 0)
        {
            enemyHealth = 0;
            Debug.Log("승리! 적 영웅을 격파했습니다.");
        }
        UpdateUI();
    }

    // 턴 종료 버튼 누를 때 호출
    public void EndTurn()
    {
        if (isEnemyTurn) return;
        StartCoroutine(EnemyPhase());
    }

    // 적의 공격 페이즈 코루틴
    IEnumerator EnemyPhase()
    {
        isEnemyTurn = true;
        Debug.Log("적 공격 페이즈 시작...");

        GameObject enemyArea = GameObject.Find("EnemyArea");
        GameObject playerArea = GameObject.Find("PlayerArea");

        if (enemyArea != null && playerArea != null)
        {
            CardDisplay[] enemies = enemyArea.GetComponentsInChildren<CardDisplay>();
            bool isPlayerEmpty = playerArea.transform.childCount == 0;

            foreach (CardDisplay enemy in enemies)
            {
                if (enemy.cardData is MonsterCardData monster)
                {
                    yield return new WaitForSeconds(0.8f);

                    if (isPlayerEmpty)
                    {
                        // 필드가 비었을 때만 유혹 공격
                        TakeLustDamage(monster.lustAttack);
                    }
                    else
                    {
                        // 필드에 몬스터가 있으면 (자동으로 때리는 로직은 아직 미구현)
                        Debug.Log($"{monster.cardName}이(가) 내 필드를 노려봅니다.");
                    }
                }
            }
        }

        yield return new WaitForSeconds(1f);
        StartNewPlayerTurn();
    }

    // 새 턴 시작 초기화
    void StartNewPlayerTurn()
    {
        isEnemyTurn = false;
        turnCount++;
        if (maxMana < 10) maxMana++;
        currentMana = maxMana;
        currentFocus = maxFocus;

        // ★ 매 턴 시작 시 카드 한 장 드로우! ★
        if (DeckManager.instance != null)
        {
            DeckManager.instance.DrawCard();
        }

        UpdateUI();
    }
    // ★ 모든 UI 요소를 한 번에 업데이트하는 단 하나의 함수 ★
    public void UpdateUI()
    {
        // 텍스트 업데이트
        if (manaText != null) manaText.text = $"{currentMana} / {maxMana}";
        if (focusText != null) focusText.text = $"Focus: {currentFocus} / {maxFocus}";
        if (healthText != null) healthText.text = $"HP: {playerHealth}";
        if (lustText != null) lustText.text = $"Lust: {playerLust}%";
        if (enemyHealthText != null) enemyHealthText.text = $"Enemy HP: {enemyHealth}";
        if (turnText != null) turnText.text = $"Turn {turnCount}";

        // 마나 아이콘 보석 업데이트
        for (int i = 0; i < manaIcons.Count; i++)
        {
            if (i < maxMana)
            {
                manaIcons[i].transform.parent.gameObject.SetActive(true);
                // 마나가 남아있으면 보석 색을 켜고(FullIcon), 없으면 끔
                manaIcons[i].enabled = (i < currentMana);
            }
            else
            {
                manaIcons[i].transform.parent.gameObject.SetActive(false);
            }
        }
    }
}