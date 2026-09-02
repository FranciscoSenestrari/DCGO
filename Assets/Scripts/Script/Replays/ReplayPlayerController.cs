using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReplayPlayerController : MonoBehaviour
{
    [Header("Replay Data")]
    public ReplayData CurrentReplay;
    public int CurrentStepIndex = 0;
    public bool IsPlaying = false;
    public float PlaybackSpeed = 1f;

    [Header("UI - Header Info")]
    public TMP_Text MatchTitleText;
    public TMP_Text TurnText;
    public TMP_Text ActivePlayerText;
    public TMP_Text MemoryText;
    public TMP_Text StepCounterText;
    public TMP_Text ActionDescriptionText;

    [Header("UI - Player 1 Details")]
    public TMP_Text P1NameText;
    public TMP_Text P1HandText;
    public TMP_Text P1SecurityText;

    [Header("UI - Player 2 Details")]
    public TMP_Text P2NameText;
    public TMP_Text P2HandText;
    public TMP_Text P2SecurityText;

    [Header("UI - Visibility Toggles")]
    public Toggle ShowHandToggle;
    public Toggle ShowSecurityToggle;

    [Header("UI - Playback Controls")]
    public Button PlayPauseButton;
    public TMP_Text PlayPauseButtonText;
    public Button StepPrevButton;
    public Button StepNextButton;
    public Button RestartButton;
    public TMP_Dropdown SpeedDropdown;

    [Header("UI - Log History")]
    public Transform LogContent;
    public ScrollRect LogScrollRect;

    private Coroutine _playCoroutine;

    private void Start()
    {
        if (ShowHandToggle != null)
        {
            ShowHandToggle.onValueChanged.AddListener(OnToggleShowHand);
        }
        if (ShowSecurityToggle != null)
        {
            ShowSecurityToggle.onValueChanged.AddListener(OnToggleShowSecurity);
        }
        if (SpeedDropdown != null)
        {
            SpeedDropdown.onValueChanged.AddListener(OnSpeedChanged);
        }
    }

    public void LoadReplay(ReplayData replay)
    {
        CurrentReplay = replay;
        CurrentStepIndex = 0;
        IsPlaying = false;

        if (MatchTitleText != null)
        {
            MatchTitleText.text = $"{replay.player1Name} vs {replay.player2Name}";
        }
        if (P1NameText != null) P1NameText.text = replay.player1Name;
        if (P2NameText != null) P2NameText.text = replay.player2Name;

        PopulateLogHistory();
        UpdateUIForCurrentStep();
    }

    private void PopulateLogHistory()
    {
        if (LogContent == null || CurrentReplay == null) return;

        foreach (Transform child in LogContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < CurrentReplay.steps.Count; i++)
        {
            var step = CurrentReplay.steps[i];
            int stepNum = i;

            GameObject item = new GameObject($"LogStep_{stepNum}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            item.transform.SetParent(LogContent, false);

            var le = item.GetComponent<LayoutElement>();
            le.minHeight = 28;
            le.preferredHeight = 28;

            var img = item.GetComponent<Image>();
            img.color = stepNum % 2 == 0 ? new Color(0.1f, 0.12f, 0.16f, 0.7f) : new Color(0.13f, 0.15f, 0.2f, 0.7f);

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
            tmp.fontSize = 13;
            tmp.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            tmp.text = $"<color=#88CCFF>[T{step.turnNumber}]</color> {step.description}";
        }
    }

    public void UpdateUIForCurrentStep()
    {
        if (CurrentReplay == null || CurrentReplay.steps.Count == 0) return;

        CurrentStepIndex = Mathf.Clamp(CurrentStepIndex, 0, CurrentReplay.steps.Count - 1);
        ReplayStep step = CurrentReplay.steps[CurrentStepIndex];

        if (TurnText != null) TurnText.text = $"Turn: {step.turnNumber}";
        if (ActivePlayerText != null) ActivePlayerText.text = $"Active: {step.activePlayerName}";
        if (MemoryText != null) MemoryText.text = $"Memory: {step.memory}";
        if (StepCounterText != null) StepCounterText.text = $"Step: {CurrentStepIndex + 1} / {CurrentReplay.steps.Count}";
        if (ActionDescriptionText != null) ActionDescriptionText.text = step.description;

        UpdateHandAndSecurityDisplay(step);
        HighlightLogItem(CurrentStepIndex);
    }

    private void UpdateHandAndSecurityDisplay(ReplayStep step)
    {
        bool showHand = ShowHandToggle != null && ShowHandToggle.isOn;
        bool showSecurity = ShowSecurityToggle != null && ShowSecurityToggle.isOn;

        // Player 1 Hand
        if (P1HandText != null)
        {
            if (showHand)
            {
                P1HandText.text = $"<b>Hand ({step.p1Hand.Count}):</b>\n" + (step.p1Hand.Count > 0 ? string.Join(", ", step.p1Hand) : "Empty");
            }
            else
            {
                P1HandText.text = $"<b>Hand:</b> {step.p1Hand.Count} cards <color=#888888>(Hidden)</color>";
            }
        }

        // Player 1 Security
        if (P1SecurityText != null)
        {
            if (showSecurity)
            {
                P1SecurityText.text = $"<b>Security ({step.p1Security.Count}):</b>\n" + (step.p1Security.Count > 0 ? string.Join(", ", step.p1Security) : "Empty");
            }
            else
            {
                P1SecurityText.text = $"<b>Security:</b> {step.p1Security.Count} cards <color=#888888>(Hidden)</color>";
            }
        }

        // Player 2 Hand
        if (P2HandText != null)
        {
            if (showHand)
            {
                P2HandText.text = $"<b>Hand ({step.p2Hand.Count}):</b>\n" + (step.p2Hand.Count > 0 ? string.Join(", ", step.p2Hand) : "Empty");
            }
            else
            {
                P2HandText.text = $"<b>Hand:</b> {step.p2Hand.Count} cards <color=#888888>(Hidden)</color>";
            }
        }

        // Player 2 Security
        if (P2SecurityText != null)
        {
            if (showSecurity)
            {
                P2SecurityText.text = $"<b>Security ({step.p2Security.Count}):</b>\n" + (step.p2Security.Count > 0 ? string.Join(", ", step.p2Security) : "Empty");
            }
            else
            {
                P2SecurityText.text = $"<b>Security:</b> {step.p2Security.Count} cards <color=#888888>(Hidden)</color>";
            }
        }
    }

    private void HighlightLogItem(int stepIndex)
    {
        if (LogContent == null) return;

        for (int i = 0; i < LogContent.childCount; i++)
        {
            var child = LogContent.GetChild(i);
            var img = child.GetComponent<Image>();
            if (img != null)
            {
                if (i == stepIndex)
                {
                    img.color = new Color(0.2f, 0.45f, 0.75f, 0.9f);
                }
                else
                {
                    img.color = i % 2 == 0 ? new Color(0.1f, 0.12f, 0.16f, 0.7f) : new Color(0.13f, 0.15f, 0.2f, 0.7f);
                }
            }
        }

        // Auto scroll
        if (LogScrollRect != null && CurrentReplay.steps.Count > 0)
        {
            float norm = 1f - ((float)stepIndex / Mathf.Max(1, CurrentReplay.steps.Count - 1));
            LogScrollRect.verticalNormalizedPosition = Mathf.Clamp01(norm);
        }
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    public void Play()
    {
        if (CurrentReplay == null || CurrentReplay.steps.Count == 0) return;

        IsPlaying = true;
        if (PlayPauseButtonText != null) PlayPauseButtonText.text = "Pause";

        if (_playCoroutine != null) StopCoroutine(_playCoroutine);
        _playCoroutine = StartCoroutine(PlayCoroutine());
    }

    public void Pause()
    {
        IsPlaying = false;
        if (PlayPauseButtonText != null) PlayPauseButtonText.text = "Play";

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
            float waitTime = 1.2f / Mathf.Max(0.25f, PlaybackSpeed);
            yield return new WaitForSeconds(waitTime);

            if (!IsPlaying) yield break;

            CurrentStepIndex++;
            UpdateUIForCurrentStep();
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
            UpdateUIForCurrentStep();
        }
    }

    public void StepBackward()
    {
        Pause();
        if (CurrentReplay == null) return;

        if (CurrentStepIndex > 0)
        {
            CurrentStepIndex--;
            UpdateUIForCurrentStep();
        }
    }

    public void Restart()
    {
        Pause();
        CurrentStepIndex = 0;
        UpdateUIForCurrentStep();
    }

    public void JumpToStep(int stepIndex)
    {
        Pause();
        CurrentStepIndex = stepIndex;
        UpdateUIForCurrentStep();
    }

    public void OnToggleShowHand(bool show)
    {
        if (CurrentReplay != null && CurrentReplay.steps.Count > CurrentStepIndex)
        {
            UpdateHandAndSecurityDisplay(CurrentReplay.steps[CurrentStepIndex]);
        }
    }

    public void OnToggleShowSecurity(bool show)
    {
        if (CurrentReplay != null && CurrentReplay.steps.Count > CurrentStepIndex)
        {
            UpdateHandAndSecurityDisplay(CurrentReplay.steps[CurrentStepIndex]);
        }
    }

    public void OnSpeedChanged(int index)
    {
        switch (index)
        {
            case 0: PlaybackSpeed = 0.5f; break;
            case 1: PlaybackSpeed = 1f; break;
            case 2: PlaybackSpeed = 2f; break;
            case 3: PlaybackSpeed = 4f; break;
            default: PlaybackSpeed = 1f; break;
        }
    }
}
