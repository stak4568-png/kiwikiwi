using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("카드 설정")]
    public GameObject cardPrefab;       // 카드 프리팹
    public Transform handArea;          // 카드가 생성될 손패 구역 (HandArea)

    [Header("덱 데이터")]
    public List<CardData> masterDeck;   // 처음에 설정하는 전체 카드 리스트
    private List<CardData> drawingDeck = new List<CardData>(); // 실제 게임 중 뽑을 카드들

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // 1. 게임 시작 시 마스터 덱의 내용을 드로잉 덱으로 복사
        SetupDeck();

        // 2. 처음 시작할 때 카드 3장 뽑기
        DrawInitialCards(3);
    }

    public void SetupDeck()
    {
        drawingDeck.Clear();
        drawingDeck.AddRange(masterDeck);
        ShuffleDeck();
    }

    // 덱 섞기 (Fisher-Yates 알고리즘)
    public void ShuffleDeck()
    {
        for (int i = 0; i < drawingDeck.Count; i++)
        {
            CardData temp = drawingDeck[i];
            int randomIndex = Random.Range(i, drawingDeck.Count);
            drawingDeck[i] = drawingDeck[randomIndex];
            drawingDeck[randomIndex] = temp;
        }
        Debug.Log("덱을 섞었습니다.");
    }

    // 카드 한 장 뽑기
    public void DrawCard()
    {
        if (drawingDeck.Count <= 0)
        {
            Debug.LogWarning("덱에 카드가 더 이상 없습니다!");
            return;
        }

        // 손패 제한 확인 (예: 최대 10장)
        if (handArea.childCount >= 10)
        {
            Debug.Log("손패가 가득 찼습니다!");
            return;
        }

        // 덱의 맨 위(마지막 인덱스)에서 카드 데이터를 가져옴
        CardData data = drawingDeck[drawingDeck.Count - 1];
        drawingDeck.RemoveAt(drawingDeck.Count - 1);

        // 카드 프리팹 생성
        GameObject newCard = Instantiate(cardPrefab, handArea);

        // 카드 데이터 주입
        CardDisplay display = newCard.GetComponent<CardDisplay>();
        if (display != null)
        {
            display.cardData = data;
            display.UpdateSourceZone(); // 손패 비주얼로 자동 설정됨
            display.UpdateCardUI();
        }

        Debug.Log($"{data.cardName}을(를) 드로우했습니다.");
    }

    public void DrawInitialCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawCard();
        }
    }
}