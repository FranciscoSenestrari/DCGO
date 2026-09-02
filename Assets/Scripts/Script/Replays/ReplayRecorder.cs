using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ReplayRecorder : MonoBehaviour
{
    public static ReplayRecorder Instance { get; private set; }

    [Header("Current Replay")]
    public ReplayData CurrentReplay;
    public bool IsRecording = false;

    private int _stepCounter = 0;
    private bool _hasSaved = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        PlayLog.OnAddLog += HandlePlayLogAdded;
    }

    private void OnDisable()
    {
        PlayLog.OnAddLog -= HandlePlayLogAdded;
    }

    public void StartRecording(GameContext context)
    {
        _stepCounter = 0;
        _hasSaved = false;
        IsRecording = true;

        CurrentReplay = new ReplayData
        {
            replayId = Guid.NewGuid().ToString(),
            matchDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            gameMode = GManager.instance != null && GManager.instance.IsAI ? "Bot Match" : "PvP Match",
            player1Name = context.PlayerFromID(0)?.PlayerName ?? "Player 1",
            player2Name = context.PlayerFromID(1)?.PlayerName ?? "Player 2",
            firstPlayerIndex = 0,
            steps = new List<ReplayStep>()
        };

        // Capture Player 1 Decks
        Player p1 = context.PlayerFromID(0);
        if (p1 != null)
        {
            if (p1.LibraryCards != null)
            {
                foreach (var card in p1.LibraryCards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.CardID))
                        CurrentReplay.player1MainDeckCardIDs.Add(card.CardID);
                }
            }
            if (p1.DigitamaLibraryCards != null)
            {
                foreach (var card in p1.DigitamaLibraryCards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.CardID))
                        CurrentReplay.player1EggDeckCardIDs.Add(card.CardID);
                }
            }
        }

        // Capture Player 2 Decks
        Player p2 = context.PlayerFromID(1);
        if (p2 != null)
        {
            if (p2.LibraryCards != null)
            {
                foreach (var card in p2.LibraryCards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.CardID))
                        CurrentReplay.player2MainDeckCardIDs.Add(card.CardID);
                }
            }
            if (p2.DigitamaLibraryCards != null)
            {
                foreach (var card in p2.DigitamaLibraryCards)
                {
                    if (card != null && !string.IsNullOrEmpty(card.CardID))
                        CurrentReplay.player2EggDeckCardIDs.Add(card.CardID);
                }
            }
        }

        RecordStep("GameStart", "Match started between " + CurrentReplay.player1Name + " and " + CurrentReplay.player2Name);
    }

    public void RecordStep(string actionType, string description, string cardId = "", string cardName = "", string source = "", string target = "")
    {
        if (!IsRecording || CurrentReplay == null) return;

        int currentTurn = 1;
        int activePlayerId = 0;
        string activePlayerName = "";
        string phase = "";
        int memory = 0;

        if (GManager.instance != null && GManager.instance.turnStateMachine != null)
        {
            var tsm = GManager.instance.turnStateMachine;
            currentTurn = tsm.TurnCount;
            if (tsm.gameContext != null)
            {
                if (tsm.gameContext.TurnPlayer != null)
                {
                    activePlayerId = tsm.gameContext.TurnPlayer.PlayerID;
                    activePlayerName = tsm.gameContext.TurnPlayer.PlayerName;
                }
                memory = tsm.gameContext.Memory;
            }
        }

        var step = new ReplayStep
        {
            stepIndex = _stepCounter++,
            turnNumber = currentTurn,
            activePlayerId = activePlayerId,
            activePlayerName = activePlayerName,
            phaseName = phase,
            actionType = actionType,
            description = CleanRichText(description),
            memory = memory,
            cardId = cardId,
            cardName = cardName,
            sourceLocation = source,
            targetLocation = target
        };

        // Capture Full Board Snapshots for both players
        CaptureSnapshots(step);

        CurrentReplay.steps.Add(step);
    }

    private void CaptureSnapshots(ReplayStep step)
    {
        if (GManager.instance == null || GManager.instance.turnStateMachine == null || GManager.instance.turnStateMachine.gameContext == null)
            return;

        var context = GManager.instance.turnStateMachine.gameContext;
        Player p1 = context.PlayerFromID(0);
        Player p2 = context.PlayerFromID(1);

        if (p1 != null)
        {
            CapturePlayerSnapshot(p1, step.p1Hand, step.p1Security, step.p1Trash, step.p1Permanents, out step.p1Breeding, out step.p1MainDeckCount, out step.p1EggDeckCount);
        }

        if (p2 != null)
        {
            CapturePlayerSnapshot(p2, step.p2Hand, step.p2Security, step.p2Trash, step.p2Permanents, out step.p2Breeding, out step.p2MainDeckCount, out step.p2EggDeckCount);
        }
    }

    private void CapturePlayerSnapshot(Player player, List<string> hand, List<string> security, List<string> trash, List<ReplayPermanentData> permanents, out ReplayPermanentData breeding, out int mainDeckCount, out int eggDeckCount)
    {
        breeding = null;
        mainDeckCount = player.LibraryCards != null ? player.LibraryCards.Count : 0;
        eggDeckCount = player.DigitamaLibraryCards != null ? player.DigitamaLibraryCards.Count : 0;

        // Hand
        if (player.HandCards != null)
        {
            foreach (var h in player.HandCards)
            {
                if (h != null && !string.IsNullOrEmpty(h.CardID))
                    hand.Add(h.CardID);
            }
        }

        // Security
        if (player.SecurityCards != null)
        {
            foreach (var s in player.SecurityCards)
            {
                if (s != null && !string.IsNullOrEmpty(s.CardID))
                    security.Add(s.CardID);
            }
        }

        // Trash
        if (player.TrashCards != null)
        {
            foreach (var t in player.TrashCards)
            {
                if (t != null && !string.IsNullOrEmpty(t.CardID))
                    trash.Add(t.CardID);
            }
        }

        // Field Frames
        if (player.fieldCardFrames != null)
        {
            foreach (var frame in player.fieldCardFrames)
            {
                if (frame == null) continue;
                Permanent perm = frame.GetFramePermanent();
                if (perm != null && perm.TopCard != null)
                {
                    var pData = new ReplayPermanentData
                    {
                        frameId = frame.FrameID,
                        isSuspended = perm.IsSuspended,
                        dp = perm.DP,
                        level = perm.Level,
                        isDigiEgg = perm.TopCard != null && perm.TopCard.IsDigiEgg,
                        cardIds = new List<string>()
                    };

                    if (perm.cardSources != null)
                    {
                        foreach (var cs in perm.cardSources)
                        {
                            if (cs != null && !string.IsNullOrEmpty(cs.CardID))
                                pData.cardIds.Add(cs.CardID);
                        }
                    }

                    if (frame.isBreedingAreaFrame())
                    {
                        breeding = pData;
                    }
                    else
                    {
                        permanents.Add(pData);
                    }
                }
            }
        }
    }

    private void HandlePlayLogAdded(string logText)
    {
        if (IsRecording && CurrentReplay != null)
        {
            string clean = CleanRichText(logText);
            string actionType = "Log";
            if (clean.Contains("Play Card") || clean.Contains("Play Option")) actionType = "PlayCard";
            else if (clean.Contains("Evolution") || clean.Contains("Jogress") || clean.Contains("Burst")) actionType = "Digivolve";
            else if (clean.Contains("Attack") || clean.Contains("Security Check")) actionType = "Attack";
            else if (clean.Contains("Draw")) actionType = "Draw";
            else if (clean.Contains("Phase")) actionType = "Phase";
            else if (clean.Contains("Hatch")) actionType = "Hatch";
            else if (clean.Contains("Winner") || clean.Contains("Won")) actionType = "GameEnd";

            RecordStep(actionType, logText);
        }
    }

    public void EndMatch(string winnerName)
    {
        if (!IsRecording || CurrentReplay == null || _hasSaved) return;

        CurrentReplay.winnerPlayerName = winnerName;
        if (GManager.instance != null && GManager.instance.turnStateMachine != null)
        {
            CurrentReplay.totalTurns = GManager.instance.turnStateMachine.TurnCount;
        }

        RecordStep("GameEnd", $"Match finished. Winner: {winnerName}");
        SaveReplayToFile();

        IsRecording = false;
        _hasSaved = true;
    }

    public string SaveReplayToFile()
    {
        if (CurrentReplay == null) return null;

        try
        {
            string cleanP1 = SanitizeFileName(CurrentReplay.player1Name);
            string cleanP2 = SanitizeFileName(CurrentReplay.player2Name);
            string timeStamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"Replay_{timeStamp}_{cleanP1}_vs_{cleanP2}.json";

            string json = CurrentReplay.ToJson();

            // Save inside Assets/Replays
            string folderPath = GetReplaysFolderPath();
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string fullPath = Path.Combine(folderPath, fileName);
            File.WriteAllText(fullPath, json);
            Debug.Log($"[ReplayRecorder] Saved replay to: {fullPath}");

            return fullPath;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ReplayRecorder] Failed to save replay: {ex.Message}");
            return null;
        }
    }

    public static string GetReplaysFolderPath()
    {
        string path;
#if UNITY_EDITOR
        path = Path.Combine(Application.dataPath, "Replays");
#else
        path = Path.Combine(Application.persistentDataPath, "Replays");
#endif
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }

    private string SanitizeFileName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Player";
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name.Trim();
    }

    private string CleanRichText(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return System.Text.RegularExpressions.Regex.Replace(input, "<.*?>", string.Empty).Trim();
    }
}
