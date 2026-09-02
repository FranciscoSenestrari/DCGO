using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReplaySceneSetup : MonoBehaviour
{
    private static TMP_FontAsset _defaultFontBold;
    private static TMP_FontAsset _defaultFontRegular;

    public static TMP_FontAsset GetFontAsset(bool bold = false)
    {
        if (bold)
        {
            if (_defaultFontBold == null)
                _defaultFontBold = Resources.Load<TMP_FontAsset>("Fonts & Materials/Play-Bold SDF");
            if (_defaultFontBold != null) return _defaultFontBold;
        }

        if (_defaultFontRegular == null)
            _defaultFontRegular = Resources.Load<TMP_FontAsset>("Fonts & Materials/Play-Regular SDF");
        if (_defaultFontRegular != null) return _defaultFontRegular;

        if (_defaultFontBold == null)
            _defaultFontBold = Resources.Load<TMP_FontAsset>("Fonts & Materials/Play-Bold SDF");
        if (_defaultFontBold != null) return _defaultFontBold;

        return TMP_Settings.defaultFontAsset;
    }

    public static void ApplyFont(TextMeshProUGUI tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = GetFontAsset(bold);
        if (font != null)
        {
            tmp.font = font;
        }
    }

    public static void ApplyFont(TMP_Text tmp, bool bold = false)
    {
        if (tmp == null) return;
        TMP_FontAsset font = GetFontAsset(bold);
        if (font != null)
        {
            tmp.font = font;
        }
    }

    private void Awake()
    {
        SetupSceneHierarchy();
    }

    private void SetupSceneHierarchy()
    {
        // 1. Ensure EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        // 2. Ensure Camera
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camObj = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            mainCam = camObj.GetComponent<Camera>();
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
            camObj.transform.position = new Vector3(0, 0, -10);
        }

        // 3. Canvas
        GameObject canvasObj = new GameObject("ReplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // 4. Background
        GameObject bgObj = new GameObject("ReplayBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(canvasObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;
        bgObj.GetComponent<Image>().color = new Color(0.08f, 0.10f, 0.15f, 1f);

        // 5. ReplayManager and PlayerController
        GameObject mgrObj = new GameObject("ReplayManager", typeof(ReplayManager), typeof(ReplayPlayerController));
        ReplayManager mgr = mgrObj.GetComponent<ReplayManager>();
        ReplayPlayerController playerCtrl = mgrObj.GetComponent<ReplayPlayerController>();
        mgr.PlayerController = playerCtrl;

        // 6. Header
        CreateHeader(canvasObj.transform, mgr);

        // 7. Replay List Panel
        GameObject listPanel = CreateListPanel(canvasObj.transform, mgr);
        mgr.ReplayListPanel = listPanel;

        // 8. Replay Player Panel
        GameObject playerPanel = CreatePlayerPanel(canvasObj.transform, mgr, playerCtrl);
        mgr.ReplayPlayerPanel = playerPanel;

        // 9. Deck Viewer Modal
        GameObject deckModal = CreateDeckViewerModal(canvasObj.transform, mgr);
        mgr.DeckViewerModal = deckModal;

        listPanel.SetActive(true);
        playerPanel.SetActive(false);
        deckModal.SetActive(false);
    }

    private void CreateHeader(Transform parent, ReplayManager mgr)
    {
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(Image));
        header.transform.SetParent(parent, false);
        var rt = header.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.sizeDelta = new Vector2(0, 70);
        rt.anchoredPosition = Vector2.zero;
        header.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 0.95f);

        // Title
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(header.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.offsetMin = new Vector2(30, 0);
        titleRt.offsetMax = Vector2.zero;
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(titleTmp, true);
        titleTmp.text = "<b><color=#5dade2>DCGO</color> MATCH REPLAYS</b>";
        titleTmp.fontSize = 28;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.color = Color.white;

        // Return to Main Menu Button
        GameObject returnBtnObj = CreateButton(header.transform, "Back to Menu", new Vector2(180, 45), new Color(0.6f, 0.2f, 0.2f, 1f));
        var returnRt = returnBtnObj.GetComponent<RectTransform>();
        returnRt.anchorMin = new Vector2(1, 0.5f);
        returnRt.anchorMax = new Vector2(1, 0.5f);
        returnRt.pivot = new Vector2(1, 0.5f);
        returnRt.anchoredPosition = new Vector2(-30, 0);
        returnBtnObj.GetComponent<Button>().onClick.AddListener(() => mgr.ReturnToOpeningScene());
    }

    private GameObject CreateListPanel(Transform parent, ReplayManager mgr)
    {
        GameObject panel = new GameObject("ReplayListPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40, 40);
        rt.offsetMax = new Vector2(-40, -90);

        // Left Container: Scroll List
        GameObject scrollObj = new GameObject("ScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(panel.transform, false);
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0, 0);
        scrollRt.anchorMax = new Vector2(0.65f, 1);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        scrollObj.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.09f, 0.8f);

        // Content
        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        var contentRt = contentObj.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot = new Vector2(0.5f, 1);
        contentRt.sizeDelta = new Vector2(0, 0);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollObj.GetComponent<ScrollRect>();
        scrollRect.content = contentRt;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        mgr.ReplayListContent = contentRt;

        // No Replays Text
        GameObject noRepObj = new GameObject("NoReplaysText", typeof(RectTransform), typeof(TextMeshProUGUI));
        noRepObj.transform.SetParent(scrollObj.transform, false);
        var noRepRt = noRepObj.GetComponent<RectTransform>();
        noRepRt.anchorMin = Vector2.zero;
        noRepRt.anchorMax = Vector2.one;
        var noRepTmp = noRepObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(noRepTmp, false);
        noRepTmp.text = "No saved replays found.\nPlay a match to record one automatically!";
        noRepTmp.fontSize = 20;
        noRepTmp.alignment = TextAlignmentOptions.Center;
        noRepTmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        mgr.NoReplaysText = noRepTmp;

        // Right Container: Replay Info & Actions
        GameObject rightBox = new GameObject("RightBox", typeof(RectTransform), typeof(Image));
        rightBox.transform.SetParent(panel.transform, false);
        var rightRt = rightBox.GetComponent<RectTransform>();
        rightRt.anchorMin = new Vector2(0.67f, 0);
        rightRt.anchorMax = new Vector2(1, 1);
        rightRt.offsetMin = Vector2.zero;
        rightRt.offsetMax = Vector2.zero;
        rightBox.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.13f, 0.9f);

        // Info Text
        GameObject infoObj = new GameObject("InfoText", typeof(RectTransform), typeof(TextMeshProUGUI));
        infoObj.transform.SetParent(rightBox.transform, false);
        var infoRt = infoObj.GetComponent<RectTransform>();
        infoRt.anchorMin = new Vector2(0, 0.45f);
        infoRt.anchorMax = new Vector2(1, 1);
        infoRt.offsetMin = new Vector2(25, 20);
        infoRt.offsetMax = new Vector2(-25, -25);
        var infoTmp = infoObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(infoTmp, false);
        infoTmp.fontSize = 18;
        infoTmp.color = Color.white;
        infoTmp.alignment = TextAlignmentOptions.TopLeft;
        infoTmp.text = "Select a replay to view match details and inspect player decks.";
        mgr.SelectedReplayInfoText = infoTmp;

        // Buttons Container
        GameObject btnContainer = new GameObject("Buttons", typeof(RectTransform), typeof(VerticalLayoutGroup));
        btnContainer.transform.SetParent(rightBox.transform, false);
        var btnRt = btnContainer.GetComponent<RectTransform>();
        btnRt.anchorMin = new Vector2(0, 0);
        btnRt.anchorMax = new Vector2(1, 0.45f);
        btnRt.offsetMin = new Vector2(25, 25);
        btnRt.offsetMax = new Vector2(-25, 0);

        var btnVlg = btnContainer.GetComponent<VerticalLayoutGroup>();
        btnVlg.spacing = 10;
        btnVlg.childControlWidth = true;
        btnVlg.childControlHeight = true;
        btnVlg.childForceExpandWidth = true;
        btnVlg.childForceExpandHeight = true;

        // Play Button
        GameObject playBtn = CreateButton(btnContainer.transform, "▶ Watch Replay", Vector2.zero, new Color(0.2f, 0.6f, 0.35f, 1f));
        mgr.PlaySelectedButton = playBtn.GetComponent<Button>();
        mgr.PlaySelectedButton.onClick.AddListener(() => mgr.OnClickPlaySelected());

        // Inspect P1 Deck Button
        GameObject p1DeckBtn = CreateButton(btnContainer.transform, "🃏 Inspect Player 1 Deck", Vector2.zero, new Color(0.2f, 0.4f, 0.65f, 1f));
        mgr.InspectP1DeckButton = p1DeckBtn.GetComponent<Button>();
        mgr.InspectP1DeckButton.onClick.AddListener(() => mgr.OnClickInspectDeck(0));

        // Inspect P2 Deck Button
        GameObject p2DeckBtn = CreateButton(btnContainer.transform, "🃏 Inspect Player 2 Deck", Vector2.zero, new Color(0.5f, 0.3f, 0.65f, 1f));
        mgr.InspectP2DeckButton = p2DeckBtn.GetComponent<Button>();
        mgr.InspectP2DeckButton.onClick.AddListener(() => mgr.OnClickInspectDeck(1));

        // Delete Replay Button
        GameObject delBtn = CreateButton(btnContainer.transform, "🗑 Delete Replay", Vector2.zero, new Color(0.5f, 0.2f, 0.2f, 1f));
        mgr.DeleteSelectedButton = delBtn.GetComponent<Button>();
        mgr.DeleteSelectedButton.onClick.AddListener(() => mgr.OnClickDeleteSelected());

        return panel;
    }

    private GameObject CreatePlayerPanel(Transform parent, ReplayManager mgr, ReplayPlayerController ctrl)
    {
        GameObject panel = new GameObject("ReplayPlayerPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(40, 30);
        rt.offsetMax = new Vector2(-40, -85);

        // Top Status Bar
        GameObject topBar = new GameObject("TopStatusBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(panel.transform, false);
        var topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = new Vector2(0.68f, 1);
        topRt.pivot = new Vector2(0, 1);
        topRt.sizeDelta = new Vector2(0, 75);
        topRt.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.95f);

        // Turn / Active / Memory / Step
        GameObject statusTextObj = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusTextObj.transform.SetParent(topBar.transform, false);
        var statusRt = statusTextObj.GetComponent<RectTransform>();
        statusRt.anchorMin = Vector2.zero;
        statusRt.anchorMax = Vector2.one;
        statusRt.offsetMin = new Vector2(15, 5);
        statusRt.offsetMax = new Vector2(-15, -5);
        var statusTmp = statusTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(statusTmp, false);
        statusTmp.fontSize = 18;
        statusTmp.color = Color.white;
        statusTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ctrl.TurnText = statusTmp;
        ctrl.ActivePlayerText = statusTmp;
        ctrl.MemoryText = statusTmp;
        ctrl.StepCounterText = statusTmp;

        // Middle Arena (Player 1 and Player 2 Box)
        GameObject arenaObj = new GameObject("Arena", typeof(RectTransform));
        arenaObj.transform.SetParent(panel.transform, false);
        var arenaRt = arenaObj.GetComponent<RectTransform>();
        arenaRt.anchorMin = new Vector2(0, 0.22f);
        arenaRt.anchorMax = new Vector2(0.68f, 0.90f);
        arenaRt.offsetMin = Vector2.zero;
        arenaRt.offsetMax = Vector2.zero;

        // Player 2 Box (Top)
        GameObject p2Box = CreatePlayerDisplayBox(arenaObj.transform, "Player 2", new Vector2(0, 0.52f), new Vector2(1, 1), ctrl, 1);
        // Player 1 Box (Bottom)
        GameObject p1Box = CreatePlayerDisplayBox(arenaObj.transform, "Player 1", new Vector2(0, 0), new Vector2(1, 0.48f), ctrl, 0);

        // Action Description Banner
        GameObject actionBanner = new GameObject("ActionBanner", typeof(RectTransform), typeof(Image));
        actionBanner.transform.SetParent(panel.transform, false);
        var actRt = actionBanner.GetComponent<RectTransform>();
        actRt.anchorMin = new Vector2(0, 0.12f);
        actRt.anchorMax = new Vector2(0.68f, 0.21f);
        actRt.offsetMin = Vector2.zero;
        actRt.offsetMax = Vector2.zero;
        actionBanner.GetComponent<Image>().color = new Color(0.1f, 0.15f, 0.25f, 0.95f);

        GameObject actTextObj = new GameObject("ActionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        actTextObj.transform.SetParent(actionBanner.transform, false);
        var actTextRt = actTextObj.GetComponent<RectTransform>();
        actTextRt.anchorMin = Vector2.zero;
        actTextRt.anchorMax = Vector2.one;
        actTextRt.offsetMin = new Vector2(15, 0);
        actTextRt.offsetMax = new Vector2(-15, 0);
        var actTmp = actTextObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(actTmp, false);
        actTmp.fontSize = 17;
        actTmp.color = new Color(1f, 0.9f, 0.5f, 1f);
        actTmp.alignment = TextAlignmentOptions.MidlineLeft;
        ctrl.ActionDescriptionText = actTmp;

        // Bottom Controls Bar
        GameObject botBar = new GameObject("BottomControls", typeof(RectTransform), typeof(Image));
        botBar.transform.SetParent(panel.transform, false);
        var botRt = botBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0, 0);
        botRt.anchorMax = new Vector2(0.68f, 0.10f);
        botRt.offsetMin = Vector2.zero;
        botRt.offsetMax = Vector2.zero;
        botBar.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.14f, 0.95f);

        // Toggles: Show Hand & Show Security
        GameObject toggleContainer = new GameObject("Toggles", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        toggleContainer.transform.SetParent(botBar.transform, false);
        var togRt = toggleContainer.GetComponent<RectTransform>();
        togRt.anchorMin = new Vector2(0, 0);
        togRt.anchorMax = new Vector2(0.35f, 1);
        togRt.offsetMin = new Vector2(10, 5);
        togRt.offsetMax = new Vector2(-10, -5);

        var togHlg = toggleContainer.GetComponent<HorizontalLayoutGroup>();
        togHlg.spacing = 15;
        togHlg.childControlWidth = true;
        togHlg.childControlHeight = true;

        ctrl.ShowHandToggle = CreateToggle(toggleContainer.transform, "Show Hand");
        ctrl.ShowSecurityToggle = CreateToggle(toggleContainer.transform, "Show Security");

        // Playback Buttons
        GameObject pbContainer = new GameObject("PlaybackButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        pbContainer.transform.SetParent(botBar.transform, false);
        var pbRt = pbContainer.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0.36f, 0);
        pbRt.anchorMax = new Vector2(0.85f, 1);
        pbRt.offsetMin = new Vector2(5, 8);
        pbRt.offsetMax = new Vector2(-5, -8);

        var pbHlg = pbContainer.GetComponent<HorizontalLayoutGroup>();
        pbHlg.spacing = 8;
        pbHlg.childControlWidth = true;
        pbHlg.childControlHeight = true;

        GameObject restartBtn = CreateButton(pbContainer.transform, "⏮", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f));
        restartBtn.GetComponent<Button>().onClick.AddListener(() => ctrl.Restart());

        GameObject prevBtn = CreateButton(pbContainer.transform, "◀ Prev", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f));
        prevBtn.GetComponent<Button>().onClick.AddListener(() => ctrl.StepBackward());

        GameObject playPauseBtn = CreateButton(pbContainer.transform, "Play", Vector2.zero, new Color(0.2f, 0.6f, 0.35f, 1f));
        ctrl.PlayPauseButton = playPauseBtn.GetComponent<Button>();
        ctrl.PlayPauseButtonText = playPauseBtn.GetComponentInChildren<TMP_Text>();
        ctrl.PlayPauseButton.onClick.AddListener(() => ctrl.TogglePlayPause());

        GameObject nextBtn = CreateButton(pbContainer.transform, "Next ▶", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f));
        nextBtn.GetComponent<Button>().onClick.AddListener(() => ctrl.StepForward());

        // Back to list button
        GameObject backBtn = CreateButton(botBar.transform, "Exit Replay", Vector2.zero, new Color(0.5f, 0.2f, 0.2f, 1f));
        var backRt = backBtn.GetComponent<RectTransform>();
        backRt.anchorMin = new Vector2(0.86f, 0);
        backRt.anchorMax = new Vector2(1, 1);
        backRt.offsetMin = new Vector2(5, 8);
        backRt.offsetMax = new Vector2(-10, -8);
        backBtn.GetComponent<Button>().onClick.AddListener(() => mgr.BackToReplayList());

        // Right Container: Action Log History
        GameObject logArea = new GameObject("LogHistoryArea", typeof(RectTransform), typeof(Image));
        logArea.transform.SetParent(panel.transform, false);
        var logRt = logArea.GetComponent<RectTransform>();
        logRt.anchorMin = new Vector2(0.70f, 0);
        logRt.anchorMax = new Vector2(1, 1);
        logRt.offsetMin = Vector2.zero;
        logRt.offsetMax = Vector2.zero;
        logArea.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.11f, 0.95f);

        // Log Header
        GameObject logHeader = new GameObject("LogHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        logHeader.transform.SetParent(logArea.transform, false);
        var logHdrRt = logHeader.GetComponent<RectTransform>();
        logHdrRt.anchorMin = new Vector2(0, 1);
        logHdrRt.anchorMax = new Vector2(1, 1);
        logHdrRt.pivot = new Vector2(0.5f, 1);
        logHdrRt.sizeDelta = new Vector2(-20, 35);
        logHdrRt.anchoredPosition = new Vector2(0, -5);
        var logHdrTmp = logHeader.GetComponent<TextMeshProUGUI>();
        ApplyFont(logHdrTmp, true);
        logHdrTmp.text = "<b>MATCH HISTORY (CLICK TO JUMP)</b>";
        logHdrTmp.fontSize = 15;
        logHdrTmp.alignment = TextAlignmentOptions.Center;
        logHdrTmp.color = new Color(0.7f, 0.85f, 1f, 1f);

        // Log Scroll
        GameObject logScroll = new GameObject("LogScroll", typeof(RectTransform), typeof(ScrollRect));
        logScroll.transform.SetParent(logArea.transform, false);
        var logScRt = logScroll.GetComponent<RectTransform>();
        logScRt.anchorMin = Vector2.zero;
        logScRt.anchorMax = Vector2.one;
        logScRt.offsetMin = new Vector2(10, 10);
        logScRt.offsetMax = new Vector2(-10, -45);

        GameObject logContent = new GameObject("LogContent", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        logContent.transform.SetParent(logScroll.transform, false);
        var logCntRt = logContent.GetComponent<RectTransform>();
        logCntRt.anchorMin = new Vector2(0, 1);
        logCntRt.anchorMax = new Vector2(1, 1);
        logCntRt.pivot = new Vector2(0.5f, 1);

        var logVlg = logContent.GetComponent<VerticalLayoutGroup>();
        logVlg.spacing = 3;
        logVlg.childControlWidth = true;
        logVlg.childControlHeight = false;
        logVlg.childForceExpandWidth = true;

        var logCsf = logContent.GetComponent<ContentSizeFitter>();
        logCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = logScroll.GetComponent<ScrollRect>();
        sr.content = logCntRt;
        sr.horizontal = false;
        sr.vertical = true;
        ctrl.LogContent = logCntRt;
        ctrl.LogScrollRect = sr;

        return panel;
    }

    private GameObject CreatePlayerDisplayBox(Transform parent, string defaultName, Vector2 anchorMin, Vector2 anchorMax, ReplayPlayerController ctrl, int playerIndex)
    {
        GameObject box = new GameObject($"Box_P{playerIndex + 1}", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(parent, false);
        var rt = box.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        box.GetComponent<Image>().color = playerIndex == 0 ? new Color(0.08f, 0.12f, 0.2f, 0.9f) : new Color(0.18f, 0.10f, 0.14f, 0.9f);

        // Header (Name & Inspect Deck Button)
        GameObject nameObj = new GameObject("PlayerName", typeof(RectTransform), typeof(TextMeshProUGUI));
        nameObj.transform.SetParent(box.transform, false);
        var nameRt = nameObj.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(0.6f, 1);
        nameRt.pivot = new Vector2(0, 1);
        nameRt.sizeDelta = new Vector2(0, 35);
        nameRt.anchoredPosition = new Vector2(15, -10);
        var nameTmp = nameObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(nameTmp, true);
        nameTmp.text = $"<b>{defaultName}</b>";
        nameTmp.fontSize = 20;
        nameTmp.color = Color.white;
        if (playerIndex == 0) ctrl.P1NameText = nameTmp; else ctrl.P2NameText = nameTmp;

        // Inspect Deck Button
        GameObject inspectBtn = CreateButton(box.transform, "🃏 Inspect Deck", new Vector2(140, 30), new Color(0.25f, 0.35f, 0.5f, 1f));
        var inspRt = inspectBtn.GetComponent<RectTransform>();
        inspRt.anchorMin = new Vector2(1, 1);
        inspRt.anchorMax = new Vector2(1, 1);
        inspRt.pivot = new Vector2(1, 1);
        inspRt.anchoredPosition = new Vector2(-15, -10);
        inspectBtn.GetComponent<Button>().onClick.AddListener(() => ReplayManager.Instance.OnClickInspectDeck(playerIndex));

        // Hand Cards Box
        GameObject handObj = new GameObject("HandText", typeof(RectTransform), typeof(TextMeshProUGUI));
        handObj.transform.SetParent(box.transform, false);
        var handRt = handObj.GetComponent<RectTransform>();
        handRt.anchorMin = new Vector2(0, 0.45f);
        handRt.anchorMax = new Vector2(1, 0.85f);
        handRt.offsetMin = new Vector2(15, 0);
        handRt.offsetMax = new Vector2(-15, 0);
        var handTmp = handObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(handTmp, false);
        handTmp.fontSize = 14;
        handTmp.color = new Color(0.85f, 0.9f, 1f, 1f);
        handTmp.text = "<b>Hand:</b> (Hidden)";
        if (playerIndex == 0) ctrl.P1HandText = handTmp; else ctrl.P2HandText = handTmp;

        // Security Cards Box
        GameObject secObj = new GameObject("SecurityText", typeof(RectTransform), typeof(TextMeshProUGUI));
        secObj.transform.SetParent(box.transform, false);
        var secRt = secObj.GetComponent<RectTransform>();
        secRt.anchorMin = new Vector2(0, 0.05f);
        secRt.anchorMax = new Vector2(1, 0.42f);
        secRt.offsetMin = new Vector2(15, 0);
        secRt.offsetMax = new Vector2(-15, 0);
        var secTmp = secObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(secTmp, false);
        secTmp.fontSize = 14;
        secTmp.color = new Color(1f, 0.85f, 0.85f, 1f);
        secTmp.text = "<b>Security:</b> (Hidden)";
        if (playerIndex == 0) ctrl.P1SecurityText = secTmp; else ctrl.P2SecurityText = secTmp;

        return box;
    }

    private GameObject CreateDeckViewerModal(Transform parent, ReplayManager mgr)
    {
        GameObject modal = new GameObject("DeckViewerModal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(parent, false);
        var rt = modal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0, 0, 0, 0.85f);

        // Center Box
        GameObject centerBox = new GameObject("CenterBox", typeof(RectTransform), typeof(Image));
        centerBox.transform.SetParent(modal.transform, false);
        var boxRt = centerBox.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(700, 750);
        centerBox.GetComponent<Image>().color = new Color(0.08f, 0.11f, 0.17f, 1f);

        // Title
        GameObject titleObj = new GameObject("DeckTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(centerBox.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 1);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.sizeDelta = new Vector2(-40, 45);
        titleRt.anchoredPosition = new Vector2(0, -15);
        var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(titleTmp, true);
        titleTmp.text = "Deck Details";
        titleTmp.fontSize = 24;
        titleTmp.alignment = TextAlignmentOptions.Center;
        titleTmp.color = Color.white;
        mgr.DeckViewerTitleText = titleTmp;

        // Card count summary
        GameObject countObj = new GameObject("DeckCount", typeof(RectTransform), typeof(TextMeshProUGUI));
        countObj.transform.SetParent(centerBox.transform, false);
        var countRt = countObj.GetComponent<RectTransform>();
        countRt.anchorMin = new Vector2(0, 1);
        countRt.anchorMax = new Vector2(1, 1);
        countRt.pivot = new Vector2(0.5f, 1);
        countRt.sizeDelta = new Vector2(-40, 30);
        countRt.anchoredPosition = new Vector2(0, -60);
        var countTmp = countObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(countTmp, false);
        countTmp.text = "Main Deck: 50 | Digi-Eggs: 5";
        countTmp.fontSize = 15;
        countTmp.alignment = TextAlignmentOptions.Center;
        countTmp.color = new Color(0.7f, 0.85f, 1f, 1f);
        mgr.DeckViewerCountText = countTmp;

        // Card Scroll Area
        GameObject scrollObj = new GameObject("CardScroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollObj.transform.SetParent(centerBox.transform, false);
        var scRt = scrollObj.GetComponent<RectTransform>();
        scRt.anchorMin = Vector2.zero;
        scRt.anchorMax = Vector2.one;
        scRt.offsetMin = new Vector2(25, 80);
        scRt.offsetMax = new Vector2(-25, -95);
        scrollObj.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.1f, 0.8f);

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        var cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0, 1);
        cntRt.anchorMax = new Vector2(1, 1);
        cntRt.pivot = new Vector2(0.5f, 1);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 4;
        vlg.padding = new RectOffset(5, 5, 5, 5);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollObj.GetComponent<ScrollRect>();
        sr.content = cntRt;
        sr.horizontal = false;
        sr.vertical = true;
        mgr.DeckViewerContent = cntRt;

        // Close Button
        GameObject closeBtn = CreateButton(centerBox.transform, "Close", new Vector2(180, 45), new Color(0.5f, 0.2f, 0.2f, 1f));
        var closeRt = closeBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.5f, 0);
        closeRt.anchorMax = new Vector2(0.5f, 0);
        closeRt.pivot = new Vector2(0.5f, 0);
        closeRt.anchoredPosition = new Vector2(0, 18);
        closeBtn.GetComponent<Button>().onClick.AddListener(() => mgr.CloseDeckViewer());

        return modal;
    }

    private GameObject CreateButton(Transform parent, string label, Vector2 size, Color bgColor)
    {
        GameObject btnObj = new GameObject($"Btn_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        if (size != Vector2.zero)
        {
            var rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = size;
        }

        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        var btn = btnObj.GetComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = bgColor * 1.25f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(tmp, true);
        tmp.text = label;
        tmp.fontSize = 16;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private Toggle CreateToggle(Transform parent, string label)
    {
        GameObject togObj = new GameObject($"Toggle_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Toggle));
        togObj.transform.SetParent(parent, false);

        var tog = togObj.GetComponent<Toggle>();

        // Background box
        GameObject bgObj = new GameObject("ToggleBackground", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(togObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.sizeDelta = new Vector2(34, 34);
        bgRt.anchoredPosition = new Vector2(20, 0);
        bgObj.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);

        // Checkmark
        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.5f, 0.5f);
        checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(24, 24);
        checkRt.anchoredPosition = Vector2.zero;
        checkObj.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.4f, 1f);

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(togObj.transform, false);
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(1, 1);
        labelRt.offsetMin = new Vector2(45, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        ApplyFont(labelTmp, false);
        labelTmp.text = label;
        labelTmp.fontSize = 16;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;

        tog.targetGraphic = bgObj.GetComponent<Image>();
        tog.graphic = checkObj.GetComponent<Image>();
        tog.isOn = false;

        return tog;
    }
}
