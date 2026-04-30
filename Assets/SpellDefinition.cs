using UnityEngine;

[CreateAssetMenu(fileName = "Spell", menuName = "Ascension/Spell Definition")]
public class SpellDefinition : ScriptableObject
{
    [Header("Basic")]
    public string spellName = "New Spell";
    public SpellType spellType = SpellType.Offensive;

    [Header("Shared Cost")]
    public int spiritualCost = 10;

    [Header("Offensive / Defensive Stats")]
    public int attackPower = 0;
    public int defensePower = 0;

    [Header("Spell Energy Pool")]
    public int maxSpiritualPower = 0;
    public int currentSpiritualPower = 0;
}
