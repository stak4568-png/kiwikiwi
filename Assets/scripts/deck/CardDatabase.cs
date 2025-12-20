using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 카드 데이터베이스
/// 게임 내 모든 카드 데이터를 관리
/// </summary>
[CreateAssetMenu(fileName = "CardDatabase", menuName = "Kiwi Card Game/Card Database")]
public class CardDatabase : ScriptableObject
{
    [Header("모든 카드")]
    public List<CardData> allCards = new List<CardData>();

    // 캐싱
    private Dictionary<string, CardData> _cardCache;
    private bool _isCacheBuilt = false;

    /// <summary>
    /// ID로 카드 가져오기
    /// </summary>
    public CardData GetCard(string cardId)
    {
        BuildCacheIfNeeded();

        if (_cardCache.TryGetValue(cardId, out CardData card))
            return card;

        return null;
    }

    /// <summary>
    /// 타입별 카드 목록
    /// </summary>
    public List<CardData> GetCardsByType(CardType type)
    {
        return allCards.FindAll(c => c.type == type);
    }

    /// <summary>
    /// 마나 비용별 카드 목록
    /// </summary>
    public List<CardData> GetCardsByMana(int manaCost)
    {
        return allCards.FindAll(c => c.mana == manaCost);
    }

    /// <summary>
    /// 키워드 보유 카드 목록
    /// </summary>
    public List<CardData> GetCardsWithKeyword(Keyword keyword)
    {
        return allCards.FindAll(c => c.HasKeyword(keyword));
    }

    /// <summary>
    /// 페티시 속성별 카드 목록
    /// </summary>
    public List<CardData> GetCardsByFetish(FetishType fetish)
    {
        return allCards.FindAll(c => c.seduceFetishType == fetish);
    }

    /// <summary>
    /// 캐시 빌드
    /// </summary>
    void BuildCacheIfNeeded()
    {
        if (_isCacheBuilt && _cardCache != null) return;

        _cardCache = new Dictionary<string, CardData>();
        foreach (CardData card in allCards)
        {
            if (card != null && !string.IsNullOrEmpty(card.id))
            {
                _cardCache[card.id] = card;
            }
        }
        _isCacheBuilt = true;
    }

    /// <summary>
    /// 캐시 갱신 (에디터에서 변경 시)
    /// </summary>
    public void RefreshCache()
    {
        _isCacheBuilt = false;
        BuildCacheIfNeeded();
    }

    /// <summary>
    /// 카드 추가 (에디터용)
    /// </summary>
    public void AddCard(CardData card)
    {
        if (card == null || allCards.Contains(card)) return;

        allCards.Add(card);
        _isCacheBuilt = false;
    }

    /// <summary>
    /// 랜덤 카드 가져오기
    /// </summary>
    public CardData GetRandomCard()
    {
        if (allCards.Count == 0) return null;
        return allCards[Random.Range(0, allCards.Count)];
    }

    /// <summary>
    /// 랜덤 카드 목록 (특정 타입)
    /// </summary>
    public List<CardData> GetRandomCards(int count, CardType type = CardType.None)
    {
        List<CardData> pool = type == CardType.None ? allCards : GetCardsByType(type);
        List<CardData> result = new List<CardData>();

        if (pool.Count == 0) return result;

        // 셔플 복사본 사용
        List<CardData> shuffled = new List<CardData>(pool);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        for (int i = 0; i < Mathf.Min(count, shuffled.Count); i++)
        {
            result.Add(shuffled[i]);
        }

        return result;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        _isCacheBuilt = false;
    }
#endif
}

