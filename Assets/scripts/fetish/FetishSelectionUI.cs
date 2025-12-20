using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 게임 시작 시 초기 페티시 선택 UI
/// </summary>
public class FetishSelectionUI : MonoBehaviour
{
    [Header("UI 요소")]
    public GameObject selectionPanel;
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public Transform optionContainer;
    public Button confirmButton;

    [Header("옵션 프리팹")]
    public GameObject fetishOptionPrefab;

    [Header("선택 가능한 페티시")]
    public List<FetishData> selectableFetishes = new List<FetishData>();

    [Header("설정")]
    [TextArea(2, 4)]
    public string titleMessage = "당신의 약점을 선택하세요";
    [TextArea(2, 4)]
    public string instructionMessage = "선택한 페티시는 전투에서 약점이 됩니다.\n하지만 특별한 이벤트를 경험할 수 있습니다.";

    // 상태
    private FetishType _selectedFetish = FetishType.None;
    private List<FetishOptionButton> _optionButtons = new List<FetishOptionButton>();
    private Action<FetishType> _onSelectionComplete;

    void Start()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
            confirmButton.interactable = false;
        }
    }

    /// <summary>
    /// 페티시 선택 UI 표시
    /// </summary>
    public void Show(Action<FetishType> onComplete)
    {
        _onSelectionComplete = onComplete;
        _selectedFetish = FetishType.None;

        if (selectionPanel != null)
            selectionPanel.SetActive(true);

        if (titleText != null)
            titleText.text = titleMessage;

        if (descriptionText != null)
            descriptionText.text = instructionMessage;

        // 기존 옵션 제거
        ClearOptions();

        // 옵션 생성
        CreateOptions();

        if (confirmButton != null)
            confirmButton.interactable = false;
    }

    void ClearOptions()
    {
        foreach (var btn in _optionButtons)
        {
            if (btn != null && btn.gameObject != null)
                Destroy(btn.gameObject);
        }
        _optionButtons.Clear();
    }

    void CreateOptions()
    {
        if (optionContainer == null || fetishOptionPrefab == null) return;

        foreach (FetishData fetish in selectableFetishes)
        {
            if (fetish == null) continue;

            GameObject optionGo = Instantiate(fetishOptionPrefab, optionContainer);
            FetishOptionButton optionBtn = optionGo.GetComponent<FetishOptionButton>();

            if (optionBtn != null)
            {
                optionBtn.Setup(fetish, OnOptionSelected);
                _optionButtons.Add(optionBtn);
            }
            else
            {
                // 프리팹에 컴포넌트가 없으면 기본 설정
                SetupBasicOption(optionGo, fetish);
            }
        }
    }

    void SetupBasicOption(GameObject optionGo, FetishData fetish)
    {
        // 아이콘
        Image icon = optionGo.GetComponentInChildren<Image>();
        if (icon != null && fetish.icon != null)
            icon.sprite = fetish.icon;

        // 이름
        TMP_Text nameText = optionGo.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
            nameText.text = fetish.displayName;

        // 버튼
        Button btn = optionGo.GetComponent<Button>();
        if (btn != null)
        {
            FetishType capturedType = fetish.fetishType;
            btn.onClick.AddListener(() => OnOptionSelected(capturedType));
        }
    }

    void OnOptionSelected(FetishType type)
    {
        _selectedFetish = type;

        // 선택 시각 피드백
        foreach (var btn in _optionButtons)
        {
            if (btn != null)
                btn.SetSelected(btn.FetishType == type);
        }

        // 선택된 페티시 설명 표시
        FetishData selected = selectableFetishes.Find(f => f.fetishType == type);
        if (selected != null && descriptionText != null)
        {
            descriptionText.text = $"<b>{selected.displayName}</b>\n{selected.description}";
        }

        if (confirmButton != null)
            confirmButton.interactable = true;
    }

    void OnConfirmClicked()
    {
        if (_selectedFetish == FetishType.None) return;

        // PlayerFetishState에 적용
        if (PlayerFetishState.instance != null)
        {
            PlayerFetishState.instance.SelectInitialFetish(_selectedFetish);
        }

        // UI 닫기
        if (selectionPanel != null)
            selectionPanel.SetActive(false);

        // 콜백 호출
        _onSelectionComplete?.Invoke(_selectedFetish);
    }

    /// <summary>
    /// UI 닫기 (선택 없이)
    /// </summary>
    public void Close()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }
}

/// <summary>
/// 페티시 옵션 버튼 컴포넌트
/// </summary>
public class FetishOptionButton : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public Image selectionIndicator;
    public Image backgroundImage;

    private FetishType _fetishType;
    private Action<FetishType> _onSelected;

    public FetishType FetishType => _fetishType;

    public void Setup(FetishData data, Action<FetishType> onSelected)
    {
        _fetishType = data.fetishType;
        _onSelected = onSelected;

        if (iconImage != null && data.icon != null)
            iconImage.sprite = data.icon;

        if (nameText != null)
            nameText.text = data.displayName;

        if (descText != null)
            descText.text = data.description;

        if (backgroundImage != null)
            backgroundImage.color = data.themeColor;

        SetSelected(false);

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => _onSelected?.Invoke(_fetishType));
    }

    public void SetSelected(bool selected)
    {
        if (selectionIndicator != null)
            selectionIndicator.gameObject.SetActive(selected);
    }
}

