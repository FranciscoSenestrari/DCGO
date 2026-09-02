using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject ReplayListPanel;
    public GameObject ReplayPlayerPanel;
    public GameObject DeckViewerModal;

    [Header("Replay List UI")]
    public Transform ReplayListContent;
    public GameObject ReplayListItemPrefab;
    public TMP_Text NoReplaysText;
    public TMP_Text SelectedReplayInfoText;
    public Button PlaySelectedButton;
    public Button DeleteSelectedButton;
    public Button InspectP1DeckButton;
    public Button InspectP2DeckButton;

    [Header("Deck Viewer Modal")]
    public TMP_Text DeckViewerTitleText;
    public TMP_Text DeckViewerCountText;
    public Transform DeckViewerContent;
    public GameObject DeckCardItemPrefab;
    public Button CloseDeckViewerButton;

    [Header("Player Controller")]
    public ReplayPlayerController PlayerController;

    private List<ReplayFileInfo> _loadedReplays = new List<ReplayFileInfo>();
    private ReplayFileInfo _selectedReplayInfo = null;

    [System.Serializable]
    public class ReplayFileInfo
    {
        public string FilePath;
        public string FileName;
        public ReplayData Data;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (ReplayListPanel != null) ReplayListPanel.SetActive(true);
        if (ReplayPlayerPanel != null) ReplayPlayerPanel.SetActive(false);
        if (DeckViewerModal != null) DeckViewerModal.SetActive(false);

        if (PlaySelectedButton != null) PlaySelectedButton.interactable = false;
        if (DeleteSelectedButton != null) DeleteSelectedButton.interactable = false;
        if (InspectP1DeckButton != null) InspectP1DeckButton.interactable = false;
        if (InspectP2DeckButton != null) InspectP2DeckButton.interactable = false;

        RefreshReplayList();
    }

    public void RefreshReplayList()
    {
        _loadedReplays.Clear();
        _selectedReplayInfo = null;
        if (PlaySelectedButton != null) PlaySelectedButton.interactable = false;
        if (DeleteSelectedButton != null) DeleteSelectedButton.interactable = false;
        if (InspectP1DeckButton != null) InspectP1DeckButton.interactable = false;
        if (InspectP2DeckButton != null) InspectP2DeckButton.interactable = false;
        if (SelectedReplayInfoText != null) SelectedReplayInfoText.text = "Select a replay from the list to view details.";

        string folder = ReplayRecorder.GetReplaysFolderPath();
        if (!Directory.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }

        string[] files = Directory.GetFiles(folder, "*.json");

        // Clear existing list items in UI
        if (ReplayListContent != null)
        {
            foreach (Transform child in ReplayListContent)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (string file in files.OrderByDescending(f => File.GetCreationTime(f)))
        {
            try
            {
                string json = File.ReadAllText(file);
                ReplayData data = ReplayData.FromJson(json);
                if (data != null)
                {
                    var info = new ReplayFileInfo
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        Data = data
                    };
                    _loadedReplays.Add(info);
                    CreateReplayListItemUI(info);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to read replay file {file}: {ex.Message}");
            }
        }

        if (NoReplaysText != null)
        {
            NoReplaysText.gameObject.SetActive(_loadedReplays.Count == 0);
        }
    }

    private void CreateReplayListItemUI(ReplayFileInfo info)
    {
        if (ReplayListContent == null) return;

        GameObject itemObj = null;
        if (ReplayListItemPrefab != null)
        {
            itemObj = Instantiate(ReplayListItemPrefab, ReplayListContent);
        }
        else
        {
            // Fallback dynamic item creation if prefab not assigned
            itemObj = CreateDynamicListItem(info);
            return;
        }

        var texts = itemObj.GetComponentsInChildren<TMP_Text>();
        if (texts.Length > 0)
        {
            texts[0].text = $"{info.Data.player1Name} vs {info.Data.player2Name}";
        }
        if (texts.Length > 1)
        {
            texts[1].text = $"Date: {info.Data.matchDate} | Turns: {info.Data.totalTurns} | Winner: {info.Data.winnerPlayerName}";
        }

        var btn = itemObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() => SelectReplay(info));
        }
    }

    private GameObject CreateDynamicListItem(ReplayFileInfo info)
    {
        GameObject item = new GameObject("ReplayItem", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        item.transform.SetParent(ReplayListContent, false);

        var le = item.GetComponent<LayoutElement>();
        le.minHeight = 65;
        le.preferredHeight = 65;

        var img = item.GetComponent<Image>();
        img.color = new Color(0.15f, 0.18f, 0.25f, 0.9f);

        var btn = item.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(0.25f, 0.35f, 0.5f, 1f);
        colors.pressedColor = new Color(0.2f, 0.45f, 0.7f, 1f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(item.transform, false);
        var rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(15, 5);
        rt.offsetMax = new Vector2(-15, -5);

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(tmp, false);
        tmp.fontSize = 18;
        tmp.color = Color.white;
        string winnerTag = !string.IsNullOrEmpty(info.Data.winnerPlayerName) ? $" <color=#FFD700>[Win: {info.Data.winnerPlayerName}]</color>" : "";
        tmp.text = $"<b>{info.Data.player1Name}</b> vs <b>{info.Data.player2Name}</b>{winnerTag}\n<size=13><color=#AAAAAA>{info.Data.matchDate} | Turns: {info.Data.totalTurns} | Mode: {info.Data.gameMode}</color></size>";

        btn.onClick.AddListener(() => SelectReplay(info));
        return item;
    }

    public void SelectReplay(ReplayFileInfo info)
    {
        _selectedReplayInfo = info;
        if (PlaySelectedButton != null) PlaySelectedButton.interactable = true;
        if (DeleteSelectedButton != null) DeleteSelectedButton.interactable = true;
        if (InspectP1DeckButton != null) InspectP1DeckButton.interactable = true;
        if (InspectP2DeckButton != null) InspectP2DeckButton.interactable = true;

        if (SelectedReplayInfoText != null && info != null)
        {
            SelectedReplayInfoText.text = $"<b>{info.Data.player1Name}</b> (Deck: {info.Data.player1MainDeckCardIDs.Count} cards)\n" +
                                         $"vs <b>{info.Data.player2Name}</b> (Deck: {info.Data.player2MainDeckCardIDs.Count} cards)\n" +
                                         $"Date: {info.Data.matchDate}\n" +
                                         $"Total Turns: {info.Data.totalTurns} | Total Steps: {info.Data.steps.Count}\n" +
                                         $"Winner: <color=#FFD700>{info.Data.winnerPlayerName}</color>";
        }

        if (Opening.instance != null)
        {
            Opening.instance.PlayDecisionSE();
        }
    }

    public void OnClickPlaySelected()
    {
        if (_selectedReplayInfo == null || _selectedReplayInfo.Data == null) return;

        if (ContinuousController.instance != null)
        {
            ContinuousController.instance.StartReplayBattle(_selectedReplayInfo.Data);
            return;
        }

        if (ReplayListPanel != null) ReplayListPanel.SetActive(false);
        if (ReplayPlayerPanel != null) ReplayPlayerPanel.SetActive(true);

        if (PlayerController != null)
        {
            PlayerController.LoadReplay(_selectedReplayInfo.Data);
        }
    }

    public void OnClickDeleteSelected()
    {
        if (_selectedReplayInfo == null) return;

        try
        {
            if (File.Exists(_selectedReplayInfo.FilePath))
            {
                File.Delete(_selectedReplayInfo.FilePath);
            }
            RefreshReplayList();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to delete replay: {ex.Message}");
        }
    }

    public void OnClickInspectDeck(int playerIndex)
    {
        if (_selectedReplayInfo == null && (PlayerController == null || PlayerController.CurrentReplay == null))
            return;

        ReplayData data = PlayerController != null && PlayerController.CurrentReplay != null
            ? PlayerController.CurrentReplay
            : _selectedReplayInfo?.Data;

        if (data == null) return;

        string playerName = playerIndex == 0 ? data.player1Name : data.player2Name;
        List<string> mainCards = playerIndex == 0 ? data.player1MainDeckCardIDs : data.player2MainDeckCardIDs;
        List<string> eggCards = playerIndex == 0 ? data.player1EggDeckCardIDs : data.player2EggDeckCardIDs;

        OpenDeckViewer(playerName, mainCards, eggCards);
    }

    public void OpenDeckViewer(string playerName, List<string> mainCards, List<string> eggCards)
    {
        if (DeckViewerModal == null) return;

        DeckViewerModal.SetActive(true);
        if (DeckViewerTitleText != null)
        {
            DeckViewerTitleText.text = $"Deck: {playerName}";
        }
        if (DeckViewerCountText != null)
        {
            DeckViewerCountText.text = $"Main: {mainCards.Count} cards | Digi-Eggs: {eggCards.Count} cards";
        }

        if (DeckViewerContent != null)
        {
            foreach (Transform child in DeckViewerContent)
            {
                Destroy(child.gameObject);
            }

            // Group card counts
            var groupedMain = mainCards.GroupBy(c => c).Select(g => new { CardID = g.Key, Count = g.Count() });
            var groupedEgg = eggCards.GroupBy(c => c).Select(g => new { CardID = g.Key, Count = g.Count() });

            foreach (var item in groupedEgg)
            {
                CreateDeckCardEntry(item.CardID, item.Count, true);
            }

            foreach (var item in groupedMain)
            {
                CreateDeckCardEntry(item.CardID, item.Count, false);
            }
        }
    }

    private void CreateDeckCardEntry(string cardId, int count, bool isEgg)
    {
        if (DeckViewerContent == null) return;

        string cardName = GetCardName(cardId);
        string eggPrefix = isEgg ? "<color=#FF88AA>[Egg]</color> " : "";

        GameObject cardObj = new GameObject("DeckCard", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        cardObj.transform.SetParent(DeckViewerContent, false);

        var le = cardObj.GetComponent<LayoutElement>();
        le.minHeight = 35;
        le.preferredHeight = 35;

        var img = cardObj.GetComponent<Image>();
        img.color = isEgg ? new Color(0.25f, 0.15f, 0.2f, 0.85f) : new Color(0.12f, 0.15f, 0.22f, 0.85f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(cardObj.transform, false);
        var rt = textObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10, 2);
        rt.offsetMax = new Vector2(-10, -2);

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(tmp, false);
        tmp.fontSize = 15;
        tmp.color = Color.white;
        tmp.text = $"{eggPrefix}<b>{count}x</b> {cardId} - {cardName}";
    }

    private string GetCardName(string cardId)
    {
        if (ContinuousController.instance != null && ContinuousController.instance.CardList != null)
        {
            var entity = Array.Find(ContinuousController.instance.CardList, c => c != null && c.CardID == cardId);
            if (entity != null)
            {
                return !string.IsNullOrEmpty(entity.CardName_ENG) ? entity.CardName_ENG : entity.CardName_JPN;
            }
        }
        return "";
    }

    public void CloseDeckViewer()
    {
        if (DeckViewerModal != null)
        {
            DeckViewerModal.SetActive(false);
        }
    }

    public void BackToReplayList()
    {
        if (PlayerController != null)
        {
            PlayerController.Pause();
        }

        if (ReplayPlayerPanel != null) ReplayPlayerPanel.SetActive(false);
        if (ReplayListPanel != null) ReplayListPanel.SetActive(true);

        RefreshReplayList();
    }

    public void ReturnToOpeningScene()
    {
        SceneManager.LoadScene("Opening");
    }
}
