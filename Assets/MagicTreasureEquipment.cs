using System.Collections.Generic;
using UnityEngine;

public class MagicTreasureEquipment : MonoBehaviour
{
    [SerializeField] private List<MagicTreasureDefinition> equippedTreasures = new List<MagicTreasureDefinition>();

    public IReadOnlyList<MagicTreasureDefinition> EquippedTreasures => equippedTreasures;

    public bool Equip(MagicTreasureDefinition treasure)
    {
        if (treasure == null)
        {
            return false;
        }

        equippedTreasures.Add(treasure);
        return true;
    }

    public bool Unequip(MagicTreasureDefinition treasure)
    {
        if (treasure == null)
        {
            return false;
        }

        return equippedTreasures.Remove(treasure);
    }

    public void ClearAll()
    {
        equippedTreasures.Clear();
    }

    public int GetTotalAttackBonus()
    {
        int total = 0;
        for (int i = 0; i < equippedTreasures.Count; i++)
        {
            if (equippedTreasures[i] == null) continue;
            total += equippedTreasures[i].attack;
        }
        return total;
    }

    public int GetTotalDefenseBonus()
    {
        int total = 0;
        for (int i = 0; i < equippedTreasures.Count; i++)
        {
            if (equippedTreasures[i] == null) continue;
            total += equippedTreasures[i].defense;
        }
        return total;
    }

    public int GetTotalEscapeSpeedBonus()
    {
        int total = 0;
        for (int i = 0; i < equippedTreasures.Count; i++)
        {
            if (equippedTreasures[i] == null) continue;
            total += equippedTreasures[i].escapeSpeed;
        }
        return total;
    }

    public bool Contains(MagicTreasureDefinition treasure)
    {
        return equippedTreasures.Contains(treasure);
    }
}
