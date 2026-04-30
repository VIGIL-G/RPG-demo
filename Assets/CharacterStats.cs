using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MagicTreasureEquipment))]
public class CharacterStats : MonoBehaviour
{
    [Header("Identity")]
    public string characterName = "Cultivator";
    public int realmLevel = 1;

    [Header("Combat Stats")]
    public int maxHP = 100;
    public int currentHP = 100;
    public int maxMP = 50;
    public int currentMP = 50;
    public int speed = 10;
    [Min(1)] public int baseMoveRange = 4;

    [Header("Grid Position")]
    public Vector2Int gridPosition;

    [Header("Magic Treasures")]
    public MagicTreasureEquipment treasureEquipment;

    [Header("Spells")]
    public List<SpellDefinition> spells = new List<SpellDefinition>();

    private void Start()
    {
        EnsureEquipmentComponent();
        gridPosition = new Vector2Int(
            Mathf.RoundToInt(transform.position.x),
            Mathf.RoundToInt(transform.position.y)
        );
    }

    private void Reset()
    {
        EnsureEquipmentComponent();
    }

    private void OnValidate()
    {
        EnsureEquipmentComponent();
    }

    private void EnsureEquipmentComponent()
    {
        if (treasureEquipment == null)
        {
            treasureEquipment = GetComponent<MagicTreasureEquipment>();
        }
    }

    public int GetCurrentMoveRange()
    {
        int bonus = treasureEquipment != null ? treasureEquipment.GetTotalEscapeSpeedBonus() : 0;
        return Mathf.Max(1, baseMoveRange + bonus);
    }

    public bool TrySpendSpiritualPower(int amount)
    {
        int cost = Mathf.Max(0, amount);
        if (currentMP < cost)
        {
            return false;
        }

        currentMP -= cost;
        return true;
    }
}