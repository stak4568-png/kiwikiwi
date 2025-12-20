using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 정화 UI
/// 페티시 정화 시스템 UI
/// </summary>
public class PurificationUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject purificationPanel;
    public CanvasGroup canvasGroup;

    [Header("페티시 리스트")]
    public Transform fetishListContainer;
    public GameObject fetishItemPrefab;

    [Header("선택된 페티시 정보")]
    public GameObject selectedFetishPanel;
    public TMP_Text fetishNameText;
    public TMP_Text fetishDescriptionText;
    public TMP_Text intensityText;
    public TMP_Text costText;
    public TMP_Text successChanceText;

    [Header("정화 방법 선택")]
    public Transform methodContainer;
    public GameObject methodButtonPrefab;

    [Header("액션 버튼")]
    public Button purifyButton;
    public Button cancelButton;

    [Header("결과 패널")]
    public GameObject resultPanel;
    public TMP_Text resultText;
    public Button closeResultButton;

    // 상태
    private List<FetishType> _availableFetishes;
    private FetishType _selectedFetish = FetishType.None;
    private PurificationMethod _selectedMethod;

    void Start()
    {
        if (purificationPanel != null)
            purificationPanel.SetActive(false);

        if (selectedFetishPanel != null)
            selectedFetishPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (purifyButton != null)
            purifyButton.onClick.AddListener(OnPurifyClicked);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Hide);

        if (closeResultButton != null)
            closeResultButton.onClick.AddListener(CloseResult);
    }

    /// <summary>
    /// 정화 UI 표시
    /// </summary>
    public void Show(List<FetishType> fetishes)
    {
        if (fetishes == null || fetishes.Count == 0)
        {
            Debug.Log("정화할 페티시가 없습니다.");
            return;
        }

        _availableFetishes = fetishes;
        _selectedFetish = FetishType.None;
        _selectedMethod = null;

        if (purificationPanel != null)
            purificationPanel.SetActive(true);

        if (selectedFetishPanel != null)
            selectedFetishPanel.SetActive(false);

        RefreshFetishList();
        RefreshMethodList();
    }

    /// <summary>
    /// 정화 UI 숨기기
    /// </summary>
    public void Hide()
    {
        if (purificationPanel != null)
            purificationPanel.SetActive(false);

        if (selectedFetishPanel != null)
            selectedFetishPanel.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        _selectedFetish = FetishType.None;
        _selectedMethod = null;
    }

    /// <summary>
    /// 페티시 리스트 새로고침
    /// </summary>
    void RefreshFetishList()
    {
        if (fetishListContainer == null || fetishItemPrefab == null || _availableFetishes == null)
            return;

        // 기존 항목 제거
        foreach (Transform child in fetishListContainer)
        {
            Destroy(child.gameObject);
        }

        // 페티시 항목 생성
        foreach (var fetish in _availableFetishes)
        {
            if (fetish == FetishType.None) continue;

            GameObject itemObj = Instantiate(fetishItemPrefab, fetishListContainer);
            Button itemButton = itemObj.GetComponent<Button>();
            TMP_Text itemText = itemObj.GetComponentInChildren<TMP_Text>();

            if (itemText != null)
            {
                int intensity = PlayerFetishState.instance?.GetIntensity(fetish) ?? 0;
                itemText.text = $"{GetFetishName(fetish)} (강도: {intensity})";
            }

            if (itemButton != null)
            {
                FetishType capturedFetish = fetish; // 클로저를 위한 캡처
                itemButton.onClick.AddListener(() => OnFetishSelected(capturedFetish));
            }
        }
    }

    /// <summary>
    /// 정화 방법 리스트 새로고침
    /// </summary>
    void RefreshMethodList()
    {
        if (methodContainer == null || methodButtonPrefab == null || PurificationManager.instance == null)
            return;

        // 기존 버튼 제거
        foreach (Transform child in methodContainer)
        {
            Destroy(child.gameObject);
        }

        // 정화 방법 버튼 생성
        foreach (var method in PurificationManager.instance.purificationMethods)
        {
            if (method == null) continue;

            GameObject buttonObj = Instantiate(methodButtonPrefab, methodContainer);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = method.methodName;

            if (button != null)
            {
                PurificationMethod capturedMethod = method; // 클로저를 위한 캡처
                button.onClick.AddListener(() => OnMethodSelected(capturedMethod));
            }
        }
    }

    /// <summary>
    /// 페티시 선택
    /// </summary>
    void OnFetishSelected(FetishType fetish)
    {
        _selectedFetish = fetish;

        if (selectedFetishPanel != null)
            selectedFetishPanel.SetActive(true);

        UpdateSelectedFetishInfo();
    }

    /// <summary>
    /// 정화 방법 선택
    /// </summary>
    void OnMethodSelected(PurificationMethod method)
    {
        _selectedMethod = method;
        UpdateSelectedFetishInfo();
    }

    /// <summary>
    /// 선택된 페티시 정보 업데이트
    /// </summary>
    void UpdateSelectedFetishInfo()
    {
        if (_selectedFetish == FetishType.None) return;

        if (fetishNameText != null)
            fetishNameText.text = GetFetishName(_selectedFetish);

        if (fetishDescriptionText != null)
            fetishDescriptionText.text = GetFetishDescription(_selectedFetish);

        int intensity = PlayerFetishState.instance?.GetIntensity(_selectedFetish) ?? 0;
        if (intensityText != null)
            intensityText.text = $"강도: {intensity}";

        if (PurificationManager.instance != null)
        {
            int cost = PurificationManager.instance.CalculatePurificationCost(_selectedFetish);
            if (costText != null)
                costText.text = $"비용: {cost} 골드";

            float chance = PurificationManager.instance.CalculateSuccessChance(_selectedFetish);
            if (successChanceText != null)
                successChanceText.text = $"성공 확률: {chance * 100:F1}%";
        }

        // 정화 버튼 활성화 여부
        if (purifyButton != null)
        {
            bool canAfford = PurificationManager.instance != null &&
                           PurificationManager.instance.playerGold >=
                           PurificationManager.instance.CalculatePurificationCost(_selectedFetish);
            purifyButton.interactable = canAfford && _selectedMethod != null;
        }
    }

    /// <summary>
    /// 정화 실행
    /// </summary>
    void OnPurifyClicked()
    {
        if (_selectedFetish == FetishType.None || _selectedMethod == null || PurificationManager.instance == null)
            return;

        bool success = PurificationManager.instance.AttemptPurification(_selectedFetish);

        ShowResult(success);
    }

    /// <summary>
    /// 결과 표시
    /// </summary>
    void ShowResult(bool success)
    {
        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = success
                ? "정화에 성공했습니다!"
                : "정화에 실패했습니다...";
            resultText.color = success ? Color.green : Color.red;
        }

        // 리스트 새로고침
        RefreshFetishList();
        UpdateSelectedFetishInfo();
    }

    /// <summary>
    /// 결과 패널 닫기
    /// </summary>
    void CloseResult()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    /// <summary>
    /// 페티시 이름 가져오기
    /// </summary>
    string GetFetishName(FetishType fetish)
    {
        switch (fetish)
        {
            case FetishType.Tentacle: return "촉수";
            case FetishType.Bondage: return "구속";
            case FetishType.Hypnosis: return "최면";
            case FetishType.Corruption: return "타락";
            case FetishType.Breast: return "가슴";
            case FetishType.Feet: return "발";
            case FetishType.Ass: return "엉덩이";
            case FetishType.Public: return "노출";
            case FetishType.Monster: return "몬스터";
            case FetishType.Femdom: return "여성 지배";
            default: return fetish.ToString();
        }
    }

    /// <summary>
    /// 페티시 설명 가져오기
    /// </summary>
    string GetFetishDescription(FetishType fetish)
    {
        // TODO: FetishData에서 가져오기
        return "페티시 설명을 여기에 표시합니다.";
    }
}

