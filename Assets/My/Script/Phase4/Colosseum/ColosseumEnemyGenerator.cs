using System.Collections.Generic;
using UnityEngine;
using TBTK;

public class ColosseumEnemyGenerator : MonoBehaviour
{
    [Header("Enemy Pool")]
    [Tooltip("If empty, generator will use all units from UnitDB.")]
    [SerializeField] private List<int> allowedEnemyUnitIds = new List<int>();

    [Header("Balance")]
    [SerializeField] private bool allowLegendaryBeforeFinalRound = false;
    [SerializeField] private bool debugLog = true;

    public List<int> GenerateEnemyTeam(List<int> playerUnitIds, int battleSize, int currentRound, int totalRounds)
    {
        List<int> result = new List<int>();

        battleSize = Mathf.Clamp(battleSize, 1, 10);

        List<Unit> candidateUnits = BuildCandidateUnits(currentRound, totalRounds);
        if (candidateUnits.Count == 0)
        {
            Debug.LogWarning("[ColosseumEnemyGenerator] No enemy candidates found.");
            return result;
        }

        int targetPower = CalculatePlayerPower(playerUnitIds, battleSize);
        float difficulty = GetRoundDifficultyMultiplier(currentRound, totalRounds);
        int desiredPower = Mathf.Max(1, Mathf.RoundToInt(targetPower * difficulty));

        result = PickBalancedTeam(candidateUnits, battleSize, desiredPower);

        if (debugLog)
        {
            Debug.Log("[ColosseumEnemyGenerator] Generated enemy team. Round=" + currentRound +
                      "/" + totalRounds +
                      " BattleSize=" + battleSize +
                      " TargetPower=" + targetPower +
                      " DesiredPower=" + desiredPower +
                      " EnemyCount=" + result.Count);
        }

        return result;
    }

    private List<Unit> BuildCandidateUnits(int currentRound, int totalRounds)
    {
        List<Unit> candidates = new List<Unit>();

        if (allowedEnemyUnitIds != null && allowedEnemyUnitIds.Count > 0)
        {
            for (int i = 0; i < allowedEnemyUnitIds.Count; i++)
            {
                Unit unit = UnitDB.GetPrefab(allowedEnemyUnitIds[i]);
                if (IsValidCandidate(unit, currentRound, totalRounds))
                    candidates.Add(unit);
            }

            return candidates;
        }

        List<Unit> allUnits = UnitDB.GetList();
        if (allUnits == null)
            return candidates;

        for (int i = 0; i < allUnits.Count; i++)
        {
            Unit unit = allUnits[i];
            if (IsValidCandidate(unit, currentRound, totalRounds))
                candidates.Add(unit);
        }

        return candidates;
    }

    private bool IsValidCandidate(Unit unit, int currentRound, int totalRounds)
    {
        if (unit == null)
            return false;

        if (!allowLegendaryBeforeFinalRound && currentRound < totalRounds && unit.rarity == UnitRarity.Legendary)
            return false;

        return true;
    }

    private List<int> PickBalancedTeam(List<Unit> candidates, int battleSize, int desiredPower)
    {
        List<int> bestTeam = new List<int>();
        int bestDiff = int.MaxValue;

        // Try multiple random combinations and keep the closest power.
        for (int attempt = 0; attempt < 80; attempt++)
        {
            List<int> tempTeam = new List<int>();
            int tempPower = 0;

            for (int i = 0; i < battleSize; i++)
            {
                Unit picked = candidates[Random.Range(0, candidates.Count)];
                tempTeam.Add(picked.prefabID);
                tempPower += GetRarityPower(picked.rarity);
            }

            int diff = Mathf.Abs(desiredPower - tempPower);
            if (diff < bestDiff)
            {
                bestDiff = diff;
                bestTeam = tempTeam;
            }

            if (bestDiff == 0)
                break;
        }

        return bestTeam;
    }

    private int CalculatePlayerPower(List<int> playerUnitIds, int battleSize)
    {
        if (playerUnitIds == null || playerUnitIds.Count == 0)
            return battleSize;

        int count = Mathf.Min(battleSize, playerUnitIds.Count);
        int power = 0;

        for (int i = 0; i < count; i++)
        {
            Unit unit = UnitDB.GetPrefab(playerUnitIds[i]);
            power += unit != null ? GetRarityPower(unit.rarity) : 1;
        }

        return Mathf.Max(1, power);
    }

    private float GetRoundDifficultyMultiplier(int currentRound, int totalRounds)
    {
        if (totalRounds <= 1)
            return 1f;

        float t = Mathf.InverseLerp(1, totalRounds, currentRound);

        // Round 1 approx 85%, final approx 125%.
        return Mathf.Lerp(0.85f, 1.25f, t);
    }

    private int GetRarityPower(UnitRarity rarity)
    {
        switch (rarity)
        {
            case UnitRarity.Common: return 1;
            case UnitRarity.Uncommon: return 2;
            case UnitRarity.Rare: return 3;
            case UnitRarity.Epic: return 4;
            case UnitRarity.Legendary: return 5;
            default: return 1;
        }
    }
}
