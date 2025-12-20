using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 카드 컬렉션
/// 소유한 모든 카드와 덱 관리
/// </summary>
public class PlayerCollection : MonoBehaviour
{
    public static PlayerCollection instance;

    [Header("컬렉션")]
    [Tooltip("소유한 카드 목록 (카드 ID와 수량)")]
    public List<CollectionEntry> ownedCards = new List<CollectionEntry>();

    [Header("덱")]
    public List<DeckData> savedDecks = new List<DeckData>();
    public int activeDeckIndex = 0;

    [Header("설정")]
    public int maxCopiesPerCard = 2;      // 한 카드당 덱에 넣을 수 있는 최대 수
    public int minDeckSize = 20;
    public int maxDeckSize = 30;

    // 캐싱
    private Dictionary<string, int> _cardCountCache = new Dictionary<string, int>();

    // 이벤트
    public event Action OnCollectionChanged;
    public event Action<DeckData> OnDeckChanged;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            BuildCache();
        }
        else Destroy(gameObject);
    }

    void BuildCache()
    {
        _cardCountCache.Clear();
        foreach (var entry in ownedCards)
        {
            _cardCountCache[entry.cardId] = entry.count;
        }
    }

    // ===== 컬렉션 관리 =====

    /// <summary>
    /// 카드 획득
    /// </summary>
    public void AddCard(string cardId, int count = 1)
    {
        if (string.IsNullOrEmpty(cardId)) return;

        var entry = ownedCards.Find(c => c.cardId == cardId);
        if (entry != null)
        {
            entry.count += count;
        }
        else
        {
            ownedCards.Add(new CollectionEntry { cardId = cardId, count = count });
        }

        _cardCountCache[cardId] = GetCardCount(cardId);
        OnCollectionChanged?.Invoke();

        Debug.Log($"<color=green>카드 획득: {cardId} x{count}</color>");
    }

    /// <summary>
    /// 카드 제거
    /// </summary>
    public void RemoveCard(string cardId, int count = 1)
    {
        var entry = ownedCards.Find(c => c.cardId == cardId);
        if (entry != null)
        {
            entry.count -= count;
            if (entry.count <= 0)
            {
                ownedCards.Remove(entry);
                _cardCountCache.Remove(cardId);
            }
            else
            {
                _cardCountCache[cardId] = entry.count;
            }

            OnCollectionChanged?.Invoke();
        }
    }

    /// <summary>
    /// 카드 보유 수량 확인
    /// </summary>
    public int GetCardCount(string cardId)
    {
        var entry = ownedCards.Find(c => c.cardId == cardId);
        return entry?.count ?? 0;
    }

    /// <summary>
    /// 카드 보유 여부
    /// </summary>
    public bool HasCard(string cardId)
    {
        return GetCardCount(cardId) > 0;
    }

    // ===== 덱 관리 =====

    /// <summary>
    /// 현재 활성 덱 가져오기
    /// </summary>
    public DeckData GetActiveDeck()
    {
        if (activeDeckIndex >= 0 && activeDeckIndex < savedDecks.Count)
            return savedDecks[activeDeckIndex];
        return null;
    }

    /// <summary>
    /// 새 덱 생성
    /// </summary>
    public DeckData CreateNewDeck(string deckName)
    {
        var newDeck = new DeckData
        {
            deckId = System.Guid.NewGuid().ToString(),
            deckName = deckName,
            cardIds = new List<string>()
        };

        savedDecks.Add(newDeck);
        return newDeck;
    }

    /// <summary>
    /// 덱 삭제
    /// </summary>
    public void DeleteDeck(int deckIndex)
    {
        if (deckIndex >= 0 && deckIndex < savedDecks.Count)
        {
            savedDecks.RemoveAt(deckIndex);

            if (activeDeckIndex >= savedDecks.Count)
                activeDeckIndex = savedDecks.Count - 1;
        }
    }

    /// <summary>
    /// 덱에 카드 추가
    /// </summary>
    public bool AddCardToDeck(DeckData deck, string cardId)
    {
        if (deck == null || string.IsNullOrEmpty(cardId)) return false;

        // 덱 크기 제한
        if (deck.cardIds.Count >= maxDeckSize)
        {
            Debug.Log("덱이 가득 찼습니다.");
            return false;
        }

        // 카드 복사본 제한
        int countInDeck = deck.cardIds.FindAll(id => id == cardId).Count;
        if (countInDeck >= maxCopiesPerCard)
        {
            Debug.Log($"이 카드는 최대 {maxCopiesPerCard}장만 넣을 수 있습니다.");
            return false;
        }

        // 소유 확인
        if (!HasCard(cardId))
        {
            Debug.Log("소유하지 않은 카드입니다.");
            return false;
        }

        deck.cardIds.Add(cardId);
        OnDeckChanged?.Invoke(deck);
        return true;
    }

    /// <summary>
    /// 덱에서 카드 제거
    /// </summary>
    public bool RemoveCardFromDeck(DeckData deck, string cardId)
    {
        if (deck == null || string.IsNullOrEmpty(cardId)) return false;

        bool removed = deck.cardIds.Remove(cardId);
        if (removed)
        {
            OnDeckChanged?.Invoke(deck);
        }
        return removed;
    }

    /// <summary>
    /// 덱 유효성 검사
    /// </summary>
    public bool IsDeckValid(DeckData deck)
    {
        if (deck == null) return false;
        return deck.cardIds.Count >= minDeckSize && deck.cardIds.Count <= maxDeckSize;
    }
}

/// <summary>
/// 컬렉션 항목 (카드 ID + 수량)
/// </summary>
[System.Serializable]
public class CollectionEntry
{
    public string cardId;
    public int count;
}

/// <summary>
/// 덱 데이터
/// </summary>
[System.Serializable]
public class DeckData
{
    public string deckId;
    public string deckName;
    public List<string> cardIds = new List<string>();

    // 선택적: 어드바이저 카드
    public string advisorCardId;
}

