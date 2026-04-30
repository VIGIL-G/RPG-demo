using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MagicTreasure", menuName = "Ascension/Magic Treasure Definition")]
public class MagicTreasureDefinition : ScriptableObject
{
    [Header("Basic")]
    public string treasureName = "New Magic Treasure";
    public MagicTreasureType treasureType = MagicTreasureType.Versatile;
    public GameObject visualPrefab;
    public Color visualColor = Color.white;

    [Header("Core Stats")]
    public int attack = 0;
    public int defense = 0;
    public int escapeSpeed = 0;
    public int spiritualCostPerUse = 0;

    [Header("Spiritual Power")]
    public int maxSpiritualPower = 100;
    public int currentSpiritualPower = 100;

    [Header("Durability")]
    public int maxDurability = 100;
    public int currentDurability = 100;

    [Header("Special Effects (Placeholder)")]
    public List<string> specialEffectIds = new List<string>();
}
