using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayBattleController : MonoBehaviour
{
    public static ReplayBattleController Instance { get; private set; }

    [Header("Replay Data")]
    public ReplayData CurrentReplay;
    public int CurrentStepIndex = 0;
    public bool IsPlaying = false;
    public float PlaybackSpeed = 1f;

    [Header("Visibility Options")]
    public bool ShowOpponentHand = true;
    public bool ShowSecurityCards = true;

    [Header("UI Elements")]
    public Canvas BattleCanvas;
    public GameObject ReplayUIRoot;
    public TextMeshProUGUI MatchTitleText;
    public TextMeshProUGUI TurnText;
    public TextMeshProUGUI MemoryText;
    public TextMeshProUGUI StepCounterText;
    public TextMeshProUGUI ActionDescriptionText;
    public Slider TimelineSlider;
    public Button PlayPauseButton;
    public TextMeshProUGUI PlayPauseButtonText;
    public Button PrevStepButton;
    public Button NextStepButton;
    public Button RestartButton;
    public TMP_Dropdown SpeedDropdown;
    public Toggle OpponentHandToggle;
    public Toggle SecurityToggle;
    public Button ExitButton;
    public Button ToggleLogsButton;
    public GameObject LogsDrawer;
    public Transform LogsContent;
    public ScrollRect LogsScrollRect;

    // Cache of CardSources by CardID to avoid duplicate allocations
    private Dictionary<string, CardSource> _cardSourceCache = new Dictionary<string, CardSource>();
    private List<GameObject> _spawnedVisuals = new List<GameObject>();
    private Coroutine _playCoroutine;
    private bool _isScrubbing = false;

    private void Awake()
    {
        Instance = this;
    }

    public void Init(ReplayData replay)
    {
        if (replay == null || replay.steps == null || replay.steps.Count == 0)
        {
            OnClickExitReplay();
            return;
        }

        CurrentReplay = replay;
        CurrentStepIndex = 0;
        IsPlaying = false;

        BuildReplayUI();

        // Setup Player Names in GameContext & UI
        if (GManager.instance != null)
        {
            if (GManager.instance.You != null)
            {
                GManager.instance.You.PlayerName = replay.player1Name;
                if (GManager.instance.You.PlayerNameText != null)
                {
                    GManager.instance.You.PlayerNameText.transform.parent.gameObject.SetActive(true);
                    GManager.instance.You.PlayerNameText.gameObject.SetActive(true);
                    GManager.instance.You.PlayerNameText.text = replay.player1Name;
                }
            }

            if (GManager.instance.Opponent != null)
            {
                GManager.instance.Opponent.PlayerName = replay.player2Name;
                if (GManager.instance.Opponent.PlayerNameText != null)
                {
                    GManager.instance.Opponent.PlayerNameText.transform.parent.gameObject.SetActive(true);
                    GManager.instance.Opponent.PlayerNameText.gameObject.SetActive(true);
                    GManager.instance.Opponent.PlayerNameText.text = replay.player2Name;
                }
            }
        }

        PopulateLogsDrawer();
        ApplyStep(0, false);
    }

    private void ShowReplaySelectionModal()
    {
        if (GManager.instance == null || GManager.instance.canvas == null) return;

        GameObject modal = new GameObject("ReplaySelectionModal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(GManager.instance.canvas.transform, false);
        var rt = modal.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.12f, 0.95f);

        GameObject cardPanel = new GameObject("CardPanel", typeof(RectTransform), typeof(Image));
        cardPanel.transform.SetParent(modal.transform, false);
        var cardRt = cardPanel.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.sizeDelta = new Vector2(680, 520);
        cardPanel.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.18f, 1f);

        // Header Title
        GameObject header = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        header.transform.SetParent(cardPanel.transform, false);
        var hRt = header.GetComponent<RectTransform>();
        hRt.anchorMin = new Vector2(0.05f, 0.88f);
        hRt.anchorMax = new Vector2(0.95f, 0.98f);
        hRt.offsetMin = Vector2.zero;
        hRt.offsetMax = Vector2.zero;
        var hTmp = header.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(hTmp, true);
        hTmp.text = "SELECT A MATCH REPLAY";
        hTmp.fontSize = 22;
        hTmp.color = new Color(0.3f, 0.8f, 1f, 1f);
        hTmp.alignment = TextAlignmentOptions.Center;

        // Scroll Area
        GameObject scrollObj = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        scrollObj.transform.SetParent(cardPanel.transform, false);
        var scrollRt = scrollObj.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0.05f, 0.15f);
        scrollRt.anchorMax = new Vector2(0.95f, 0.86f);
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;
        scrollObj.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 1f);

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(scrollObj.transform, false);
        var contRt = content.GetComponent<RectTransform>();
        contRt.anchorMin = new Vector2(0, 1);
        contRt.anchorMax = new Vector2(1, 1);
        contRt.pivot = new Vector2(0.5f, 1);
        contRt.sizeDelta = new Vector2(0, 0);

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = scrollObj.GetComponent<ScrollRect>();
        sr.content = contRt;
        sr.horizontal = false;
        sr.vertical = true;

        // Bottom Exit Button
        var exitObj = CreateButton(cardPanel.transform, "Return to Menu", new Vector2(200, 38), new Color(0.6f, 0.2f, 0.25f, 1f));
        var exitRt = exitObj.GetComponent<RectTransform>();
        exitRt.anchorMin = new Vector2(0.5f, 0.03f);
        exitRt.anchorMax = new Vector2(0.5f, 0.03f);
        exitRt.anchoredPosition = new Vector2(0, 20);
        exitObj.GetComponent<Button>().onClick.AddListener(OnClickExitReplay);

        // Load Replay Files
        string folder = ReplayRecorder.GetReplaysFolderPath();
        List<ReplayData> loaded = new List<ReplayData>();
        if (System.IO.Directory.Exists(folder))
        {
            string[] files = System.IO.Directory.GetFiles(folder, "*.json");
            foreach (var f in files.OrderByDescending(x => System.IO.File.GetCreationTime(x)))
            {
                try
                {
                    string json = System.IO.File.ReadAllText(f);
                    ReplayData d = ReplayData.FromJson(json);
                    if (d != null && d.steps != null && d.steps.Count > 0) loaded.Add(d);
                }
                catch {}
            }
        }

        if (loaded.Count == 0)
        {
            GameObject emptyText = new GameObject("Empty", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyText.transform.SetParent(content.transform, false);
            var etTmp = emptyText.GetComponent<TextMeshProUGUI>();
            ReplaySceneSetup.ApplyFont(etTmp, false);
            etTmp.text = "No saved match replays found.\nPlay a match to record and watch replays here!";
            etTmp.fontSize = 16;
            etTmp.alignment = TextAlignmentOptions.Center;
            etTmp.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        }
        else
        {
            foreach (var rep in loaded)
            {
                var rData = rep;
                var itemObj = CreateButton(content.transform, $"{rData.player1Name} vs {rData.player2Name}  -  Turns: {rData.totalTurns}  ({rData.matchDate})", new Vector2(0, 48), new Color(0.12f, 0.18f, 0.28f, 1f));
                var btn = itemObj.GetComponent<Button>();
                btn.onClick.AddListener(() =>
                {
                    Destroy(modal);
                    Init(rData);
                });
            }
        }
    }

    private void Update()
    {
        // Keyboard Shortcuts for comfortable replay control
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TogglePlayPause();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
        {
            StepForward();
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
        {
            StepBackward();
        }
        else if (Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
    }

    #region Replay UI Construction

    private void BuildReplayUI()
    {
        if (GManager.instance == null || GManager.instance.canvas == null) return;
        BattleCanvas = GManager.instance.canvas;

        ReplayUIRoot = new GameObject("ReplayUIRoot", typeof(RectTransform));
        ReplayUIRoot.transform.SetParent(BattleCanvas.transform, false);
        var rootRt = ReplayUIRoot.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;

        // 1. Top Control Bar
        GameObject topBar = new GameObject("TopControlBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(ReplayUIRoot.transform, false);
        var topRt = topBar.GetComponent<RectTransform>();
        topRt.anchorMin = new Vector2(0, 1);
        topRt.anchorMax = new Vector2(1, 1);
        topRt.pivot = new Vector2(0.5f, 1);
        topRt.sizeDelta = new Vector2(0, 75);
        topRt.anchoredPosition = Vector2.zero;
        topBar.GetComponent<Image>().color = new Color(0.05f, 0.08f, 0.14f, 0.95f);

        // Title & Turn Info
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(topBar.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0);
        titleRt.anchorMax = new Vector2(0.35f, 1);
        titleRt.offsetMin = new Vector2(20, 0);
        titleRt.offsetMax = Vector2.zero;
        MatchTitleText = titleObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(MatchTitleText, true);
        MatchTitleText.fontSize = 20;
        MatchTitleText.alignment = TextAlignmentOptions.MidlineLeft;
        MatchTitleText.color = Color.white;
        MatchTitleText.text = $"<b>{CurrentReplay.player1Name}</b> <color=#5dade2>vs</color> <b>{CurrentReplay.player2Name}</b>";

        // Action Description Banner (Center of Top Bar)
        GameObject descObj = new GameObject("ActionDescriptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        descObj.transform.SetParent(topBar.transform, false);
        var descRt = descObj.GetComponent<RectTransform>();
        descRt.anchorMin = new Vector2(0.36f, 0);
        descRt.anchorMax = new Vector2(0.72f, 1);
        descRt.offsetMin = Vector2.zero;
        descRt.offsetMax = Vector2.zero;
        ActionDescriptionText = descObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(ActionDescriptionText, false);
        ActionDescriptionText.fontSize = 17;
        ActionDescriptionText.alignment = TextAlignmentOptions.Center;
        ActionDescriptionText.color = new Color(1f, 0.88f, 0.45f, 1f);
        ActionDescriptionText.text = "Replay Loaded";

        // Top Right Controls (Logs Toggle & Exit Replay Button)
        GameObject topButtons = new GameObject("TopButtons", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        topButtons.transform.SetParent(topBar.transform, false);
        var topBtnRt = topButtons.GetComponent<RectTransform>();
        topBtnRt.anchorMin = new Vector2(0.73f, 0);
        topBtnRt.anchorMax = new Vector2(1, 1);
        topBtnRt.offsetMin = new Vector2(0, 15);
        topBtnRt.offsetMax = new Vector2(-20, -15);
        var topHlg = topButtons.GetComponent<HorizontalLayoutGroup>();
        topHlg.spacing = 10;
        topHlg.childControlWidth = true;
        topHlg.childControlHeight = true;
        topHlg.childAlignment = TextAnchor.MiddleRight;

        GameObject logsBtnObj = CreateButton(topButtons.transform, "📜 Logs", Vector2.zero, new Color(0.2f, 0.35f, 0.55f, 1f));
        ToggleLogsButton = logsBtnObj.GetComponent<Button>();
        ToggleLogsButton.onClick.AddListener(ToggleLogsDrawer);

        GameObject exitBtnObj = CreateButton(topButtons.transform, "Exit Replay ✕", Vector2.zero, new Color(0.6f, 0.2f, 0.2f, 1f));
        ExitButton = exitBtnObj.GetComponent<Button>();
        ExitButton.onClick.AddListener(OnClickExitReplay);

        // 2. Bottom Control Bar (Timeline Slider + Playback controls)
        GameObject botBar = new GameObject("BottomControlBar", typeof(RectTransform), typeof(Image));
        botBar.transform.SetParent(ReplayUIRoot.transform, false);
        var botRt = botBar.GetComponent<RectTransform>();
        botRt.anchorMin = new Vector2(0.15f, 0);
        botRt.anchorMax = new Vector2(0.85f, 0);
        botRt.pivot = new Vector2(0.5f, 0);
        botRt.sizeDelta = new Vector2(0, 85);
        botRt.anchoredPosition = new Vector2(0, 10);
        botBar.GetComponent<Image>().color = new Color(0.06f, 0.09f, 0.16f, 0.95f);

        // Rounded look or border outline
        var outline = botBar.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.35f, 0.55f, 0.5f);
        outline.effectDistance = new Vector2(2, 2);

        // Timeline Slider (Top half of bottom bar)
        GameObject sliderObj = new GameObject("TimelineSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(botBar.transform, false);
        var sliderRt = sliderObj.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.03f, 0.58f);
        sliderRt.anchorMax = new Vector2(0.82f, 0.92f);
        sliderRt.offsetMin = Vector2.zero;
        sliderRt.offsetMax = Vector2.zero;

        TimelineSlider = sliderObj.GetComponent<Slider>();
        TimelineSlider.minValue = 0;
        TimelineSlider.maxValue = Mathf.Max(1, CurrentReplay.steps.Count - 1);
        TimelineSlider.wholeNumbers = true;
        TimelineSlider.onValueChanged.AddListener(OnTimelineSliderChanged);

        // Slider Background & Fill
        GameObject bgTrack = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgTrack.transform.SetParent(sliderObj.transform, false);
        var bgTrackRt = bgTrack.GetComponent<RectTransform>();
        bgTrackRt.anchorMin = new Vector2(0, 0.25f);
        bgTrackRt.anchorMax = new Vector2(1, 0.75f);
        bgTrackRt.offsetMin = Vector2.zero;
        bgTrackRt.offsetMax = Vector2.zero;
        bgTrack.GetComponent<Image>().color = new Color(0.15f, 0.18f, 0.25f, 1f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        var faRt = fillArea.GetComponent<RectTransform>();
        faRt.anchorMin = new Vector2(0, 0.25f);
        faRt.anchorMax = new Vector2(1, 0.75f);
        faRt.offsetMin = Vector2.zero;
        faRt.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        var fillImg = fill.GetComponent<Image>();
        fillImg.color = new Color(0.2f, 0.65f, 0.95f, 1f);
        TimelineSlider.fillRect = fillRt;

        // Step Counter Text next to Slider
        GameObject stepCntObj = new GameObject("StepCounterText", typeof(RectTransform), typeof(TextMeshProUGUI));
        stepCntObj.transform.SetParent(botBar.transform, false);
        var scRt = stepCntObj.GetComponent<RectTransform>();
        scRt.anchorMin = new Vector2(0.83f, 0.58f);
        scRt.anchorMax = new Vector2(0.98f, 0.92f);
        scRt.offsetMin = Vector2.zero;
        scRt.offsetMax = Vector2.zero;
        StepCounterText = stepCntObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(StepCounterText, false);
        StepCounterText.fontSize = 15;
        StepCounterText.alignment = TextAlignmentOptions.MidlineRight;
        StepCounterText.color = new Color(0.85f, 0.9f, 1f, 1f);
        StepCounterText.text = $"Step: 1 / {CurrentReplay.steps.Count}";

        // Playback Buttons Row (Bottom half of bottom bar)
        GameObject pbRow = new GameObject("PlaybackRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        pbRow.transform.SetParent(botBar.transform, false);
        var pbRt = pbRow.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0.03f, 0.08f);
        pbRt.anchorMax = new Vector2(0.97f, 0.52f);
        pbRt.offsetMin = Vector2.zero;
        pbRt.offsetMax = Vector2.zero;

        var pbHlg = pbRow.GetComponent<HorizontalLayoutGroup>();
        pbHlg.spacing = 10;
        pbHlg.childControlWidth = true;
        pbHlg.childControlHeight = true;

        RestartButton = CreateButton(pbRow.transform, "⏮", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f)).GetComponent<Button>();
        RestartButton.onClick.AddListener(Restart);

        PrevStepButton = CreateButton(pbRow.transform, "◀ Prev", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f)).GetComponent<Button>();
        PrevStepButton.onClick.AddListener(StepBackward);

        var playPauseObj = CreateButton(pbRow.transform, "▶ Play", Vector2.zero, new Color(0.2f, 0.65f, 0.35f, 1f));
        PlayPauseButton = playPauseObj.GetComponent<Button>();
        PlayPauseButtonText = playPauseObj.GetComponentInChildren<TextMeshProUGUI>();
        PlayPauseButton.onClick.AddListener(TogglePlayPause);

        NextStepButton = CreateButton(pbRow.transform, "Next ▶", Vector2.zero, new Color(0.25f, 0.35f, 0.5f, 1f)).GetComponent<Button>();
        NextStepButton.onClick.AddListener(StepForward);

        // Speed Dropdown
        GameObject speedObj = new GameObject("SpeedDropdown", typeof(RectTransform), typeof(Image), typeof(TMP_Dropdown));
        speedObj.transform.SetParent(pbRow.transform, false);
        speedObj.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.32f, 1f);
        SpeedDropdown = speedObj.GetComponent<TMP_Dropdown>();
        SpeedDropdown.ClearOptions();
        SpeedDropdown.AddOptions(new List<string> { "0.5x", "1.0x", "2.0x", "4.0x" });
        SpeedDropdown.value = 1;
        SpeedDropdown.onValueChanged.AddListener(OnSpeedChanged);

        GameObject speedCaption = new GameObject("CaptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        speedCaption.transform.SetParent(speedObj.transform, false);
        var scTmpRt = speedCaption.GetComponent<RectTransform>();
        scTmpRt.anchorMin = Vector2.zero;
        scTmpRt.anchorMax = Vector2.one;
        var spdTmp = speedCaption.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(spdTmp, false);
        spdTmp.fontSize = 14;
        spdTmp.alignment = TextAlignmentOptions.Center;
        spdTmp.text = "1.0x";
        SpeedDropdown.captionText = spdTmp;

        // Toggles
        OpponentHandToggle = CreateToggle(pbRow.transform, "Reveal Hand", ShowOpponentHand, (val) => { ShowOpponentHand = val; ApplyStep(CurrentStepIndex, false); });
        SecurityToggle = CreateToggle(pbRow.transform, "Reveal Security", ShowSecurityCards, (val) => { ShowSecurityCards = val; ApplyStep(CurrentStepIndex, false); });

        // 3. Collapsible Match History Drawer (Right Side)
        CreateLogsDrawer();
    }

    private void CreateLogsDrawer()
    {
        LogsDrawer = new GameObject("LogsDrawer", typeof(RectTransform), typeof(Image));
        LogsDrawer.transform.SetParent(ReplayUIRoot.transform, false);
        var rt = LogsDrawer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.72f, 0.12f);
        rt.anchorMax = new Vector2(0.99f, 0.88f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        LogsDrawer.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.11f, 0.95f);

        var outline = LogsDrawer.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.4f, 0.7f, 0.6f);
        outline.effectDistance = new Vector2(2, 2);

        // Header
        GameObject hdrObj = new GameObject("DrawerHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
        hdrObj.transform.SetParent(LogsDrawer.transform, false);
        var hdrRt = hdrObj.GetComponent<RectTransform>();
        hdrRt.anchorMin = new Vector2(0, 1);
        hdrRt.anchorMax = new Vector2(1, 1);
        hdrRt.pivot = new Vector2(0.5f, 1);
        hdrRt.sizeDelta = new Vector2(-20, 40);
        hdrRt.anchoredPosition = new Vector2(0, -10);
        var hdrTmp = hdrObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(hdrTmp, true);
        hdrTmp.fontSize = 17;
        hdrTmp.alignment = TextAlignmentOptions.Center;
        hdrTmp.color = new Color(0.7f, 0.85f, 1f, 1f);
        hdrTmp.text = "MATCH ACTION LOGS (CLICK TO JUMP)";

        // Scroll View
        GameObject scrollObj = new GameObject("LogsScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollObj.transform.SetParent(LogsDrawer.transform, false);
        var scrRt = scrollObj.GetComponent<RectTransform>();
        scrRt.anchorMin = Vector2.zero;
        scrRt.anchorMax = Vector2.one;
        scrRt.offsetMin = new Vector2(10, 10);
        scrRt.offsetMax = new Vector2(-10, -50);

        GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentObj.transform.SetParent(scrollObj.transform, false);
        var cntRt = contentObj.GetComponent<RectTransform>();
        cntRt.anchorMin = new Vector2(0, 1);
        cntRt.anchorMax = new Vector2(1, 1);
        cntRt.pivot = new Vector2(0.5f, 1);

        var vlg = contentObj.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 3;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = contentObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        LogsScrollRect = scrollObj.GetComponent<ScrollRect>();
        LogsScrollRect.content = cntRt;
        LogsScrollRect.horizontal = false;
        LogsScrollRect.vertical = true;
        LogsContent = cntRt;

        LogsDrawer.SetActive(false); // Collapsed by default
    }

    private void PopulateLogsDrawer()
    {
        if (LogsContent == null || CurrentReplay == null) return;

        foreach (Transform child in LogsContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < CurrentReplay.steps.Count; i++)
        {
            var step = CurrentReplay.steps[i];
            int stepNum = i;

            GameObject item = new GameObject($"LogStep_{stepNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(LogsContent, false);

            var le = item.GetComponent<LayoutElement>();
            le.minHeight = 28;
            le.preferredHeight = 28;

            var img = item.GetComponent<Image>();
            img.color = stepNum % 2 == 0 ? new Color(0.1f, 0.12f, 0.18f, 0.85f) : new Color(0.14f, 0.16f, 0.24f, 0.85f);

            var btn = item.GetComponent<Button>();
            btn.onClick.AddListener(() => JumpToStep(stepNum));

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(item.transform, false);
            var rt = textObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(8, 2);
            rt.offsetMax = new Vector2(-8, -2);

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            ReplaySceneSetup.ApplyFont(tmp, false);
            tmp.fontSize = 13;
            tmp.color = Color.white;
            tmp.text = $"<color=#88CCFF>[T{step.turnNumber}]</color> {step.description}";
        }
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
        ReplaySceneSetup.ApplyFont(tmp, true);
        tmp.text = label;
        tmp.fontSize = 15;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btnObj;
    }

    private Toggle CreateToggle(Transform parent, string label, bool defaultVal, Action<bool> onToggle)
    {
        GameObject togObj = new GameObject($"Toggle_{label.Replace(" ", "")}", typeof(RectTransform), typeof(Toggle));
        togObj.transform.SetParent(parent, false);

        var tog = togObj.GetComponent<Toggle>();

        GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(togObj.transform, false);
        var bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0.5f);
        bgRt.anchorMax = new Vector2(0, 0.5f);
        bgRt.sizeDelta = new Vector2(24, 24);
        bgRt.anchoredPosition = new Vector2(15, 0);
        bgObj.GetComponent<Image>().color = new Color(0.2f, 0.25f, 0.35f, 1f);

        GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkObj.transform.SetParent(bgObj.transform, false);
        var checkRt = checkObj.GetComponent<RectTransform>();
        checkRt.anchorMin = new Vector2(0.5f, 0.5f);
        checkRt.anchorMax = new Vector2(0.5f, 0.5f);
        checkRt.sizeDelta = new Vector2(18, 18);
        checkRt.anchoredPosition = Vector2.zero;
        checkObj.GetComponent<Image>().color = new Color(0.3f, 0.8f, 0.4f, 1f);

        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelObj.transform.SetParent(togObj.transform, false);
        var labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 0);
        labelRt.anchorMax = new Vector2(1, 1);
        labelRt.offsetMin = new Vector2(35, 0);
        labelRt.offsetMax = Vector2.zero;
        var labelTmp = labelObj.GetComponent<TextMeshProUGUI>();
        ReplaySceneSetup.ApplyFont(labelTmp, false);
        labelTmp.text = label;
        labelTmp.fontSize = 14;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        labelTmp.color = Color.white;

        tog.targetGraphic = bgObj.GetComponent<Image>();
        tog.graphic = checkObj.GetComponent<Image>();
        tog.isOn = defaultVal;
        tog.onValueChanged.AddListener((b) => onToggle?.Invoke(b));

        return tog;
    }

    #endregion

    #region Board State Application

    public void ApplyStep(int stepIndex, bool playAudio = true)
    {
        if (CurrentReplay == null || CurrentReplay.steps.Count == 0) return;

        CurrentStepIndex = Mathf.Clamp(stepIndex, 0, CurrentReplay.steps.Count - 1);
        ReplayStep step = CurrentReplay.steps[CurrentStepIndex];

        // Update Slider without retriggering event loop
        if (TimelineSlider != null && !_isScrubbing)
        {
            TimelineSlider.SetValueWithoutNotify(CurrentStepIndex);
        }

        if (StepCounterText != null)
        {
            StepCounterText.text = $"Step: {CurrentStepIndex + 1} / {CurrentReplay.steps.Count}";
        }

        if (ActionDescriptionText != null)
        {
            ActionDescriptionText.text = $"<color=#88CCFF>[Turn {step.turnNumber} - {step.activePlayerName}]</color> {step.description}";
        }

        HighlightLogDrawerItem(CurrentStepIndex);

        // 1. Update Memory Gauge
        UpdateMemoryGauge(step.memory);

        // 2. Clear Visual Cards previously spawned
        ClearVisuals();

        // 3. Render Field Permanents for both players
        RenderFieldPermanents(step);

        // 4. Render Hand Cards for both players
        RenderHandCards(step);

        // 5. Render Security Stacks
        RenderSecurity(step);

        // 6. Play Audio Effects
        if (playAudio && ContinuousController.instance != null)
        {
            PlayActionSE(step);
        }
    }

    private void UpdateMemoryGauge(int targetMemory)
    {
        if (GManager.instance == null || GManager.instance.memoryObject == null) return;

        if (GManager.instance.turnStateMachine != null && GManager.instance.turnStateMachine.gameContext != null)
        {
            GManager.instance.turnStateMachine.gameContext.Memory = targetMemory;
        }

        // Highlight the memory gauge tab
        foreach (var tab in GManager.instance.memoryObject.memoryTabs)
        {
            if (tab != null && tab.Light != null)
            {
                tab.Light.SetActive(tab.Memory == targetMemory);
            }
        }
    }

    private void ClearVisuals()
    {
        foreach (var obj in _spawnedVisuals)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedVisuals.Clear();

        if (GManager.instance != null)
        {
            if (GManager.instance.You != null)
            {
                if (GManager.instance.You.HandTransform != null)
                {
                    foreach (Transform child in GManager.instance.You.HandTransform)
                    {
                        Destroy(child.gameObject);
                    }
                }
                if (GManager.instance.You.PermanentTransform != null)
                {
                    foreach (Transform child in GManager.instance.You.PermanentTransform)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }

            if (GManager.instance.Opponent != null)
            {
                if (GManager.instance.Opponent.HandTransform != null)
                {
                    foreach (Transform child in GManager.instance.Opponent.HandTransform)
                    {
                        Destroy(child.gameObject);
                    }
                }
                if (GManager.instance.Opponent.PermanentTransform != null)
                {
                    foreach (Transform child in GManager.instance.Opponent.PermanentTransform)
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
    }

    private void RenderFieldPermanents(ReplayStep step)
    {
        if (GManager.instance == null || GManager.instance.fieldCardPrefab == null) return;

        Player p1 = GManager.instance.You;
        Player p2 = GManager.instance.Opponent;

        // Player 1 Field
        if (p1 != null && step.p1Permanents != null)
        {
            foreach (var permData in step.p1Permanents)
            {
                SpawnFieldPermanent(p1, permData);
            }

            if (step.p1Breeding != null)
            {
                SpawnBreedingPermanent(p1, step.p1Breeding);
            }
        }

        // Player 2 Field
        if (p2 != null && step.p2Permanents != null)
        {
            foreach (var permData in step.p2Permanents)
            {
                SpawnFieldPermanent(p2, permData);
            }

            if (step.p2Breeding != null)
            {
                SpawnBreedingPermanent(p2, step.p2Breeding);
            }
        }
    }

    private void SpawnFieldPermanent(Player player, ReplayPermanentData permData)
    {
        if (player.fieldCardFrames == null || permData.frameId < 0 || permData.frameId >= player.fieldCardFrames.Count)
            return;

        var frame = player.fieldCardFrames[permData.frameId];
        if (frame == null) return;

        List<CardSource> cardSources = new List<CardSource>();
        if (permData.cardIds != null && permData.cardIds.Count > 0)
        {
            foreach (var cid in permData.cardIds)
            {
                var cs = GetOrCreateCardSource(cid, player);
                if (cs != null) cardSources.Add(cs);
            }
        }

        if (cardSources.Count == 0) return;

        // Create Permanent container
        Permanent perm = new Permanent(cardSources);
        perm.IsSuspended = permData.isSuspended;

        // Instantiate visual FieldPermanentCard
        FieldPermanentCard fpc = Instantiate(GManager.instance.fieldCardPrefab, player.PermanentTransform);
        _spawnedVisuals.Add(fpc.gameObject);

        fpc.SetPermanentData(perm, true);
        fpc.StartScale = fpc.transform.localScale;
        perm.ShowingPermanentCard = fpc;
        fpc.transform.localPosition = frame.GetLocalCanvasPosition();
        fpc.gameObject.SetActive(true);

        // Evolution stack badge
        if (cardSources.Count > 1 && fpc.EvoRootCountText != null)
        {
            fpc.EvoRootCountText.transform.parent.gameObject.SetActive(true);
            fpc.EvoRootCountText.text = (cardSources.Count - 1).ToString();
        }

        // DP Text
        if (fpc.DPText != null)
        {
            fpc.DPText.transform.parent.gameObject.SetActive(true);
            fpc.DPText.text = permData.dp > 0 ? permData.dp.ToString() : perm.DP.ToString();
        }
    }

    private void SpawnBreedingPermanent(Player player, ReplayPermanentData permData)
    {
        if (player.fieldCardFrames == null) return;
        var breedingFrame = player.fieldCardFrames.Find(f => f.isBreedingAreaFrame());
        if (breedingFrame == null) return;

        List<CardSource> cardSources = new List<CardSource>();
        if (permData.cardIds != null && permData.cardIds.Count > 0)
        {
            foreach (var cid in permData.cardIds)
            {
                var cs = GetOrCreateCardSource(cid, player);
                if (cs != null) cardSources.Add(cs);
            }
        }

        if (cardSources.Count == 0) return;

        Permanent perm = new Permanent(cardSources);
        perm.IsSuspended = permData.isSuspended;

        FieldPermanentCard fpc = Instantiate(GManager.instance.fieldCardPrefab, player.PermanentTransform);
        _spawnedVisuals.Add(fpc.gameObject);

        fpc.SetPermanentData(perm, true);
        fpc.transform.localPosition = breedingFrame.GetLocalCanvasPosition();
        fpc.gameObject.SetActive(true);
    }

    private void RenderHandCards(ReplayStep step)
    {
        if (GManager.instance == null || GManager.instance.handCardPrefab == null) return;

        // Player 1 Hand
        if (GManager.instance.You != null && GManager.instance.You.HandTransform != null)
        {
            foreach (var cardId in step.p1Hand)
            {
                CardSource cs = GetOrCreateCardSource(cardId, GManager.instance.You);
                if (cs != null)
                {
                    HandCard hc = Instantiate(GManager.instance.handCardPrefab, GManager.instance.You.HandTransform);
                    hc.cardSource = cs;
                    hc.SetUpHandCardImage();
                    hc.gameObject.SetActive(true);
                }
            }
        }

        // Player 2 Hand
        if (GManager.instance.Opponent != null && GManager.instance.Opponent.HandTransform != null)
        {
            foreach (var cardId in step.p2Hand)
            {
                CardSource cs = GetOrCreateCardSource(cardId, GManager.instance.Opponent);
                if (cs != null)
                {
                    HandCard hc = Instantiate(GManager.instance.handCardPrefab, GManager.instance.Opponent.HandTransform);
                    hc.cardSource = cs;
                    if (ShowOpponentHand)
                    {
                        hc.SetUpHandCardImage();
                    }
                    else
                    {
                        if (ContinuousController.instance != null && hc.CardImage != null)
                        {
                            hc.CardImage.sprite = ContinuousController.instance.ReverseCard;
                        }
                    }
                    hc.gameObject.SetActive(true);
                }
            }
        }
    }

    private void RenderSecurity(ReplayStep step)
    {
        if (GManager.instance == null) return;

        if (GManager.instance.You != null && GManager.instance.You.securityObject != null)
        {
            var sec = GManager.instance.You.securityObject;
            if (sec.SecurityText != null)
            {
                sec.SecurityText.text = step.p1Security.Count.ToString();
            }
            if (sec.LifeCards != null)
            {
                for (int i = 0; i < sec.LifeCards.Count; i++)
                {
                    if (sec.LifeCards[i] != null)
                    {
                        if (i < step.p1Security.Count)
                        {
                            sec.LifeCards[i].gameObject.SetActive(true);
                            if (ContinuousController.instance != null && ContinuousController.instance.ReverseCard != null)
                                sec.LifeCards[i].sprite = ContinuousController.instance.ReverseCard;
                        }
                        else
                        {
                            sec.LifeCards[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        if (GManager.instance.Opponent != null && GManager.instance.Opponent.securityObject != null)
        {
            var sec = GManager.instance.Opponent.securityObject;
            if (sec.SecurityText != null)
            {
                sec.SecurityText.text = step.p2Security.Count.ToString();
            }
            if (sec.LifeCards != null)
            {
                for (int i = 0; i < sec.LifeCards.Count; i++)
                {
                    if (sec.LifeCards[i] != null)
                    {
                        if (i < step.p2Security.Count)
                        {
                            sec.LifeCards[i].gameObject.SetActive(true);
                            if (ContinuousController.instance != null && ContinuousController.instance.ReverseCard != null)
                                sec.LifeCards[i].sprite = ContinuousController.instance.ReverseCard;
                        }
                        else
                        {
                            sec.LifeCards[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    private CardSource GetOrCreateCardSource(string cardId, Player owner)
    {
        if (string.IsNullOrEmpty(cardId) || owner == null) return null;

        string cacheKey = $"{cardId}_{owner.PlayerID}";
        if (_cardSourceCache.TryGetValue(cacheKey, out CardSource existing) && existing != null)
        {
            return existing;
        }

        if (ContinuousController.instance == null || ContinuousController.instance.CardList == null)
            return null;

        CEntity_Base entity = Array.Find(ContinuousController.instance.CardList, c => c != null && c.CardID == cardId);
        if (entity == null) return null;

        if (GManager.instance == null || GManager.instance.CardPrefab == null) return null;

        CardSource newCs = Instantiate(GManager.instance.CardPrefab, owner.CardSorcesParent);
        newCs.SetBaseData(entity, owner);
        if (newCs.cEntity_EffectController != null && !string.IsNullOrEmpty(entity.CardEffectClassName))
        {
            newCs.cEntity_EffectController.AddCardEffect(entity.CardID, entity.CardEffectClassName);
        }
        newCs.SetUpCardIndex(_cardSourceCache.Count);
        newCs.gameObject.SetActive(false);

        _cardSourceCache[cacheKey] = newCs;
        return newCs;
    }

    private void PlayActionSE(ReplayStep step)
    {
        if (GManager.instance == null || ContinuousController.instance == null) return;

        switch (step.actionType)
        {
            case "PlayCard":
                if (GManager.instance.PlayPokemonSE != null)
                    ContinuousController.instance.PlaySE(GManager.instance.PlayPokemonSE);
                break;
            case "Digivolve":
                if (GManager.instance.UseSkillSE != null)
                    ContinuousController.instance.PlaySE(GManager.instance.UseSkillSE);
                break;
            case "Attack":
                if (GManager.instance.DamageSE != null)
                    ContinuousController.instance.PlaySE(GManager.instance.DamageSE);
                break;
            case "Draw":
                if (GManager.instance.DrawSE != null)
                    ContinuousController.instance.PlaySE(GManager.instance.DrawSE);
                break;
            case "GameEnd":
                if (GManager.instance.WinSE != null)
                    ContinuousController.instance.PlaySE(GManager.instance.WinSE);
                break;
        }
    }

    #endregion

    #region Playback Controls

    public void TogglePlayPause()
    {
        if (IsPlaying) Pause(); else Play();
    }

    public void Play()
    {
        if (CurrentReplay == null || CurrentReplay.steps.Count == 0) return;

        IsPlaying = true;
        if (PlayPauseButtonText != null) PlayPauseButtonText.text = "⏸ Pause";

        if (_playCoroutine != null) StopCoroutine(_playCoroutine);
        _playCoroutine = StartCoroutine(PlayCoroutine());
    }

    public void Pause()
    {
        IsPlaying = false;
        if (PlayPauseButtonText != null) PlayPauseButtonText.text = "▶ Play";

        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
        }
    }

    private IEnumerator PlayCoroutine()
    {
        while (IsPlaying && CurrentStepIndex < CurrentReplay.steps.Count - 1)
        {
            float waitTime = 1.3f / Mathf.Max(0.25f, PlaybackSpeed);
            yield return new WaitForSeconds(waitTime);

            if (!IsPlaying) yield break;

            CurrentStepIndex++;
            ApplyStep(CurrentStepIndex, true);
        }

        Pause();
    }

    public void StepForward()
    {
        Pause();
        if (CurrentReplay == null) return;

        if (CurrentStepIndex < CurrentReplay.steps.Count - 1)
        {
            CurrentStepIndex++;
            ApplyStep(CurrentStepIndex, true);
        }
    }

    public void StepBackward()
    {
        Pause();
        if (CurrentReplay == null) return;

        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            ApplyStep(CurrentStepIndex, false);
        }
    }

    public void Restart()
    {
        Pause();
        CurrentStepIndex = 0;
        ApplyStep(0, false);
    }

    public void JumpToStep(int stepIndex)
    {
        Pause();
        CurrentStepIndex = stepIndex;
        ApplyStep(stepIndex, false);
    }

    public void OnTimelineSliderChanged(float val)
    {
        int targetStep = Mathf.RoundToInt(val);
        if (targetStep != CurrentStepIndex)
        {
            _isScrubbing = true;
            JumpToStep(targetStep);
            _isScrubbing = false;
        }
    }

    public void OnSpeedChanged(int index)
    {
        switch (index)
        {
            case 0: PlaybackSpeed = 0.5f; break;
            case 1: PlaybackSpeed = 1.0f; break;
            case 2: PlaybackSpeed = 2.0f; break;
            case 3: PlaybackSpeed = 4.0f; break;
            default: PlaybackSpeed = 1.0f; break;
        }
    }

    public void ToggleLogsDrawer()
    {
        if (LogsDrawer != null)
        {
            LogsDrawer.SetActive(!LogsDrawer.activeSelf);
        }
    }

    private void HighlightLogDrawerItem(int stepIndex)
    {
        if (LogsContent == null) return;

        for (int i = 0; i < LogsContent.childCount; i++)
        {
            var child = LogsContent.GetChild(i);
            var img = child.GetComponent<Image>();
            if (img != null)
            {
                if (i == stepIndex)
                {
                    img.color = new Color(0.2f, 0.5f, 0.85f, 0.95f);
                }
                else
                {
                    img.color = i % 2 == 0 ? new Color(0.1f, 0.12f, 0.18f, 0.85f) : new Color(0.14f, 0.16f, 0.24f, 0.85f);
                }
            }
        }

        if (LogsScrollRect != null && CurrentReplay.steps.Count > 0)
        {
            float norm = 1f - ((float)stepIndex / Mathf.Max(1, CurrentReplay.steps.Count - 1));
            LogsScrollRect.verticalNormalizedPosition = Mathf.Clamp01(norm);
        }
    }

    public void OnClickExitReplay()
    {
        Pause();
        if (ContinuousController.instance != null)
        {
            ContinuousController.instance.EndReplayBattle();
        }
    }

    #endregion
}
