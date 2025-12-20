using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("프리팹 설정")]
    public GameObject cardPrefab;
    public Transform playerHandArea;   // 플레이어 손패 구역

    [Header("플레이어 덱")]
    public List<CardData> playerMasterDeck;
    private List<CardData> playerDrawingDeck = new List<CardData>();

    [Header("적 덱 및 패")]
    public List<CardData> enemyMasterDeck;
    private List<CardData> enemyDrawingDeck = new List<CardData>();
    public List<CardData> enemyHand = new List<CardData>(); // 적의 패 (데이터로만 관리)

    void Awake() => instance = this;

    void Start()
    {
        SetupDecks();
        DrawInitialCards(true, 3);  // 플레이어 3장
        DrawInitialCards(false, 3); // 적 3장
    }

    public void SetupDecks()
    {
        // 플레이어 덱 세팅
        playerDrawingDeck.Clear();
        playerDrawingDeck.AddRange(playerMasterDeck);
        Shuffle(playerDrawingDeck);

        // 적 덱 세팅
        enemyDrawingDeck.Clear();
        enemyDrawingDeck.AddRange(enemyMasterDeck);
        Shuffle(enemyDrawingDeck);
    }

    private void Shuffle(List<CardData> deck)
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardData temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    // 카드 뽑기 통합 함수
    public void DrawCard(bool isPlayer)
    {
        if (isPlayer)
        {
            if (playerDrawingDeck.Count <= 0 || playerHandArea.childCount >= 10) return;
            CardData data = playerDrawingDeck[playerDrawingDeck.Count - 1];
            playerDrawingDeck.RemoveAt(playerDrawingDeck.Count - 1);

            GameObject newCard = Instantiate(cardPrefab, playerHandArea);
            newCard.GetComponent<CardDisplay>().Init(data, true);
        }
        else
        {
            if (enemyDrawingDeck.Count <= 0 || enemyHand.Count >= 10) return;
            CardData data = enemyDrawingDeck[enemyDrawingDeck.Count - 1];
            enemyDrawingDeck.RemoveAt(enemyDrawingDeck.Count - 1);

            enemyHand.Add(data); // 적은 리스트에만 추가
            Debug.Log($"적이 카드를 1장 뽑았습니다. (현재 적 패: {enemyHand.Count}장)");
        }
    }

    public void DrawInitialCards(bool isPlayer, int count)
    {
        for (int i = 0; i < count; i++) DrawCard(isPlayer);
    }
}