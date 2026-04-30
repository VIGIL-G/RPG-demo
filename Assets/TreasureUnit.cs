using UnityEngine;
using UnityEngine.Tilemaps;

public class TreasureUnit : MonoBehaviour
{
    [Header("Runtime Owner")]
    public CharacterStats owner;
    public MagicTreasureDefinition sourceDefinition;

    [Header("Runtime Stats")]
    public int attack;
    public int defense;
    public int currentSpiritualPower;
    public int maxSpiritualPower;
    public int currentDurability;
    public int maxDurability;
    public int escapeSpeed;
    public int spiritualCostPerUse;

    [Header("Runtime Combat Mode")]
    public TreasureBattleMode battleMode = TreasureBattleMode.Attacking;

    [Header("Grid Position")]
    public Vector2Int gridPosition;

    private Tilemap _tilemap;

    public void Initialize(CharacterStats ownerCharacter, MagicTreasureDefinition definition, Vector2Int spawnGridPos, Tilemap tilemap, TreasureBattleMode mode)
    {
        owner = ownerCharacter;
        sourceDefinition = definition;
        _tilemap = tilemap;
        battleMode = mode;
        name = definition != null ? $"{definition.treasureName}_Unit" : "TreasureUnit";

        if (definition != null)
        {
            attack = definition.attack;
            defense = definition.defense;
            maxSpiritualPower = definition.maxSpiritualPower;
            currentSpiritualPower = definition.currentSpiritualPower;
            maxDurability = definition.maxDurability;
            currentDurability = definition.currentDurability;
            escapeSpeed = Mathf.Max(1, definition.escapeSpeed);
            spiritualCostPerUse = Mathf.Max(0, definition.spiritualCostPerUse);
        }
        else
        {
            escapeSpeed = 1;
        }

        SetGridPosition(spawnGridPos);
    }

    public bool TryMoveTo(Vector2Int targetGrid)
    {
        if (battleMode == TreasureBattleMode.Guarding) return false;
        if (!IsWithinMoveRange(gridPosition, targetGrid, escapeSpeed)) return false;

        SetGridPosition(targetGrid);
        return true;
    }

    public void SpendSpirit(int amount)
    {
        currentSpiritualPower = Mathf.Max(0, currentSpiritualPower - Mathf.Max(0, amount));
    }

    public void SetGridPosition(Vector2Int newGridPos)
    {
        gridPosition = newGridPos;
        Vector3 worldPos = GridToWorldCenter(newGridPos);
        transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    private Vector3 GridToWorldCenter(Vector2Int pos)
    {
        if (_tilemap != null)
        {
            return _tilemap.GetCellCenterWorld(new Vector3Int(pos.x, pos.y, 0));
        }

        return new Vector3(pos.x, pos.y, 0f);
    }

    private bool IsWithinMoveRange(Vector2Int from, Vector2Int to, int range)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) <= Mathf.Max(1, range);
    }
}
