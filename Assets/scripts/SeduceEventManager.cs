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
    public Button blockButton;  // 마나로 저항
    public Button endureButton; // 그냥 맞기

    [Header("Button Texts")]
    public TMP_Text blockButtonText;

    private CardDisplay currentAttacker;
    private int currentLustAtk;
    private Action onComplete; // 공격 종료 후 적 턴을 계속 진행하기 위한 콜백

    void Awake()
    {
        instance = this;
        seducePanel.SetActive(false);
    }

    /// <summary>
    /// 유혹 이벤트 시작
    /// </summary>
    public void StartSeduceEvent(CardDisplay attacker, Action callback)
    {
        currentAttacker = attacker;
        onComplete = callback;

        if (attacker.cardData is MonsterCardData monster)
        {
            currentLustAtk = monster.lustAttack;

            // 1. UI 세팅
            seducePanel.SetActive(true);
            monsterNameText.text = monster.cardName;
            // 유혹 시에는 해금 여부와 상관없이 원본 일러스트를 보여주어 위기감 조성 가능
            monsterArt.sprite = attacker.isArtRevealed ? monster.originalArt : monster.censoredArt;
            descriptionText.text = $"{monster.cardName}의 유혹 공격! ({currentLustAtk} Lust)";

            // 2. 버튼 세팅
            int currentMana = GameManager.instance.currentMana;
            blockButtonText.text = $"마나로 저항 (남은 마나: {currentMana})";

            // 마나가 없어도 버튼은 누를 수 있게 하되, 효율은 0이 됨
            blockButton.onClick.RemoveAllListeners();
            blockButton.onClick.AddListener(OnBlockClicked);

            endureButton.onClick.RemoveAllListeners();
            endureButton.onClick.AddListener(OnEndureClicked);
        }
    }

    // [선택지 1] 마나로 저항
    void OnBlockClicked()
    {
        int manaUsed = GameManager.instance.currentMana;
        int finalDamage = Mathf.Max(0, currentLustAtk - manaUsed);

        // 마나 전부 소모
        GameManager.instance.TrySpendMana(manaUsed);

        Debug.Log($"마나 {manaUsed}를 사용하여 유혹 저항! 최종 데미지: {finalDamage}");
        GameManager.instance.AddLustDirectly(finalDamage);

        FinishEvent();
    }

    // [선택지 2] 그냥 맞기 (마나 보존)
    void OnEndureClicked()
    {
        Debug.Log("유혹을 그대로 받아들임. 마나 보존.");
        GameManager.instance.AddLustDirectly(currentLustAtk);

        FinishEvent();
    }

    void FinishEvent()
    {
        seducePanel.SetActive(false);
        onComplete?.Invoke(); // 다음 적의 공격으로 넘어감
    }
}