using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using TBTK;

public class ZoidsDataCopy : EditorWindow
{
    [Header("Input")]
    [SerializeField] private TextAsset jsonFile;
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private string unitNameFromJson = "";

    [Header("Options")]
    [SerializeField] private bool copyDirectUnitFields = true;
    [SerializeField] private bool copyDefaultStats = true;
    [SerializeField] private bool copyMeleeStats = true;
    [SerializeField] private bool copyRarityAndFactoryCost = true;
    [SerializeField] private bool copyDescription = true;
    [SerializeField] private bool overwriteUnitItemName = false;

    [Header("Damage / Armor Type Mapping")]
    [Tooltip("Default armor type ID when JSON says Light.")]
    [SerializeField] private int lightArmorTypeId = 0;

    [Tooltip("Default armor type ID when JSON says Medium.")]
    [SerializeField] private int mediumArmorTypeId = 1;

    [Tooltip("Default armor type ID when JSON says Heavy.")]
    [SerializeField] private int heavyArmorTypeId = 2;

    [Tooltip("Default damage type ID for range/default attack.")]
    [SerializeField] private int defaultRangeDamageTypeId = 0;

    [Tooltip("Default damage type ID for melee attack.")]
    [SerializeField] private int defaultMeleeDamageTypeId = 0;

    private ZoidsUnitStatsJson loadedJson;
    private Vector2 scrollPos;
    private string lastMessage = "";
    private MessageType lastMessageType = MessageType.Info;

    [MenuItem("Tools/Zoids/Zoids Data Copy")]
    public static void OpenWindow()
    {
        ZoidsDataCopy window = GetWindow<ZoidsDataCopy>("Zoids Data Copy");
        window.minSize = new Vector2(520, 580);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Zoids Data Copy", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Assign one prefab that has TBTK.Unit, type the Zoid name from JSON, then press Copy Data To Prefab. " +
            "This tool is for one-by-one prefab setup when you do not have prefabs for every Zoid.",
            MessageType.Info
        );

        EditorGUILayout.Space(8);
        DrawInputs();

        EditorGUILayout.Space(8);
        DrawOptions();

        EditorGUILayout.Space(8);
        DrawButtons();

        EditorGUILayout.Space(8);
        DrawPreview();

        if (!string.IsNullOrEmpty(lastMessage))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(lastMessage, lastMessageType);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawInputs()
    {
        EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);

        jsonFile = (TextAsset)EditorGUILayout.ObjectField("Stats JSON", jsonFile, typeof(TextAsset), false);
        unitPrefab = (GameObject)EditorGUILayout.ObjectField("Unit Prefab", unitPrefab, typeof(GameObject), false);
        unitNameFromJson = EditorGUILayout.TextField("Name From JSON", unitNameFromJson);

        if (jsonFile == null)
        {
            EditorGUILayout.HelpBox("Assign ZoidsUnitStats_SuggestedStatsFixed.json here.", MessageType.Warning);
        }

        if (unitPrefab != null && unitPrefab.GetComponent<Unit>() == null)
        {
            EditorGUILayout.HelpBox("The assigned prefab does not have Unit.cs on the root GameObject.", MessageType.Error);
        }
    }

    private void DrawOptions()
    {
        EditorGUILayout.LabelField("Copy Options", EditorStyles.boldLabel);

        copyDirectUnitFields = EditorGUILayout.Toggle("Copy Direct Unit Fields", copyDirectUnitFields);
        copyDefaultStats = EditorGUILayout.Toggle("Copy Default/Range Stats", copyDefaultStats);
        copyMeleeStats = EditorGUILayout.Toggle("Copy Melee Stats", copyMeleeStats);
        copyRarityAndFactoryCost = EditorGUILayout.Toggle("Copy Rarity + Factory Cost", copyRarityAndFactoryCost);
        copyDescription = EditorGUILayout.Toggle("Copy Description", copyDescription);
        overwriteUnitItemName = EditorGUILayout.Toggle("Overwrite Unit Item Name", overwriteUnitItemName);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Type Mapping", EditorStyles.boldLabel);

        lightArmorTypeId = EditorGUILayout.IntField("Light Armor Type ID", lightArmorTypeId);
        mediumArmorTypeId = EditorGUILayout.IntField("Medium Armor Type ID", mediumArmorTypeId);
        heavyArmorTypeId = EditorGUILayout.IntField("Heavy Armor Type ID", heavyArmorTypeId);
        defaultRangeDamageTypeId = EditorGUILayout.IntField("Range Damage Type ID", defaultRangeDamageTypeId);
        defaultMeleeDamageTypeId = EditorGUILayout.IntField("Melee Damage Type ID", defaultMeleeDamageTypeId);
    }

    private void DrawButtons()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Load JSON", GUILayout.Height(32)))
        {
            LoadJson();
        }

        if (GUILayout.Button("Find Name", GUILayout.Height(32)))
        {
            FindByNameAndShowMessage();
        }

        EditorGUILayout.EndHorizontal();

        GUI.enabled = jsonFile != null && unitPrefab != null && !string.IsNullOrWhiteSpace(unitNameFromJson);

        if (GUILayout.Button("Copy Data To Prefab", GUILayout.Height(38)))
        {
            CopyDataToPrefab();
        }

        GUI.enabled = true;
    }

    private void DrawPreview()
    {
        if (jsonFile == null || string.IsNullOrWhiteSpace(unitNameFromJson))
            return;

        ZoidsUnitStatEntry entry = FindEntryByName(unitNameFromJson);
        if (entry == null)
            return;

        EditorGUILayout.LabelField("JSON Preview", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Unit ID", entry.unitId.ToString());
        EditorGUILayout.LabelField("Unit Name", entry.unitName);
        EditorGUILayout.LabelField("Rarity", entry.rarity);
        EditorGUILayout.LabelField("Role", entry.role);
        EditorGUILayout.LabelField("Size Class", entry.sizeClass);
        EditorGUILayout.LabelField("Primary Attack Type", entry.primaryAttackType);
        EditorGUILayout.LabelField("Factory Data Cost", entry.factoryDataCost.ToString());

        if (entry.unitFields != null)
        {
            EditorGUILayout.LabelField("HP / AP / Move", entry.unitFields.hp + " / " + entry.unitFields.ap + " / " + entry.unitFields.moveSpeed);
            EditorGUILayout.LabelField("Has Melee", entry.unitFields.hasMeleeAttack.ToString());
            EditorGUILayout.LabelField("Armor Type Name", entry.unitFields.armorTypeName);
            EditorGUILayout.LabelField("Damage Type Name", entry.unitFields.damageTypeName);
            EditorGUILayout.LabelField("Melee Damage Type Name", entry.unitFields.damageTypeMeleeName);
        }

        if (entry.stats != null)
        {
            EditorGUILayout.LabelField("Default Attack / Def", entry.stats.attack + " / " + entry.stats.defense);
            EditorGUILayout.LabelField("Default Range", entry.stats.attackRangeMin + " - " + entry.stats.attackRange);
        }

        if (entry.statsMelee != null)
        {
            EditorGUILayout.LabelField("Melee Attack", entry.statsMelee.attack.ToString());
            EditorGUILayout.LabelField("Melee Range", entry.statsMelee.attackRange.ToString());
        }
    }

    private void LoadJson()
    {
        loadedJson = null;

        if (jsonFile == null)
        {
            SetMessage("JSON file is missing.", MessageType.Error);
            return;
        }

        try
        {
            loadedJson = JsonUtility.FromJson<ZoidsUnitStatsJson>(jsonFile.text);
        }
        catch (Exception ex)
        {
            SetMessage("Failed to parse JSON: " + ex.Message, MessageType.Error);
            return;
        }

        if (loadedJson == null || loadedJson.units == null || loadedJson.units.Count == 0)
        {
            SetMessage("JSON loaded but no units were found.", MessageType.Error);
            return;
        }

        SetMessage("JSON loaded. Units found: " + loadedJson.units.Count, MessageType.Info);
    }

    private void FindByNameAndShowMessage()
    {
        ZoidsUnitStatEntry entry = FindEntryByName(unitNameFromJson);

        if (entry == null)
        {
            SetMessage("No Zoid found in JSON with name: " + unitNameFromJson, MessageType.Warning);
            return;
        }

        SetMessage("Found: " + entry.unitName + " | Rarity=" + entry.rarity + " | Role=" + entry.role, MessageType.Info);
    }

    private ZoidsUnitStatEntry FindEntryByName(string searchName)
    {
        if (loadedJson == null)
            LoadJson();

        if (loadedJson == null || loadedJson.units == null)
            return null;

        if (string.IsNullOrWhiteSpace(searchName))
            return null;

        string normalizedSearch = NormalizeName(searchName);

        // Exact normalized match first.
        for (int i = 0; i < loadedJson.units.Count; i++)
        {
            ZoidsUnitStatEntry unit = loadedJson.units[i];
            if (unit == null) continue;

            if (NormalizeName(unit.unitName) == normalizedSearch)
                return unit;
        }

        // Then contains match for convenience.
        for (int i = 0; i < loadedJson.units.Count; i++)
        {
            ZoidsUnitStatEntry unit = loadedJson.units[i];
            if (unit == null) continue;

            if (NormalizeName(unit.unitName).Contains(normalizedSearch))
                return unit;
        }

        return null;
    }

    private string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value.Trim().ToLowerInvariant()
            .Replace("-", "")
            .Replace("_", "")
            .Replace(" ", "")
            .Replace("'", "")
            .Replace("\"", "");
    }

    private void CopyDataToPrefab()
    {
        if (unitPrefab == null)
        {
            SetMessage("Unit prefab is missing.", MessageType.Error);
            return;
        }

        Unit unit = unitPrefab.GetComponent<Unit>();
        if (unit == null)
        {
            SetMessage("Selected prefab does not have Unit.cs on root GameObject.", MessageType.Error);
            return;
        }

        ZoidsUnitStatEntry entry = FindEntryByName(unitNameFromJson);
        if (entry == null)
        {
            SetMessage("No matching Zoid found in JSON: " + unitNameFromJson, MessageType.Error);
            return;
        }

        string prefabPath = AssetDatabase.GetAssetPath(unitPrefab);
        if (string.IsNullOrEmpty(prefabPath))
        {
            SetMessage("Selected object is not a prefab asset from the Project window.", MessageType.Error);
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        if (prefabRoot == null)
        {
            SetMessage("Failed to load prefab contents: " + prefabPath, MessageType.Error);
            return;
        }

        try
        {
            Unit prefabUnit = prefabRoot.GetComponent<Unit>();
            if (prefabUnit == null)
            {
                SetMessage("Prefab contents do not have Unit.cs on root GameObject.", MessageType.Error);
                return;
            }

            Undo.RegisterCompleteObjectUndo(prefabUnit, "Zoids Data Copy");

            ApplyEntryToUnit(entry, prefabUnit);

            EditorUtility.SetDirty(prefabUnit);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetMessage("Copied JSON data from [" + entry.unitName + "] to prefab: " + unitPrefab.name, MessageType.Info);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private void ApplyEntryToUnit(ZoidsUnitStatEntry entry, Unit unit)
    {
        if (entry == null || unit == null)
            return;

        if (copyRarityAndFactoryCost)
        {
            unit.rarity = ParseRarity(entry.rarity, unit.rarity);
            unit.factoryCost = Mathf.Max(1, entry.factoryDataCost);
        }

        if (copyDescription)
        {
            unit.unitDescription =
                "Role: " + Safe(entry.role) +
                "\nSize: " + Safe(entry.sizeClass) +
                "\nAttack Type: " + Safe(entry.primaryAttackType);
        }

        if (overwriteUnitItemName && !string.IsNullOrEmpty(entry.unitName))
            unit.itemName = entry.unitName;

        if (copyDirectUnitFields && entry.unitFields != null)
        {
            unit.hp = entry.unitFields.hp;
            unit.ap = entry.unitFields.ap;
            unit.moveSpeed = entry.unitFields.moveSpeed;
            unit.hasMeleeAttack = entry.unitFields.hasMeleeAttack;

            unit.armorType = ParseArmorType(entry.unitFields.armorTypeName);
            unit.damageType = defaultRangeDamageTypeId;
            unit.damageTypeMelee = defaultMeleeDamageTypeId;
        }

        if (copyDefaultStats && entry.stats != null)
        {
            if (unit.stats == null)
                unit.stats = new Stats();

            ApplyStats(entry.stats, unit.stats, true);
        }

        if (copyMeleeStats && entry.statsMelee != null)
        {
            if (unit.statsMelee == null)
                unit.statsMelee = new Stats();

            // Start melee stats from default stats so missing melee fields remain useful.
            if (unit.stats != null)
                CopyStats(unit.stats, unit.statsMelee);

            ApplyStats(entry.statsMelee, unit.statsMelee, false);

            // Force melee range defaults.
            unit.statsMelee.attackRangeMin = 0;
            if (unit.statsMelee.attackRange <= 0)
                unit.statsMelee.attackRange = 1;
        }
    }

    private string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "-" : value;
    }

    private UnitRarity ParseRarity(string rarity, UnitRarity fallback)
    {
        if (string.IsNullOrEmpty(rarity))
            return fallback;

        UnitRarity parsed;
        if (Enum.TryParse(rarity, true, out parsed))
            return parsed;

        return fallback;
    }

    private int ParseArmorType(string armorTypeName)
    {
        string value = armorTypeName == null ? "" : armorTypeName.ToLowerInvariant();

        if (value.Contains("light"))
            return lightArmorTypeId;

        if (value.Contains("heavy"))
            return heavyArmorTypeId;

        if (value.Contains("medium"))
            return mediumArmorTypeId;

        return mediumArmorTypeId;
    }

    private void ApplyStats(ZoidsStatsJson src, Stats dst, bool includeFullStats)
    {
        if (src == null || dst == null)
            return;

        if (includeFullStats)
        {
            dst.hp = src.hp;
            dst.ap = src.ap;
            dst.hpRegen = src.hpRegen;
            dst.apRegen = src.apRegen;
            dst.defense = src.defense;
            dst.dodge = src.dodge;
            dst.critReduc = src.critReduc;

            dst.cDmgMultip = src.cDmgMul;
            dst.cHitPenalty = src.cHitPenalty;
            dst.cCritPenalty = src.cCritPenalty;

            dst.oDmgMultip = src.oDmgMul;
            dst.oHitPenalty = src.oHitPenalty;
            dst.oCritPenalty = src.oCritPenalty;

            dst.attackRangeMin = src.attackRangeMin;
            dst.moveRange = src.moveRange;
            dst.sight = src.sight;
            dst.turnPriority = src.turnPriority;

            dst.moveLimit = src.moveLimit;
            dst.attackLimit = src.attackLimit;
            dst.counterLimit = src.counterLimit;
            dst.abilityLimit = src.abilityLimit;
        }

        dst.attack = src.attack;
        dst.hit = src.hit;
        dst.dmgHPMin = src.dmgHPMin;
        dst.dmgHPMax = src.dmgHPMax;
        dst.dmgAPMin = src.dmgAPMin;
        dst.dmgAPMax = src.dmgAPMax;
        dst.critChance = src.critChance;
        dst.critMultiplier = src.critMultiplier;
        dst.attackRange = src.attackRange;
    }

    private void CopyStats(Stats src, Stats dst)
    {
        if (src == null || dst == null)
            return;

        dst.hp = src.hp;
        dst.ap = src.ap;
        dst.hpRegen = src.hpRegen;
        dst.apRegen = src.apRegen;
        dst.attack = src.attack;
        dst.defense = src.defense;
        dst.hit = src.hit;
        dst.dodge = src.dodge;
        dst.dmgHPMin = src.dmgHPMin;
        dst.dmgHPMax = src.dmgHPMax;
        dst.dmgAPMin = src.dmgAPMin;
        dst.dmgAPMax = src.dmgAPMax;
        dst.critChance = src.critChance;
        dst.critReduc = src.critReduc;
        dst.critMultiplier = src.critMultiplier;
        dst.cDmgMultip = src.cDmgMultip;
        dst.cHitPenalty = src.cHitPenalty;
        dst.cCritPenalty = src.cCritPenalty;
        dst.oDmgMultip = src.oDmgMultip;
        dst.oHitPenalty = src.oHitPenalty;
        dst.oCritPenalty = src.oCritPenalty;
        dst.attackRange = src.attackRange;
        dst.attackRangeMin = src.attackRangeMin;
        dst.moveRange = src.moveRange;
        dst.turnPriority = src.turnPriority;
        dst.sight = src.sight;
        dst.moveLimit = src.moveLimit;
        dst.attackLimit = src.attackLimit;
        dst.counterLimit = src.counterLimit;
        dst.abilityLimit = src.abilityLimit;
    }

    private void SetMessage(string message, MessageType type)
    {
        lastMessage = message;
        lastMessageType = type;
        Repaint();
    }
}

[Serializable]
public class ZoidsUnitStatsJson
{
    public string schema;
    public string sourceWorkbook;
    public string sourceSheet;
    public string description;
    public string importNotes;
    public int count;
    public List<ZoidsUnitStatEntry> units = new List<ZoidsUnitStatEntry>();
}

[Serializable]
public class ZoidsUnitStatEntry
{
    public int unitId;
    public string unitName;
    public string rarity;
    public string role;
    public string sizeClass;
    public string primaryAttackType;
    public int factoryDataCost;
    public ZoidsUnitFieldsJson unitFields;
    public ZoidsStatsJson stats;
    public ZoidsStatsJson statsMelee;
}

[Serializable]
public class ZoidsUnitFieldsJson
{
    public float hp;
    public float ap;
    public float moveSpeed;
    public bool hasMeleeAttack;
    public string armorTypeName;
    public string damageTypeName;
    public string damageTypeMeleeName;
}

[Serializable]
public class ZoidsStatsJson
{
    public float hp;
    public float ap;
    public float hpRegen;
    public float apRegen;
    public float attack;
    public float defense;
    public float hit;
    public float dodge;
    public float dmgHPMin;
    public float dmgHPMax;
    public float dmgAPMin;
    public float dmgAPMax;
    public float critChance;
    public float critReduc;
    public float critMultiplier;

    public float cDmgMul;
    public float cHitPenalty;
    public float cCritPenalty;

    public float oDmgMul;
    public float oHitPenalty;
    public float oCritPenalty;

    public float attackRangeMin;
    public float attackRange;
    public float moveRange;
    public float sight;
    public float turnPriority;

    public float moveLimit;
    public float attackLimit;
    public float counterLimit;
    public float abilityLimit;
}
