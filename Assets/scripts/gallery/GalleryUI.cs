using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 갤러리 UI
/// </summary>
public class GalleryUI : MonoBehaviour
{
    [Header("메인 패널")]
    public GameObject galleryPanel;
    public CanvasGroup canvasGroup;

    [Header("카테고리")]
    public Transform categoryContainer;
    public GameObject categoryButtonPrefab;

    [Header("항목 그리드")]
    public Transform entryContainer;
    public GameObject entryThumbnailPrefab;

    [Header("일러스트 뷰어")]
    public GameObject viewerPanel;
    public Image viewerImage;
    public Button prevButton;
    public Button nextButton;
    public TMP_Text pageText;
    public Button closeViewerButton;

    [Header("정보 패널")]
    public TMP_Text entryNameText;
    public TMP_Text entryDescText;
    public TMP_Text progressText;

    [Header("설정")]
    public Sprite lockedThumbnail;
    public Color lockedColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    // 상태
    private List<GalleryEntry> _allEntries;
    private List<string> _unlockedIds;
    private GalleryCategory _currentCategory = GalleryCategory.Character;
    private GalleryEntry _viewingEntry;
    private int _currentImageIndex = 0;
    private List<GameObject> _activeButtons = new List<GameObject>();

    void Start()
    {
        if (galleryPanel != null)
            galleryPanel.SetActive(false);

        if (viewerPanel != null)
            viewerPanel.SetActive(false);

        // 버튼 이벤트
        if (closeViewerButton != null)
            closeViewerButton.onClick.AddListener(CloseViewer);

        if (prevButton != null)
            prevButton.onClick.AddListener(PrevImage);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextImage);
    }

    /// <summary>
    /// 갤러리 표시
    /// </summary>
    public void Show(List<GalleryEntry> entries, List<string> unlockedIds)
    {
        _allEntries = entries;
        _unlockedIds = unlockedIds;

        if (galleryPanel != null)
            galleryPanel.SetActive(true);

        CreateCategoryButtons();
        ShowCategory(_currentCategory);
        UpdateProgress();
    }

    /// <summary>
    /// 갤러리 닫기
    /// </summary>
    public void Hide()
    {
        CloseViewer();

        if (galleryPanel != null)
            galleryPanel.SetActive(false);
    }

    void CreateCategoryButtons()
    {
        if (categoryContainer == null || categoryButtonPrefab == null) return;

        // 기존 버튼 제거
        foreach (Transform child in categoryContainer)
        {
            Destroy(child.gameObject);
        }

        // 카테고리별 버튼 생성
        foreach (GalleryCategory cat in System.Enum.GetValues(typeof(GalleryCategory)))
        {
            GameObject btnGo = Instantiate(categoryButtonPrefab, categoryContainer);
            TMP_Text btnText = btnGo.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = GetCategoryName(cat);

            Button btn = btnGo.GetComponent<Button>();
            if (btn != null)
            {
                GalleryCategory captured = cat;
                btn.onClick.AddListener(() => ShowCategory(captured));
            }
        }
    }

    void ShowCategory(GalleryCategory category)
    {
        _currentCategory = category;

        // 기존 썸네일 제거
        foreach (var btn in _activeButtons)
        {
            if (btn != null) Destroy(btn);
        }
        _activeButtons.Clear();

        if (entryContainer == null || entryThumbnailPrefab == null) return;

        // 해당 카테고리 항목 가져오기
        var entries = _allEntries.FindAll(e => e.category == category);

        foreach (var entry in entries)
        {
            // 숨김 항목은 해금 전까지 표시 안 함
            if (entry.isHidden && !_unlockedIds.Contains(entry.entryId))
                continue;

            CreateThumbnail(entry);
        }
    }

    void CreateThumbnail(GalleryEntry entry)
    {
        GameObject thumbGo = Instantiate(entryThumbnailPrefab, entryContainer);
        _activeButtons.Add(thumbGo);

        bool isUnlocked = _unlockedIds.Contains(entry.entryId);

        // 이미지
        Image thumbImage = thumbGo.GetComponentInChildren<Image>();
        if (thumbImage != null)
        {
            if (isUnlocked && entry.thumbnail != null)
            {
                thumbImage.sprite = entry.thumbnail;
                thumbImage.color = Color.white;
            }
            else
            {
                thumbImage.sprite = lockedThumbnail;
                thumbImage.color = lockedColor;
            }
        }

        // 이름
        TMP_Text nameText = thumbGo.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = isUnlocked ? entry.displayName : "???";
        }

        // 클릭 이벤트
        Button btn = thumbGo.GetComponent<Button>();
        if (btn != null)
        {
            if (isUnlocked)
            {
                btn.onClick.AddListener(() => OnEntryClicked(entry));
            }
            else
            {
                btn.onClick.AddListener(() => ShowLockedHint(entry));
            }
        }
    }

    void OnEntryClicked(GalleryEntry entry)
    {
        if (entry.entryType == GalleryEntryType.Illustration)
        {
            ShowIllustration(entry);
        }
        else
        {
            GalleryManager.instance?.ViewEntry(entry.entryId);
        }
    }

    /// <summary>
    /// 일러스트 표시
    /// </summary>
    public void ShowIllustration(GalleryEntry entry)
    {
        if (entry.illustrations == null || entry.illustrations.Length == 0) return;

        _viewingEntry = entry;
        _currentImageIndex = 0;

        if (viewerPanel != null)
            viewerPanel.SetActive(true);

        UpdateViewer();
    }

    void UpdateViewer()
    {
        if (_viewingEntry == null || _viewingEntry.illustrations == null) return;

        if (viewerImage != null && _currentImageIndex < _viewingEntry.illustrations.Length)
        {
            viewerImage.sprite = _viewingEntry.illustrations[_currentImageIndex];
        }

        if (pageText != null)
        {
            pageText.text = $"{_currentImageIndex + 1} / {_viewingEntry.illustrations.Length}";
        }

        // 버튼 상태
        if (prevButton != null)
            prevButton.interactable = _currentImageIndex > 0;

        if (nextButton != null)
            nextButton.interactable = _currentImageIndex < _viewingEntry.illustrations.Length - 1;
    }

    void PrevImage()
    {
        if (_currentImageIndex > 0)
        {
            _currentImageIndex--;
            UpdateViewer();
        }
    }

    void NextImage()
    {
        if (_viewingEntry != null && _currentImageIndex < _viewingEntry.illustrations.Length - 1)
        {
            _currentImageIndex++;
            UpdateViewer();
        }
    }

    void CloseViewer()
    {
        _viewingEntry = null;

        if (viewerPanel != null)
            viewerPanel.SetActive(false);
    }

    void ShowLockedHint(GalleryEntry entry)
    {
        if (entryNameText != null)
            entryNameText.text = "???";

        if (entryDescText != null)
            entryDescText.text = !string.IsNullOrEmpty(entry.unlockHint) ? entry.unlockHint : "아직 해금되지 않았습니다.";
    }

    void UpdateProgress()
    {
        if (progressText != null)
        {
            int total = _allEntries.Count;
            int unlocked = _unlockedIds.Count;
            float percent = total > 0 ? (float)unlocked / total * 100f : 0f;
            progressText.text = $"해금률: {unlocked}/{total} ({percent:F1}%)";
        }
    }

    string GetCategoryName(GalleryCategory cat)
    {
        return cat switch
        {
            GalleryCategory.Character => "캐릭터",
            GalleryCategory.Boss => "보스",
            GalleryCategory.Event => "이벤트",
            GalleryCategory.Defeat => "패배",
            GalleryCategory.Masturbation => "자위",
            GalleryCategory.Ending => "엔딩",
            GalleryCategory.Extra => "기타",
            _ => cat.ToString()
        };
    }
}

