using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 덱 편집 UI
/// 컬렉션에서 카드를 선택하여 덱 구성
/// </summary>
public class DeckEditorUI : MonoBehaviour
{
    [Header("패널")]
    public GameObject editorPanel;
    public GameObject collectionPanel;
    public GameObject deckPanel;

    [Header("컬렉션 영역")]
    public Transform collectionContainer;
    public GameObject cardSlotPrefab;
    public TMP_InputField searchInput;
    public TMP_Dropdown filterDropdown;
    public TMP_Dropdown sortDropdown;

    [Header("덱 영역")]
    public Transform deckContainer;
    public TMP_Text deckNameText;
    public TMP_Text deckCountText;
    public TMP_Text manaCurveText;
    public Button saveButton;
    public Button clearButton;

    [Header("덱 선택")]
    public TMP_Dropdown deckSelectDropdown;
    public Button newDeckButton;
    public Button deleteDeckButton;

    [Header("카드 정보")]
    public GameObject cardPreviewPanel;
    public Image previewImage;
    public TMP_Text previewName;
    public TMP_Text previewStats;
    public TMP_Text previewText;

    [Header("설정")]
    public CardDatabase cardDatabase;

    // 상태
    private DeckData _currentDeck;
    private List<CardSlotUI> _collectionSlots = new List<CardSlotUI>();
    private List<CardSlotUI> _deckSlots = new List<CardSlotUI>();
    private string _searchFilter = "";
    private CardType _typeFilter = CardType.None;
    private SortMode _sortMode = SortMode.Mana;

    public enum SortMode { Mana, Name, Attack, Type }

    void Start()
    {
        if (editorPanel != null)
            editorPanel.SetActive(false);

        SetupButtons();
        SetupDropdowns();
    }

    void SetupButtons()
    {
        if (saveButton != null)
            saveButton.onClick.AddListener(SaveCurrentDeck);

        if (clearButton != null)
            clearButton.onClick.AddListener(ClearCurrentDeck);

        if (newDeckButton != null)
            newDeckButton.onClick.AddListener(CreateNewDeck);

        if (deleteDeckButton != null)
            deleteDeckButton.onClick.AddListener(DeleteCurrentDeck);

        if (searchInput != null)
            searchInput.onValueChanged.AddListener(OnSearchChanged);
    }

    void SetupDropdowns()
    {
        if (filterDropdown != null)
        {
            filterDropdown.ClearOptions();
            filterDropdown.AddOptions(new List<string> { "전체", "캐릭터", "주문", "아티팩트" });
            filterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }

        if (sortDropdown != null)
        {
            sortDropdown.ClearOptions();
            sortDropdown.AddOptions(new List<string> { "마나", "이름", "공격력", "타입" });
            sortDropdown.onValueChanged.AddListener(OnSortChanged);
        }
    }

    /// <summary>
    /// 덱 에디터 열기
    /// </summary>
    public void Open()
    {
        if (editorPanel != null)
            editorPanel.SetActive(true);

        RefreshDeckDropdown();
        LoadDeck(PlayerCollection.instance?.GetActiveDeck());
        RefreshCollection();
    }

    /// <summary>
    /// 덱 에디터 닫기
    /// </summary>
    public void Close()
    {
        if (editorPanel != null)
            editorPanel.SetActive(false);
    }

    /// <summary>
    /// 컬렉션 새로고침
    /// </summary>
    void RefreshCollection()
    {
        // 기존 슬롯 정리
        foreach (var slot in _collectionSlots)
        {
            if (slot != null && slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        _collectionSlots.Clear();

        if (cardDatabase == null || PlayerCollection.instance == null) return;

        // 소유한 카드 목록 가져오기
        List<CardData> cards = new List<CardData>();
        foreach (var entry in PlayerCollection.instance.ownedCards)
        {
            CardData card = cardDatabase.GetCard(entry.cardId);
            if (card != null)
                cards.Add(card);
        }

        // 필터링
        cards = FilterCards(cards);

        // 정렬
        cards = SortCards(cards);

        // 슬롯 생성
        foreach (CardData card in cards)
        {
            CreateCollectionSlot(card);
        }
    }

    void CreateCollectionSlot(CardData card)
    {
        if (collectionContainer == null || cardSlotPrefab == null) return;

        GameObject slotGo = Instantiate(cardSlotPrefab, collectionContainer);
        CardSlotUI slot = slotGo.GetComponent<CardSlotUI>();

        if (slot != null)
        {
            int ownedCount = PlayerCollection.instance.GetCardCount(card.id);
            int inDeckCount = _currentDeck?.cardIds.FindAll(id => id == card.id).Count ?? 0;

            slot.Setup(card, ownedCount, inDeckCount);
            slot.OnClicked += () => OnCollectionCardClicked(card);
            slot.OnHovered += () => ShowCardPreview(card);

            _collectionSlots.Add(slot);
        }
    }

    /// <summary>
    /// 덱 표시 새로고침
    /// </summary>
    void RefreshDeck()
    {
        // 기존 슬롯 정리
        foreach (var slot in _deckSlots)
        {
            if (slot != null && slot.gameObject != null)
                Destroy(slot.gameObject);
        }
        _deckSlots.Clear();

        if (_currentDeck == null || cardDatabase == null) return;

        // 덱 이름 및 카드 수
        if (deckNameText != null)
            deckNameText.text = _currentDeck.deckName;

        if (deckCountText != null)
        {
            int count = _currentDeck.cardIds.Count;
            int min = PlayerCollection.instance?.minDeckSize ?? 20;
            int max = PlayerCollection.instance?.maxDeckSize ?? 30;
            deckCountText.text = $"{count}/{max}";
            deckCountText.color = count >= min ? Color.white : Color.red;
        }

        // 마나 커브 계산
        UpdateManaCurve();

        // 카드별로 그룹화하여 표시
        Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        foreach (string cardId in _currentDeck.cardIds)
        {
            if (cardCounts.ContainsKey(cardId))
                cardCounts[cardId]++;
            else
                cardCounts[cardId] = 1;
        }

        // 슬롯 생성
        foreach (var kvp in cardCounts)
        {
            CardData card = cardDatabase.GetCard(kvp.Key);
            if (card != null)
            {
                CreateDeckSlot(card, kvp.Value);
            }
        }
    }

    void CreateDeckSlot(CardData card, int count)
    {
        if (deckContainer == null || cardSlotPrefab == null) return;

        GameObject slotGo = Instantiate(cardSlotPrefab, deckContainer);
        CardSlotUI slot = slotGo.GetComponent<CardSlotUI>();

        if (slot != null)
        {
            slot.SetupDeckMode(card, count);
            slot.OnClicked += () => OnDeckCardClicked(card);
            slot.OnHovered += () => ShowCardPreview(card);

            _deckSlots.Add(slot);
        }
    }

    void UpdateManaCurve()
    {
        if (_currentDeck == null || manaCurveText == null) return;

        int[] curve = new int[8]; // 0-7+
        foreach (string cardId in _currentDeck.cardIds)
        {
            CardData card = cardDatabase?.GetCard(cardId);
            if (card != null)
            {
                int index = Mathf.Min(card.mana, 7);
                curve[index]++;
            }
        }

        // 간단한 텍스트 표시
        string curveStr = "";
        for (int i = 0; i <= 7; i++)
        {
            curveStr += $"{i}: {curve[i]} ";
        }
        manaCurveText.text = curveStr;
    }

    // ===== 이벤트 핸들러 =====

    void OnCollectionCardClicked(CardData card)
    {
        if (_currentDeck == null) return;

        if (PlayerCollection.instance.AddCardToDeck(_currentDeck, card.id))
        {
            RefreshCollection();
            RefreshDeck();
        }
    }

    void OnDeckCardClicked(CardData card)
    {
        if (_currentDeck == null) return;

        if (PlayerCollection.instance.RemoveCardFromDeck(_currentDeck, card.id))
        {
            RefreshCollection();
            RefreshDeck();
        }
    }

    void OnSearchChanged(string search)
    {
        _searchFilter = search.ToLower();
        RefreshCollection();
    }

    void OnFilterChanged(int index)
    {
        _typeFilter = index switch
        {
            1 => CardType.Character,
            2 => CardType.Spell,
            3 => CardType.Artifact,
            _ => CardType.None
        };
        RefreshCollection();
    }

    void OnSortChanged(int index)
    {
        _sortMode = (SortMode)index;
        RefreshCollection();
    }

    void ShowCardPreview(CardData card)
    {
        if (cardPreviewPanel == null) return;

        cardPreviewPanel.SetActive(true);

        if (previewImage != null && card.art_full != null)
            previewImage.sprite = card.art_full;

        if (previewName != null)
            previewName.text = card.title;

        if (previewStats != null)
            previewStats.text = $"마나: {card.mana}  공격: {card.attack}  체력: {card.hp}";

        if (previewText != null)
            previewText.text = card.text;
    }

    // ===== 덱 관리 =====

    void LoadDeck(DeckData deck)
    {
        _currentDeck = deck ?? PlayerCollection.instance?.CreateNewDeck("새 덱");
        RefreshDeck();
    }

    void SaveCurrentDeck()
    {
        if (_currentDeck == null) return;

        if (PlayerCollection.instance.IsDeckValid(_currentDeck))
        {
            Debug.Log($"<color=green>덱 저장 완료: {_currentDeck.deckName}</color>");
        }
        else
        {
            Debug.Log("<color=yellow>덱이 유효하지 않습니다.</color>");
        }
    }

    void ClearCurrentDeck()
    {
        if (_currentDeck == null) return;

        _currentDeck.cardIds.Clear();
        RefreshDeck();
        RefreshCollection();
    }

    void CreateNewDeck()
    {
        DeckData newDeck = PlayerCollection.instance?.CreateNewDeck($"덱 {PlayerCollection.instance.savedDecks.Count}");
        if (newDeck != null)
        {
            LoadDeck(newDeck);
            RefreshDeckDropdown();
        }
    }

    void DeleteCurrentDeck()
    {
        if (PlayerCollection.instance == null) return;

        int index = PlayerCollection.instance.savedDecks.IndexOf(_currentDeck);
        if (index >= 0)
        {
            PlayerCollection.instance.DeleteDeck(index);
            LoadDeck(PlayerCollection.instance.GetActiveDeck());
            RefreshDeckDropdown();
        }
    }

    void RefreshDeckDropdown()
    {
        if (deckSelectDropdown == null || PlayerCollection.instance == null) return;

        deckSelectDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var deck in PlayerCollection.instance.savedDecks)
        {
            options.Add(deck.deckName);
        }

        deckSelectDropdown.AddOptions(options);
        deckSelectDropdown.value = PlayerCollection.instance.activeDeckIndex;
        deckSelectDropdown.onValueChanged.AddListener(OnDeckSelected);
    }

    void OnDeckSelected(int index)
    {
        if (PlayerCollection.instance == null) return;

        PlayerCollection.instance.activeDeckIndex = index;
        LoadDeck(PlayerCollection.instance.GetActiveDeck());
    }

    // ===== 필터링 & 정렬 =====

    List<CardData> FilterCards(List<CardData> cards)
    {
        return cards.FindAll(card =>
        {
            // 검색어 필터
            if (!string.IsNullOrEmpty(_searchFilter))
            {
                if (!card.title.ToLower().Contains(_searchFilter) &&
                    !card.text.ToLower().Contains(_searchFilter))
                    return false;
            }

            // 타입 필터
            if (_typeFilter != CardType.None && card.type != _typeFilter)
                return false;

            return true;
        });
    }

    List<CardData> SortCards(List<CardData> cards)
    {
        switch (_sortMode)
        {
            case SortMode.Mana:
                cards.Sort((a, b) => a.mana.CompareTo(b.mana));
                break;
            case SortMode.Name:
                cards.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.Ordinal));
                break;
            case SortMode.Attack:
                cards.Sort((a, b) => b.attack.CompareTo(a.attack));
                break;
            case SortMode.Type:
                cards.Sort((a, b) => a.type.CompareTo(b.type));
                break;
        }
        return cards;
    }
}

/// <summary>
/// 카드 슬롯 UI 컴포넌트
/// </summary>
public class CardSlotUI : MonoBehaviour
{
    public Image cardImage;
    public TMP_Text nameText;
    public TMP_Text manaText;
    public TMP_Text countText;
    public Image manaCrystal;
    public GameObject dimOverlay;

    public event Action OnClicked;
    public event Action OnHovered;

    private CardData _cardData;

    public void Setup(CardData card, int ownedCount, int inDeckCount)
    {
        _cardData = card;

        if (cardImage != null && card.art_full != null)
            cardImage.sprite = card.art_full;

        if (nameText != null)
            nameText.text = card.title;

        if (manaText != null)
            manaText.text = card.mana.ToString();

        if (countText != null)
            countText.text = $"{inDeckCount}/{ownedCount}";

        // 더 이상 추가 불가능하면 어둡게
        int maxCopies = PlayerCollection.instance?.maxCopiesPerCard ?? 2;
        bool canAdd = inDeckCount < maxCopies && inDeckCount < ownedCount;

        if (dimOverlay != null)
            dimOverlay.SetActive(!canAdd);

        // 버튼 설정
        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => OnClicked?.Invoke());
    }

    public void SetupDeckMode(CardData card, int count)
    {
        _cardData = card;

        if (cardImage != null && card.art_full != null)
            cardImage.sprite = card.art_full;

        if (nameText != null)
            nameText.text = card.title;

        if (manaText != null)
            manaText.text = card.mana.ToString();

        if (countText != null)
            countText.text = $"x{count}";

        Button btn = GetComponent<Button>();
        if (btn != null)
            btn.onClick.AddListener(() => OnClicked?.Invoke());
    }

    public void OnPointerEnter()
    {
        OnHovered?.Invoke();
    }
}

