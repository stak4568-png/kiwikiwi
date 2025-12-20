using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("������ ����")]
    public GameObject cardPrefab;
    [Tooltip("필드에 소환되는 카드 프리팹 (슬롯 크기에 맞춤). 없으면 cardPrefab 사용")]
    public GameObject cardFieldPrefab; // 필드용
    public Transform playerHandArea;   // �÷��̾� ���� ����

    [Header("�÷��̾� ��")]
    public List<CardData> playerMasterDeck;
    private List<CardData> playerDrawingDeck = new List<CardData>();

    [Header("�� �� �� ��")]
    public List<CardData> enemyMasterDeck;
    private List<CardData> enemyDrawingDeck = new List<CardData>();
    public List<CardData> enemyHand = new List<CardData>(); // ���� �� (�����ͷθ� ����)

    void Awake() => instance = this;

    void Start()
    {
        SetupDecks();
        DrawInitialCards(true, 3);  // �÷��̾� 3��
        DrawInitialCards(false, 3); // �� 3��
    }

    public void SetupDecks()
    {
        // �÷��̾� �� ����
        playerDrawingDeck.Clear();
        playerDrawingDeck.AddRange(playerMasterDeck);
        Shuffle(playerDrawingDeck);

        // �� �� ����
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

    // ī�� �̱� ���� �Լ�
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

            enemyHand.Add(data); // ���� ����Ʈ���� �߰�
            Debug.Log($"���� ī�带 1�� �̾ҽ��ϴ�. (���� �� ��: {enemyHand.Count}��)");
        }
    }

    public void DrawInitialCards(bool isPlayer, int count)
    {
        for (int i = 0; i < count; i++) DrawCard(isPlayer);
    }
}