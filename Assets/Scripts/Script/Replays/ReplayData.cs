using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ReplayData
{
    public string replayId;
    public string matchDate;
    public string gameMode;
    public int totalTurns;
    public string winnerPlayerName;

    // Player 1 info
    public string player1Name;
    public string player1DeckName;
    public string player1DeckCode;
    public List<string> player1MainDeckCardIDs = new List<string>();
    public List<string> player1EggDeckCardIDs = new List<string>();

    // Player 2 info
    public string player2Name;
    public string player2DeckName;
    public string player2DeckCode;
    public List<string> player2MainDeckCardIDs = new List<string>();
    public List<string> player2EggDeckCardIDs = new List<string>();

    public int firstPlayerIndex;

    // Chronological list of steps/actions in match
    public List<ReplayStep> steps = new List<ReplayStep>();

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static ReplayData FromJson(string json)
    {
        return JsonUtility.FromJson<ReplayData>(json);
    }
}

[System.Serializable]
public class ReplayPermanentData
{
    public int frameId;
    public List<string> cardIds = new List<string>();
    public bool isSuspended;
    public int dp;
    public int level;
    public bool isDigiEgg;
}

[System.Serializable]
public class ReplayStep
{
    public int stepIndex;
    public int turnNumber;
    public int activePlayerId;
    public string activePlayerName;
    public string phaseName;
    public string actionType;
    public string description;
    public int memory;
    public string cardId;
    public string cardName;
    public string sourceLocation;
    public string targetLocation;

    // Action metadata for VFX / audio
    public int attackerFrameId = -1;
    public int targetFrameId = -1;
    public bool isSecurityAttack = false;

    // Snapshots for hand / security
    public List<string> p1Hand = new List<string>();
    public List<string> p2Hand = new List<string>();
    public List<string> p1Security = new List<string>();
    public List<string> p2Security = new List<string>();

    // Trash snapshots
    public List<string> p1Trash = new List<string>();
    public List<string> p2Trash = new List<string>();

    // Field Permanents
    public List<ReplayPermanentData> p1Permanents = new List<ReplayPermanentData>();
    public List<ReplayPermanentData> p2Permanents = new List<ReplayPermanentData>();

    // Breeding Area
    public ReplayPermanentData p1Breeding = null;
    public ReplayPermanentData p2Breeding = null;

    // Deck counts
    public int p1MainDeckCount;
    public int p2MainDeckCount;
    public int p1EggDeckCount;
    public int p2EggDeckCount;
}
