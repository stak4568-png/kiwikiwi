using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SeduceEventManager : MonoBehaviour
{
    public static SeduceEventManager instance;

    [Header("UI Panels")]
    public GameObject seducePanel;       // 유혹 이벤트 전체 패널
    public Image monsterArt;             // 이벤트 전용 일러스트가 표시될 곳
    public TMP_Text monsterNameText;     // 공격자 이름
    public TMP_Text descriptionText;     // 유혹 공격 설명 텍스트

    [Header("Buttons")]
    public Button blockButton;           // 마나로 저항 버튼
    public Button endureButton;          // 그대로 받아들이기 버튼

    [Header("Button Texts")]
    public TMP_Text blockButtonText;     // "마나로 저항 (보유 마나: X)"

    private int currentLustAtk;
    private Action onComplete;           // 이벤트 종료 시 실행할 콜백 (적 턴 재개용)

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);

        if (seducePanel != null) seducePanel.SetActive(false);
    }

    // --- 1. 하수인(Monster)용 유혹 이벤트 ---
    public void StartSeduceEvent(CardDisplay attacker, Action callback)
    {
        onComplete = callback;

        if (attacker.cardData is MonsterCardData monster)
        {
            currentLustAtk = monster.lustAttack;

            seducePanel.SetActive(true);
            monsterNameText.text = monster.cardName;

            // [일러스트 설정] 전용 이벤트 아트 -> 해금 아트 -> 검열 아트 순서
            if (monsterArt != null)
            {
                // MonsterCardData에 seduceEventArt 변수가 있다고 가정
                // 만약 없다면 일반 아트 사용
                monsterArt.sprite = monster.seduceEventArt ??
                                   (attacker.isArtRevealed ? (monster.originalArt ?? monster.censoredArt) : monster.censoredArt);
            }

            descriptionText.text = $"{monster.cardName}의 유혹 공격! \n({currentLustAtk} Lust)";

            SetupButtons();
        }
    }

    // --- 2. 영웅(Hero)용 유혹 이벤트 ---
    public void StartHeroSeduceEvent(HeroPortrait attacker, Action callback)
    {
        onComplete = callback;

        if (attacker.heroData != null)
        {
            currentLustAtk = attacker.heroData.seducePower;

            seducePanel.SetActive(true);
            monsterNameText.text = attacker.heroData.heroName;

            // [일러스트 설정] 영웅 전용 이벤트 아트 -> 일반 초상화 순서
            if (monsterArt != null)
            {
                if (attacker.heroData.seduceEventArt != null)
                {
                    monsterArt.sprite = attacker.heroData.seduceEventArt;
                }
                else
                {
                    monsterArt.sprite = attacker.heroData.portrait;
                }
            }

            descriptionText.text = $"{attacker.heroData.heroName}의 치명적인 유혹! \n({currentLustAtk} Lust)";

            SetupButtons();
        }
    }

    // --- 공통 버튼 설정 ---
    private void SetupButtons()
    {
        UpdateButtonsUI();

        blockButton.onClick.RemoveAllListeners();
        blockButton.onClick.AddListener(OnBlockClicked);

        endureButton.onClick.RemoveAllListeners();
        endureButton.onClick.AddListener(OnEndureClicked);
    }

    void UpdateButtonsUI()
    {
        if (GameManager.instance == null) return;

        int currentMana = GameManager.instance.currentMana;
        if (blockButtonText != null)
        {
            blockButtonText.text = $"마나로 저항\n(보유 마나: {currentMana})";
        }
    }

    // [선택지 1] 마나로 저항
    void OnBlockClicked()
    {
        int currentMana = GameManager.instance.currentMana;
        int finalDamage = Mathf.Max(0, currentLustAtk - currentMana);

        // 마나 차감
        GameManager.instance.TrySpendMana(currentMana);

        // 영웅에게 데미지 전달 (이미 마나 방어 계산이 끝났으므로 ignoreMana: true)
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.TakeLustDamage(finalDamage, true);
        }

        FinishEvent();
    }

    // [선택지 2] 그대로 받아들이기
    void OnEndureClicked()
    {
        if (HeroPortrait.playerHero != null)
        {
            HeroPortrait.playerHero.TakeLustDamage(currentLustAtk, true);
        }

        FinishEvent();
    }

    void FinishEvent()
    {
        if (seducePanel != null) seducePanel.SetActive(false);

        // GameManager의 코루틴 등에 신호를 주어 적 턴을 계속 진행함
        onComplete?.Invoke();
    }
}