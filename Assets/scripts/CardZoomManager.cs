using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardZoomManager : MonoBehaviour
{
    public static CardZoomManager instance;

    [Header("UI Panel")]
    public GameObject zoomPanel;

    [Header("Large Card UI References")]
    public Image largeImage;
    public TMP_Text nameText;
    public TMP_Text descText;
    public TMP_Text elementText;
    public TMP_Text statsText;

    [Header("Gaze Buttons")]
    public GameObject appreciateBtn; // 감상
    public GameObject analyzeBtn;    // 분석

    private CardDisplay currentCard;

    void Awake()
    {
        // 싱글톤 초기화
        if (instance == null) instance = this;
        if (zoomPanel != null) zoomPanel.SetActive(false);
    }

    public void ShowCardZoom(CardDisplay card)
    {
        currentCard = card;
        if (zoomPanel != null) zoomPanel.SetActive(true);
        UpdateZoomUI();
    }

    public void UpdateZoomUI()
    {
        if (currentCard == null || currentCard.cardData == null) return;
        CardData data = currentCard.cardData;

        // 1. 공통 텍스트 세팅
        if (nameText != null) nameText.text = data.cardName;
        if (elementText != null) elementText.text = data.element.ToString();

        // 2. 일러스트 (해금 여부 반영)
        if (largeImage != null)
        {
            largeImage.sprite = currentCard.isArtRevealed ?
                (data.originalArt ?? data.censoredArt) : (data.censoredArt ?? data.originalArt);
        }

        // 3. 효과 설명 (해금 여부 반영)
        if (descText != null)
        {
            descText.text = currentCard.isInfoRevealed ?
                data.description : data.censoredDescription;
        }

        // 4. 스탯 표시 (몬스터 데이터로 형변환 시도)
        if (statsText != null)
        {
            if (data is MonsterCardData monster)
                statsText.text = $"{monster.attack} / {monster.health}";
            else
                statsText.text = ""; // 마법 등은 스탯 안 보임
        }

        // 5. 버튼 표시 로직 (적 구역이고 아직 해금 안 됐을 때만)
        if (currentCard.transform.parent != null)
        {
            DropZone parentZone = currentCard.transform.parent.GetComponent<DropZone>();
            bool isEnemy = (parentZone != null && parentZone.zoneType == ZoneType.EnemyField);

            if (appreciateBtn != null)
                appreciateBtn.SetActive(isEnemy && !currentCard.isArtRevealed);

            if (analyzeBtn != null)
                analyzeBtn.SetActive(isEnemy && !currentCard.isInfoRevealed);
        }
    }

    // 감상 버튼 클릭
    public void OnClickAppreciate()
    {
        if (GameManager.instance.TryUseFocus())
        {
            currentCard.isArtRevealed = true;
            SyncUI();
        }
    }

    // 분석 버튼 클릭
    public void OnClickAnalyze()
    {
        if (GameManager.instance.TryUseFocus())
        {
            currentCard.isInfoRevealed = true;
            SyncUI();
        }
    }

    void SyncUI()
    {
        currentCard.UpdateCardUI(); // 필드 카드 갱신
        UpdateZoomUI();             // 확대 창 갱신
    }

    public void CloseZoom()
    {
        if (zoomPanel != null) zoomPanel.SetActive(false);
    }
}