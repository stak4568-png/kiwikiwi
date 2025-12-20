using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 마을 UI
/// 마을 입장 시 시설 선택 UI
/// </summary>
public class VillageUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject villagePanel;
    public CanvasGroup canvasGroup;

    [Header("마을 정보")]
    public TMP_Text villageNameText;
    public TMP_Text villageFloorText;
    public Image villageBackground;

    [Header("시설 버튼")]
    public Transform facilityButtonContainer;
    public GameObject facilityButtonPrefab;

    [Header("설명 패널")]
    public GameObject descriptionPanel;
    public TMP_Text facilityNameText;
    public TMP_Text facilityDescriptionText;
    public Button useFacilityButton;
    public Button closeDescriptionButton;

    // 상태
    private VillageData _currentVillage;
    private FacilityType _selectedFacility;

    void Start()
    {
        if (villagePanel != null)
            villagePanel.SetActive(false);

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        if (closeDescriptionButton != null)
            closeDescriptionButton.onClick.AddListener(CloseDescription);

        if (useFacilityButton != null)
            useFacilityButton.onClick.AddListener(OnUseFacilityClicked);
    }

    /// <summary>
    /// 마을 UI 표시
    /// </summary>
    public void Show(VillageData village)
    {
        if (village == null) return;

        _currentVillage = village;

        if (villagePanel != null)
            villagePanel.SetActive(true);

        if (villageNameText != null)
            villageNameText.text = village.villageName;

        if (villageFloorText != null && VillageManager.instance != null)
            villageFloorText.text = $"{VillageManager.instance.currentVillageFloor}층";

        if (villageBackground != null && village.backgroundImage != null)
            villageBackground.sprite = village.backgroundImage;

        RefreshFacilityButtons();
    }

    /// <summary>
    /// 마을 UI 숨기기
    /// </summary>
    public void Hide()
    {
        if (villagePanel != null)
            villagePanel.SetActive(false);

        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);

        _currentVillage = null;
    }

    /// <summary>
    /// 시설 버튼 새로고침
    /// </summary>
    void RefreshFacilityButtons()
    {
        if (facilityButtonContainer == null || facilityButtonPrefab == null || _currentVillage == null)
            return;

        // 기존 버튼 제거
        foreach (Transform child in facilityButtonContainer)
        {
            Destroy(child.gameObject);
        }

        // 사용 가능한 시설 버튼 생성
        foreach (var facility in _currentVillage.availableFacilities)
        {
            GameObject buttonObj = Instantiate(facilityButtonPrefab, facilityButtonContainer);
            Button button = buttonObj.GetComponent<Button>();
            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
                buttonText.text = GetFacilityName(facility);

            if (button != null)
            {
                FacilityType capturedFacility = facility; // 클로저를 위한 캡처
                button.onClick.AddListener(() => OnFacilityButtonClicked(capturedFacility));
            }
        }
    }

    /// <summary>
    /// 시설 이름 가져오기
    /// </summary>
    string GetFacilityName(FacilityType facility)
    {
        switch (facility)
        {
            case FacilityType.Inn: return "여관";
            case FacilityType.Shop: return "상점";
            case FacilityType.Church: return "교회";
            case FacilityType.DeckEditor: return "덱 편집";
            case FacilityType.Gallery: return "갤러리";
            case FacilityType.Blacksmith: return "대장간";
            case FacilityType.Library: return "도서관";
            case FacilityType.Arena: return "투기장";
            default: return facility.ToString();
        }
    }

    /// <summary>
    /// 시설 버튼 클릭
    /// </summary>
    void OnFacilityButtonClicked(FacilityType facility)
    {
        _selectedFacility = facility;
        ShowFacilityDescription(facility);
    }

    /// <summary>
    /// 시설 설명 표시
    /// </summary>
    void ShowFacilityDescription(FacilityType facility)
    {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(true);

        if (facilityNameText != null)
            facilityNameText.text = GetFacilityName(facility);

        if (facilityDescriptionText != null)
            facilityDescriptionText.text = GetFacilityDescription(facility);
    }

    /// <summary>
    /// 시설 설명 가져오기
    /// </summary>
    string GetFacilityDescription(FacilityType facility)
    {
        switch (facility)
        {
            case FacilityType.Inn:
                return "체력을 회복할 수 있습니다.";
            case FacilityType.Shop:
                return "카드와 아이템을 구매할 수 있습니다.";
            case FacilityType.Church:
                return "페티시를 정화할 수 있습니다.";
            case FacilityType.DeckEditor:
                return "덱을 편집할 수 있습니다.";
            case FacilityType.Gallery:
                return "해금된 일러스트를 볼 수 있습니다.";
            case FacilityType.Blacksmith:
                return "카드를 강화할 수 있습니다.";
            case FacilityType.Library:
                return "게임 정보를 확인할 수 있습니다.";
            case FacilityType.Arena:
                return "추가 전투를 할 수 있습니다.";
            default:
                return "";
        }
    }

    /// <summary>
    /// 시설 사용 버튼 클릭
    /// </summary>
    void OnUseFacilityClicked()
    {
        if (VillageManager.instance != null)
        {
            VillageManager.instance.UseFacility(_selectedFacility);
        }
        CloseDescription();
    }

    /// <summary>
    /// 설명 패널 닫기
    /// </summary>
    void CloseDescription()
    {
        if (descriptionPanel != null)
            descriptionPanel.SetActive(false);
    }
}

