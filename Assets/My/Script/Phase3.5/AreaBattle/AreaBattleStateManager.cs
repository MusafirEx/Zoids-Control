using System;
using System.Collections.Generic;
using UnityEngine;

public class AreaBattleStateManager : MonoBehaviour
{
    public static AreaBattleStateManager Instance { get; private set; }

    [Header("Save")]
    [SerializeField] private string playerPrefsKey = "ZOIDS_AREA_BATTLE_STATE_V1";

    [Header("Cooldowns")]
    [SerializeField] private int areaLockHours = 24;
    [SerializeField] private int playerAttemptLockHours = 24;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private AreaBattleStateCollection state = new AreaBattleStateCollection();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public bool CanAttemptArea(int areaId, out string reason)
    {
        reason = "";

        AreaBattleStateData area = GetAreaState(areaId, false);
        if (area == null)
            return true;

        if (area.IsAreaLocked())
        {
            TimeSpan remain = area.GetAreaLockRemaining();
            reason = "Area locked :" + FormatTimeSpan(remain);
            return false;
        }

        if (area.IsPlayerAttemptLocked())
        {
            TimeSpan remain = area.GetPlayerAttemptRemaining();
            reason = "Attempt :" + FormatTimeSpan(remain);
            return false;
        }

        return true;
    }

    public AreaBattleStateData GetAreaState(int areaId, bool createIfMissing = true)
    {
        for (int i = 0; i < state.areas.Count; i++)
        {
            if (state.areas[i].areaId == areaId)
                return state.areas[i];
        }

        if (!createIfMissing)
            return null;

        AreaBattleStateData data = new AreaBattleStateData();
        data.areaId = areaId;
        state.areas.Add(data);
        return data;
    }

    public void ApplyBattleResult(BattleContextData context, bool playerWon)
    {
        if (context == null)
        {
            Debug.LogWarning("[AreaBattleStateManager] Cannot apply result. Context is null.");
            return;
        }

        AreaBattleStateData area = GetAreaState(context.areaId, true);

        DateTime now = DateTime.UtcNow;
        area.lastBattleUtc = now.ToString("o");
        area.lastPlayerWon = playerWon;

        // Player can only try this area once in 24 hours, win or lose.
        area.playerAttemptLockedUntilUtcTicks = now.AddHours(playerAttemptLockHours).Ticks;

        if (playerWon)
        {
            area.ownerFactionId = context.playerFactionId;
            area.ownerFactionName = context.playerFactionName;

            area.defenderUnitIds.Clear();
            if (context.playerUnitIds != null)
                area.defenderUnitIds.AddRange(context.playerUnitIds);

            // Area itself becomes locked for 24 hours after a winning faction captures it.
            area.areaLockedUntilUtcTicks = now.AddHours(areaLockHours).Ticks;
        }

        Save();

        if (debugLog)
        {
            Debug.Log("[AreaBattleStateManager] Result applied. Area=" + context.areaId +
                      " PlayerWon=" + playerWon +
                      " OwnerFaction=" + area.ownerFactionName +
                      " DefenderUnits=" + area.defenderUnitIds.Count);
        }
    }

    public void Save()
    {
        string json = JsonUtility.ToJson(state);
        PlayerPrefs.SetString(playerPrefsKey, json);
        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Saved area state.");
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(playerPrefsKey))
        {
            state = new AreaBattleStateCollection();
            return;
        }

        string json = PlayerPrefs.GetString(playerPrefsKey, "");
        if (string.IsNullOrEmpty(json))
        {
            state = new AreaBattleStateCollection();
            return;
        }

        try
        {
            state = JsonUtility.FromJson<AreaBattleStateCollection>(json);
            if (state == null)
                state = new AreaBattleStateCollection();
        }
        catch
        {
            state = new AreaBattleStateCollection();
        }

        if (state.areas == null)
            state.areas = new List<AreaBattleStateData>();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Loaded area state. Count=" + state.areas.Count);
    }


    public bool IsGlobalPlayerAttemptLocked()
    {
        if (state == null)
            Load();

        if (state == null)
            state = new AreaBattleStateCollection();

        return DateTime.UtcNow.Ticks < state.globalPlayerAttemptLockedUntilUtcTicks;
    }

    public TimeSpan GetGlobalPlayerAttemptRemaining()
    {
        if (state == null)
            Load();

        if (state == null)
            state = new AreaBattleStateCollection();

        long remaining = state.globalPlayerAttemptLockedUntilUtcTicks - DateTime.UtcNow.Ticks;
        return remaining > 0 ? new TimeSpan(remaining) : TimeSpan.Zero;
    }

    public void ClearGlobalPlayerAttemptCooldown()
    {
        if (state == null)
            state = new AreaBattleStateCollection();

        state.globalPlayerAttemptLockedUntilUtcTicks = 0;
        Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared global player attempt cooldown.");
    }


    public void ClearAreaCooldown(int areaId)
    {
        AreaBattleStateData area = GetAreaState(areaId, false);
        if (area == null)
            return;

        area.areaLockedUntilUtcTicks = 0;
        Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared area lock cooldown. Area=" + areaId);
    }

    public void ClearPlayerAttemptCooldown(int areaId)
    {
        AreaBattleStateData area = GetAreaState(areaId, false);
        if (area == null)
            return;

        area.playerAttemptLockedUntilUtcTicks = 0;
        Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared player attempt cooldown. Area=" + areaId);
    }

    public void ClearAllCooldownsForArea(int areaId)
    {
        AreaBattleStateData area = GetAreaState(areaId, false);
        if (area != null)
        {
            area.areaLockedUntilUtcTicks = 0;
            area.playerAttemptLockedUntilUtcTicks = 0;
        }

        if (state == null)
            state = new AreaBattleStateCollection();

        state.globalPlayerAttemptLockedUntilUtcTicks = 0;

        Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared all cooldowns for area and global attempt. Area=" + areaId);
    }

    public void ClearAllCooldownsOnly()
    {
        if (state == null)
            state = new AreaBattleStateCollection();

        state.globalPlayerAttemptLockedUntilUtcTicks = 0;

        if (state.areas != null)
        {
            for (int i = 0; i < state.areas.Count; i++)
            {
                if (state.areas[i] == null) continue;
                state.areas[i].areaLockedUntilUtcTicks = 0;
                state.areas[i].playerAttemptLockedUntilUtcTicks = 0;
            }
        }

        Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared all cooldowns only. Ownership/defenders preserved.");
    }

    public void ClearAllAreaState()
    {
        state = new AreaBattleStateCollection();
        PlayerPrefs.DeleteKey(playerPrefsKey);
        PlayerPrefs.Save();

        if (debugLog)
            Debug.Log("[AreaBattleStateManager] Cleared all area state.");
    }

    public string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return Mathf.CeilToInt((float)time.TotalHours) + "h";

        if (time.TotalMinutes >= 1)
            return Mathf.CeilToInt((float)time.TotalMinutes) + "m";

        return Mathf.CeilToInt((float)time.TotalSeconds) + "s";
    }
}
