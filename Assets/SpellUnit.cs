using UnityEngine;
using UnityEngine.Tilemaps;

public class SpellUnit : MonoBehaviour
{
    public CharacterStats owner;
    public SpellDefinition sourceDefinition;

    public SpellType spellType = SpellType.Offensive;
    public int attackPower;
    public int defensePower;
    public int spiritualCost;
    public int maxSpiritualPower;
    public int currentSpiritualPower;
    public Vector2Int gridPosition;

    private Tilemap _tilemap;

    public void Initialize(CharacterStats ownerCharacter, SpellDefinition definition, Vector2Int spawnPos, Tilemap tilemap)
    {
        owner = ownerCharacter;
        sourceDefinition = definition;
        _tilemap = tilemap;

        if (definition != null)
        {
            spellType = definition.spellType;
            attackPower = Mathf.Max(0, definition.attackPower);
            defensePower = Mathf.Max(0, definition.defensePower);
            spiritualCost = Mathf.Max(0, definition.spiritualCost);
            maxSpiritualPower = definition.maxSpiritualPower > 0 ? definition.maxSpiritualPower : spiritualCost;
            currentSpiritualPower = definition.currentSpiritualPower > 0 ? definition.currentSpiritualPower : maxSpiritualPower;
            currentSpiritualPower = Mathf.Clamp(currentSpiritualPower, 0, maxSpiritualPower);
        }

        SetGridPosition(spawnPos);
    }

    public int GetCombatPower()
    {
        if (spellType == SpellType.Defensive) return Mathf.Max(0, defensePower);
        if (spellType == SpellType.ArtifactControl) return 0;
        return Mathf.Max(0, attackPower);
    }

    public void SetGridPosition(Vector2Int newGridPos)
    {
        gridPosition = newGridPos;
        Vector3 pos = _tilemap != null
            ? _tilemap.GetCellCenterWorld(new Vector3Int(newGridPos.x, newGridPos.y, 0))
            : new Vector3(newGridPos.x, newGridPos.y, 0f);
        transform.position = new Vector3(pos.x, pos.y, 0f);
    }

    public bool IsExpired()
    {
        return currentSpiritualPower <= 0;
    }
}
