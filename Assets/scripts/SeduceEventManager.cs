using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class SeduceEventManager : MonoBehaviour
{
    public static SeduceEventManager instance;

    [Header("UI Panels")]
    public GameObject seducePanel;
    public Image monsterArt;
    public TMP_Text monsterNameText;
    public TMP_Text descriptionText;

    [Header("Buttons")]
    public Button blockButton;  // 막기 버튼
    public Button endureButton; // 버티기 버튼

    [Header("Button Texts")]
    public TMP_Text blockButtonText;

    private CardDisplay currentAttacker;
    private HeroPortrait currentHeroAttacker;  // 영웅 공격자
    private int currentLustAtk;
    private Action onComplete;

    void Awake()
    {
        instance = this;
        seducePanel.SetActive(false);
    }

    /// <summary>
    /// 유혹 이벤트 시작 (카드 공격자)
    /// </summary>
    public void StartSeduceEvent(CardDisplay attacker, Action callback)
    {
        currentAttacker = attacker;
        currentHeroAttacker = null;
        onComplete = callback;

        if (attacker.cardData is MonsterCardData monster)
        {
            currentLustAtk = monster.lustAttack;

            // 1. UI 설정
            seducePanel.SetActive(true);
            monsterNameText.text = monster.cardName;
            monsterArt.sprite = attacker.isArtRevealed ? monster.originalArt : monster.censoredArt;
            descriptionText.text = $"{monster.cardName}의 유혹 공격! ({currentLustAtk} Lust)";

            // 2. 버튼 설정
            SetupButtons();
        }
    }

    /// <summary>
    /// 유혹 이벤트 시작 (영웅 공격자)
    /// </summary>
    public void StartSeduceEvent(HeroPortrait attacker, Action callback)
    {
        currentAttacker = null;
        currentHeroAttacker = attacker;
        onComplete = callback;

        // HeroPortrait의 메서드로 유혹력 가져오기
        currentLustAtk = attacker.GetSeducePower();

        // HeroData가 있으면 데이터에서, 없으면 기본값 사용
        string heroName = "적 영웅";
        Sprite heroPortrait = null;
        string seduceDesc = "";

        if (attacker.heroData != null)
        {
            heroName = attacker.heroData.heroName;
            heroPortrait = attacker.heroData.portrait;
            seduceDesc = attacker.heroData.seduceDescription;
        }

        // 1. UI 설정
        seducePanel.SetActive(true);
        monsterNameText.text = heroName;

        // 영웅 초상화 사용
        if (heroPortrait != null)
            monsterArt.sprite = heroPortrait;

        // 유혹 설명이 있으면 사용, 없으면 기본 텍스트
        string desc = !string.IsNullOrEmpty(seduceDesc)
            ? seduceDesc
            : $"{heroName}의 유혹 공격!";
        descriptionText.text = $"{desc} ({currentLustAtk} Lust)";

        // 2. 버튼 설정
        SetupButtons();
    }

    /// <summary>
    /// 버튼 이벤트 설정
    /// </summary>
    void SetupButtons()
    {
        int currentMana = GameManager.instance.currentMana;
        blockButtonText.text = $"막기 (마나 사용: {currentMana})";

        blockButton.onClick.RemoveAllListeners();
        blockButton.onClick.AddListener(OnBlockClicked);

        endureButton.onClick.RemoveAllListeners();
        endureButton.onClick.AddListener(OnEndureClicked);
    }

    // [선택지 1] 막기
    void OnBlockClicked()
    {
        int manaUsed = GameManager.instance.currentMana;
        int finalDamage = Mathf.Max(0, currentLustAtk - manaUsed);

        // 마나 전부 소모
        GameManager.instance.TrySpendMana(manaUsed);

        Debug.Log($"마나 {manaUsed}를 사용하여 유혹 방어! 최종 데미지: {finalDamage}");
        GameManager.instance.AddLustDirectly(finalDamage);

        FinishEvent();
    }

    // [선택지 2] 버티기 (전부 받기)
    void OnEndureClicked()
    {
        Debug.Log("유혹을 그대로 받아들임. 전부 피해.");
        GameManager.instance.AddLustDirectly(currentLustAtk);

        FinishEvent();
    }

    void FinishEvent()
    {
        seducePanel.SetActive(false);
        currentAttacker = null;
        currentHeroAttacker = null;
        onComplete?.Invoke();
    }
}
