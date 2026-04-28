using UnityEngine;

public class UnitRewardCalculator : MonoBehaviour
{
    [Header("Win Reward By Rarity")]
    [SerializeField] private int commonWinData = 10;
    [SerializeField] private int uncommonWinData = 8;
    [SerializeField] private int rareWinData = 6;
    [SerializeField] private int epicWinData = 4;
    [SerializeField] private int legendaryWinData = 3;

    [Header("Loss Reward")]
    [SerializeField] private int loseDataPerDefeatedUnit = 1;

    [Header("References")]
    [SerializeField] private FactoryUnitDatabase factoryDatabase;

    public int GetRewardAmount(int unitId, bool playerWon)
    {
        if (!playerWon)
            return Mathf.Max(0, loseDataPerDefeatedUnit);

        UnitRarity rarity = GetRarity(unitId);
        switch (rarity)
        {
            case UnitRarity.Common: return commonWinData;
            case UnitRarity.Uncommon: return uncommonWinData;
            case UnitRarity.Rare: return rareWinData;
            case UnitRarity.Epic: return epicWinData;
            case UnitRarity.Legendary: return legendaryWinData;
            default: return commonWinData;
        }
    }

    public UnitRarity GetRarity(int unitId)
    {
        if (factoryDatabase == null)
            return UnitRarity.Common;

        FactoryUnitDefinition def = factoryDatabase.GetUnit(unitId);
        if (def == null)
            return UnitRarity.Common;

        return def.rarity;
    }
}
