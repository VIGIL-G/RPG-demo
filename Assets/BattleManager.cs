using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class BattleManager : MonoBehaviour
{
    public enum BattleState { PlayerMenu, PlayerMoving, EnemyTurn }
    public enum TreasureAttackRule { SameCell, Range1 }

    public BattleState state;

    [Header("Character References")]
    public CharacterStats player;
    public CharacterStats enemy;

    [Header("Enemy Movement")]
    [Min(1)] public int enemyMoveRange = 2;
    [Min(1)] public int enemySpellRange = 1;

    [Header("Board Spawn (Normalized)")]
    [Range(0f, 1f)] public float playerStartNormalizedX = 0.25f;
    [Range(0f, 1f)] public float enemyStartNormalizedX = 0.75f;
    [Range(0f, 1f)] public float startNormalizedY = 0.5f;
    public Tilemap battleTilemap;

    [Header("Visual Scale")]
    [Range(0.1f, 1f)] public float playerVisualScale = 0.39f;
    [Range(0.1f, 1f)] public float enemyVisualScale = 0.39f;
    [Range(0.1f, 1f)] public float treasureVisualScale = 0.32f;
    [Range(0.1f, 1f)] public float spellVisualScale = 0.25f;

    [Header("Demo Treasures")]
    public bool autoEquipDemoTreasures = true;
    public string shieldResourcePath = "MagicTreasures/ShuiYuanDun";
    public string swordResourcePath = "MagicTreasures/QingYuanJian";

    [Header("Demo Spells")]
    public bool autoLoadArtifactControlSpell = true;
    public string artifactControlSpellPath = "Spells/YuWuShu";
    public string enemyAttackSpellPath = "Spells/YaoShouAttack";
    public SpellDefinition playerArtifactControlSpell;
    public SpellDefinition enemyAttackSpell;

    [Header("UI References")]
    public TextMeshProUGUI turnIndicator;
    public GameObject actionMenu;
    public GameObject treasureSelectMenu;
    public GameObject treasureModeMenu;
    public RectTransform uiMenuRoot;

    [Header("UI Layout")]
    public Vector2 treasureAttackButtonPos = new Vector2(80f, 135f);
    public Vector2 treasureSelectMenuPos = new Vector2(270f, 75f);
    public Vector2 treasureModeMenuPos = new Vector2(450f, 75f);

    [Header("Rules")]
    public TreasureAttackRule treasureAttackRule = TreasureAttackRule.SameCell;
    public bool enableCombatDebugLog = true;

    [Header("Debug Panel")]
    public bool showCombatDebugPanel = false;

    private readonly List<TreasureUnit> _playerSummonedTreasures = new List<TreasureUnit>();
    private readonly List<TreasureUnit> _playerGuardTreasures = new List<TreasureUnit>();
    private readonly List<SpellUnit> _activeSpells = new List<SpellUnit>();
    private readonly HashSet<MagicTreasureDefinition> _summonedTreasureDefs = new HashSet<MagicTreasureDefinition>();

    private Camera _mainCamera;
    private bool _isMovePending;
    private Vector2Int _pendingMoveTarget;
    private Vector2Int _playerTurnStartPos;

    private bool _isTreasureMoveMode;
    private bool _isTreasureMovePending;
    private TreasureUnit _activeTreasure;
    private Vector2Int _pendingTreasureTarget;
    private Vector2Int _treasureTurnStartPos;

    private bool _isDeployPositionSelecting;
    private bool _isTreasureAttackSelecting;
    private MagicTreasureDefinition _pendingTreasureDefinition;
    private TreasureBattleMode _pendingTreasureMode = TreasureBattleMode.Attacking;

    private bool _playerMovedThisTurn;
    private bool _treasureMovedThisTurn;
    private bool _treasureAttackedThisTurn;

    private static Sprite _defaultTreasureSprite;
    private static Sprite _defaultSpellSprite;
    private TMP_FontAsset _runtimeChineseFont;
    private bool _battleEnded;
    private string _lastCollisionSummary = "无";

    void Start()
    {
        _mainCamera = Camera.main;
        EnsureChineseFontSupport();
        AutoBindCharactersIfNeeded();
        AutoBindTilemapIfNeeded();
        TryBindExistingBattleMenus();
        BindPersistentBattleMenuCallbacks();
        ApplyPersistentMenuLayout();
        ValidatePersistentBattleMenus();
        PlaceCharactersAtBoardStartPositions();
        ApplyCharacterVisualScale();
        SnapCharactersToGrid();
        AutoEquipDemoTreasures();
        AutoLoadDemoSpell();
        _battleEnded = false;
        StartPlayerTurn();
    }

    void Update()
    {
        if (_battleEnded) return;
        SyncGuardTreasuresWithPlayer();

        if (_isDeployPositionSelecting)
        {
            if (Input.GetMouseButtonDown(0)) SelectTreasureDeployPosition();
            if (Input.GetMouseButtonDown(1)) CancelTreasureDeploy();
            return;
        }

        if (_isTreasureAttackSelecting)
        {
            if (Input.GetMouseButtonDown(0)) SelectTreasureAttackTarget();
            if (Input.GetMouseButtonDown(1)) CancelTreasureAttackSelection();
            return;
        }

        if (_isTreasureMoveMode)
        {
            if (Input.GetMouseButtonDown(0)) SelectTreasureMoveTarget();
            if (Input.GetMouseButtonDown(1)) CancelTreasureMove();
            return;
        }

        if (state != BattleState.PlayerMoving)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0)) SelectMoveTarget();
        if (Input.GetMouseButtonDown(1)) CancelPendingMove();
    }

    public void StartPlayerTurn()
    {
        if (_battleEnded) return;
        if (player == null)
        {
            Debug.LogError("BattleManager 缺少玩家引用。");
            return;
        }

        state = BattleState.PlayerMenu;
        _isMovePending = false;
        _isTreasureMoveMode = false;
        _isTreasureMovePending = false;
        _isDeployPositionSelecting = false;
        _isTreasureAttackSelecting = false;
        _activeTreasure = null;
        _playerTurnStartPos = player.gridPosition;
        _playerMovedThisTurn = false;
        _treasureMovedThisTurn = false;
        _treasureAttackedThisTurn = false;
        SetTurnText("当前回合: 玩家");
        SetActionMenu(true);
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);
    }

    public void OnMoveButtonClick()
    {
        if (player == null || _isTreasureMoveMode || _isDeployPositionSelecting || _isTreasureAttackSelecting) return;

        if (state == BattleState.PlayerMenu)
        {
            if (_playerMovedThisTurn)
            {
                SetTurnText("本回合角色已移动");
                return;
            }
            BeginMoveSelection();
            return;
        }

        if (state == BattleState.PlayerMoving && _isMovePending)
        {
            ConfirmPendingMove();
        }
    }

    public void OnEndTurnButtonClick()
    {
        if (_battleEnded) return;
        if (_isDeployPositionSelecting)
        {
            CancelTreasureDeploy();
            return;
        }

        if (_isTreasureAttackSelecting)
        {
            CancelTreasureAttackSelection();
            return;
        }

        if (_isTreasureMoveMode)
        {
            CancelTreasureMove();
            return;
        }

        if (state == BattleState.PlayerMoving)
        {
            CancelPendingMove();
            return;
        }

        SetActionMenu(false);
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);
        StartCoroutine(EnemyTurnRoutine());
    }

    public void OnSummonTreasureButtonClick()
    {
        if (_battleEnded) return;
        if (state != BattleState.PlayerMenu)
        {
            SetTurnText("当前阶段无法祭出法宝");
            return;
        }

        if (player == null || player.treasureEquipment == null || player.treasureEquipment.EquippedTreasures.Count == 0)
        {
            SetTurnText("未装备法宝");
            return;
        }

        SetActionMenu(false);
        SetTreasureSelectMenu(true);
        SetTreasureModeMenu(false);
        RefreshTreasureSelectLabels();
        SetTurnText("请选择要祭出的法宝");
    }

    public void OnSelectTreasureByIndex(int index)
    {
        IReadOnlyList<MagicTreasureDefinition> equipped = player != null && player.treasureEquipment != null
            ? player.treasureEquipment.EquippedTreasures
            : null;
        if (equipped == null || index < 0 || index >= equipped.Count || equipped[index] == null)
        {
            SetTurnText("该法宝无效");
            return;
        }
        if (_summonedTreasureDefs.Contains(equipped[index]))
        {
            SetTurnText("该法宝本场战斗已召唤过");
            return;
        }

        _pendingTreasureDefinition = equipped[index];
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(true);
        SetTurnText($"已选择 {_pendingTreasureDefinition.treasureName}，请选择祭出方式");
    }

    public void OnTreasureSelectBackButtonClick()
    {
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);
        SetActionMenu(true);
        SetTurnText("已返回主菜单");
    }

    public void OnDeployModeAttackButtonClick()
    {
        if (_pendingTreasureDefinition == null)
        {
            SetTurnText("请先选择法宝");
            return;
        }

        if (!TryCastArtifactControl()) return;

        _pendingTreasureMode = TreasureBattleMode.Attacking;
        _isDeployPositionSelecting = true;
        SetActionMenu(false);
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);
        SetTurnText("攻击祭出：左键选择位置，右键取消");
    }

    public void OnDeployModeGuardButtonClick()
    {
        if (_pendingTreasureDefinition == null)
        {
            SetTurnText("请先选择法宝");
            return;
        }

        if (!TryCastArtifactControl()) return;

        TreasureUnit guardTreasure = CreateTreasureUnit(_pendingTreasureDefinition, player.gridPosition, TreasureBattleMode.Guarding);
        _playerSummonedTreasures.Add(guardTreasure);
        _playerGuardTreasures.Add(guardTreasure);
        _summonedTreasureDefs.Add(_pendingTreasureDefinition);

        SetTreasureModeMenu(false);
        SetActionMenu(true);
        SetTurnText($"已护身祭出：{_pendingTreasureDefinition.treasureName}");
        _pendingTreasureDefinition = null;
    }

    public void OnTreasureModeBackButtonClick()
    {
        SetTreasureModeMenu(false);
        SetTreasureSelectMenu(true);
        SetTurnText("返回法宝选择");
    }

    public void OnTreasureMoveButtonClick()
    {
        if (_treasureMovedThisTurn)
        {
            SetTurnText("本回合法宝已移动");
            return;
        }
        BeginTreasureMoveSelection();
    }

    public void OnTreasureAttackButtonClick()
    {
        if (state != BattleState.PlayerMenu)
        {
            SetTurnText("仅可在玩家菜单阶段发动法宝攻击");
            return;
        }

        if (_treasureAttackedThisTurn)
        {
            SetTurnText("本回合法宝已攻击");
            return;
        }

        CleanupDeadObjects();
        TreasureUnit candidate = null;
        for (int i = 0; i < _playerSummonedTreasures.Count; i++)
        {
            TreasureUnit t = _playerSummonedTreasures[i];
            if (t != null && t.battleMode == TreasureBattleMode.Attacking)
            {
                candidate = t;
                break;
            }
        }

        if (candidate == null)
        {
            SetTurnText("没有可攻击的法宝");
            return;
        }

        _activeTreasure = candidate;
        _isTreasureAttackSelecting = true;
        SetActionMenu(false);
        SetTurnText("法宝攻击：左键点选目标，右键取消");
    }

    void BeginMoveSelection()
    {
        state = BattleState.PlayerMoving;
        _isMovePending = false;
        SetActionMenu(false);
        SetTurnText($"移动模式：左键选点（范围 {player.GetCurrentMoveRange()}），右键取消");
    }

    void SelectMoveTarget()
    {
        if (!TryGetMouseGrid(out Vector2Int targetPos)) return;

        int moveRange = player != null ? player.GetCurrentMoveRange() : 1;
        if (!IsWithinRange(player.gridPosition, targetPos, moveRange))
        {
            SetTurnText($"超出移动范围（{moveRange}）");
            return;
        }

        if (enemy != null && targetPos == enemy.gridPosition)
        {
            SetTurnText("角色与敌人不能同处一格");
            return;
        }

        if (_isMovePending && targetPos == _pendingMoveTarget)
        {
            ConfirmPendingMove();
            return;
        }

        _pendingMoveTarget = targetPos;
        _isMovePending = true;
        SetCharacterGridPosition(player, targetPos);
        SyncGuardTreasuresWithPlayer();
        SetTurnText($"已选中 {targetPos}，再次左键确认，右键取消");
    }

    void ConfirmPendingMove()
    {
        _isMovePending = false;
        state = BattleState.PlayerMenu;
        _playerMovedThisTurn = true;
        SetActionMenu(true);
        SetTurnText("移动已确认");
    }

    void CancelPendingMove()
    {
        if (player != null && _isMovePending)
        {
            SetCharacterGridPosition(player, _playerTurnStartPos);
            SyncGuardTreasuresWithPlayer();
        }

        _isMovePending = false;
        state = BattleState.PlayerMenu;
        SetActionMenu(true);
        SetTurnText("已取消移动");
    }

    void SelectTreasureDeployPosition()
    {
        if (_pendingTreasureDefinition == null || !TryGetMouseGrid(out Vector2Int targetPos)) return;
        if (_pendingTreasureMode == TreasureBattleMode.Attacking && player != null && !IsWithinRange(player.gridPosition, targetPos, 1))
        {
            SetTurnText("攻击祭出只能在角色周围1格");
            return;
        }

        TreasureUnit treasure = CreateTreasureUnit(_pendingTreasureDefinition, targetPos, _pendingTreasureMode);
        _playerSummonedTreasures.Add(treasure);
        _summonedTreasureDefs.Add(_pendingTreasureDefinition);

        _pendingTreasureDefinition = null;
        _isDeployPositionSelecting = false;
        SetActionMenu(true);
        SetTurnText("法宝祭出成功");
    }

    void CancelTreasureDeploy()
    {
        _isDeployPositionSelecting = false;
        _pendingTreasureDefinition = null;
        SetActionMenu(true);
        SetTurnText("已取消祭出");
    }

    void BeginTreasureMoveSelection()
    {
        if (state != BattleState.PlayerMenu)
        {
            SetTurnText("仅可在玩家菜单阶段操控法宝");
            return;
        }

        CleanupDeadObjects();
        _activeTreasure = GetFirstAttackTreasure();
        if (_activeTreasure == null)
        {
            SetTurnText("当前没有可移动的攻击法宝");
            return;
        }

        _treasureTurnStartPos = _activeTreasure.gridPosition;
        _isTreasureMoveMode = true;
        _isTreasureMovePending = false;
        SetActionMenu(false);
        SetTurnText($"法宝移动：左键选点（范围 {_activeTreasure.escapeSpeed}），右键取消");
    }

    void SelectTreasureMoveTarget()
    {
        if (_activeTreasure == null || !TryGetMouseGrid(out Vector2Int targetPos))
        {
            CancelTreasureMove();
            return;
        }

        if (!IsWithinRange(_activeTreasure.gridPosition, targetPos, _activeTreasure.escapeSpeed))
        {
            SetTurnText($"超出法宝移动范围（{_activeTreasure.escapeSpeed}）");
            return;
        }

        if (_isTreasureMovePending && targetPos == _pendingTreasureTarget)
        {
            ConfirmTreasureMove();
            return;
        }

        _pendingTreasureTarget = targetPos;
        _isTreasureMovePending = true;
        _activeTreasure.SetGridPosition(targetPos);
        SetTurnText($"法宝已选中 {targetPos}，再次左键确认，右键取消");
    }

    void ConfirmTreasureMove()
    {
        _isTreasureMoveMode = false;
        _isTreasureMovePending = false;
        _treasureMovedThisTurn = true;
        _activeTreasure = null;
        SetActionMenu(true);
        SetTurnText("法宝移动已确认");
    }

    void CancelTreasureMove()
    {
        if (_activeTreasure != null && _isTreasureMovePending)
        {
            _activeTreasure.SetGridPosition(_treasureTurnStartPos);
        }

        _isTreasureMoveMode = false;
        _isTreasureMovePending = false;
        _activeTreasure = null;
        SetActionMenu(true);
        SetTurnText("已取消法宝移动");
    }

    void SelectTreasureAttackTarget()
    {
        if (_activeTreasure == null || !TryGetMouseGrid(out Vector2Int targetPos))
        {
            CancelTreasureAttackSelection();
            return;
        }

        if (!CanTreasureAttack(_activeTreasure.gridPosition, targetPos))
        {
            SetTurnText("不满足攻击距离规则");
            return;
        }

        if (enemy != null && enemy.gridPosition == targetPos)
        {
            enemy.currentHP = Mathf.Max(0, enemy.currentHP - Mathf.Max(1, _activeTreasure.attack));
            _activeTreasure.SpendSpirit(_activeTreasure.spiritualCostPerUse);
            _treasureAttackedThisTurn = true;
            SetTurnText("法宝命中敌人");
            EndTreasureAttackSelection();
            CheckBattleEnd();
            return;
        }

        SpellUnit spell = FindSpellAt(targetPos, enemy);
        if (spell != null)
        {
            CollisionOutcome outcome = CollisionResolver.ResolveSpellVsTreasure(spell, _activeTreasure, false);
            LogCollision("法术-法宝", outcome);
            if (spell.IsExpired()) DestroySpell(spell);
            RemoveBrokenTreasure(_activeTreasure);
            _treasureAttackedThisTurn = true;
            EndTreasureAttackSelection();
            SetTurnText("已触发法术-法宝对撞");
            CheckBattleEnd();
            return;
        }

        SetTurnText("目标无效，请点选敌方角色/法宝/术法");
    }

    void CancelTreasureAttackSelection()
    {
        _isTreasureAttackSelecting = false;
        _activeTreasure = null;
        SetActionMenu(true);
        SetTurnText("已取消法宝攻击");
    }

    void EndTreasureAttackSelection()
    {
        _isTreasureAttackSelecting = false;
        _activeTreasure = null;
        SetActionMenu(true);
    }

    IEnumerator EnemyTurnRoutine()
    {
        if (_battleEnded) yield break;
        state = BattleState.EnemyTurn;
        SetTurnText("当前回合: 敌人");

        yield return new WaitForSeconds(0.4f);
        ExecuteEnemyTurnAction();
        yield return new WaitForSeconds(0.5f);

        if (_battleEnded) yield break;
        StartPlayerTurn();
    }

    void ExecuteEnemyTurnAction()
    {
        if (enemy == null || player == null) return;

        SpellDefinition toCast = GetEnemyOffensiveSpell();
        if (toCast == null)
        {
            ExecuteEnemyApproach();
            return;
        }

        int attackRange = Mathf.Clamp(enemySpellRange, 0, 1);
        EnemyAttackPlan plan;
        if (TryPlanEnemyAttack(enemy.gridPosition, enemyMoveRange, attackRange, out plan))
        {
            if (plan.moveTo != enemy.gridPosition)
            {
                SetCharacterGridPosition(enemy, plan.moveTo);
            }
            TryEnemyCastAttackSpellAt(toCast, plan.targetTreasure, plan.targetPlayer);
            return;
        }

        ExecuteEnemyApproach();
    }

    void ExecuteEnemyApproach()
    {
        if (enemy == null || player == null) return;

        Vector2Int enemyPos = enemy.gridPosition;
        Vector2Int targetPos = player.gridPosition;
        Vector2Int nextPos = StepTowards(enemyPos, targetPos, enemyMoveRange);

        if (nextPos == targetPos)
        {
            nextPos = enemyPos;
        }

        SetCharacterGridPosition(enemy, nextPos);
    }

    SpellDefinition GetEnemyOffensiveSpell()
    {
        if (enemy == null) return null;
        if (enemyAttackSpell == null && !string.IsNullOrEmpty(enemyAttackSpellPath))
        {
            enemyAttackSpell = Resources.Load<SpellDefinition>(enemyAttackSpellPath);
        }

        SpellDefinition toCast = enemyAttackSpell;
        if (toCast == null && enemy.spells != null && enemy.spells.Count > 0)
        {
            toCast = enemy.spells[0];
        }

        if (toCast == null || toCast.spellType != SpellType.Offensive) return null;
        return toCast;
    }

    void TryEnemyCastAttackSpell()
    {
        SpellDefinition toCast = GetEnemyOffensiveSpell();
        if (toCast == null) return;
        TryEnemyCastAttackSpellAt(toCast, null, player);
    }

    void TryEnemyCastAttackSpellAt(SpellDefinition toCast, TreasureUnit targetTreasure, CharacterStats targetPlayer)
    {
        if (enemy == null || toCast == null) return;
        CleanupDeadObjects();

        if (enemy.currentMP < toCast.spiritualCost)
        {
            enemy.currentMP = toCast.spiritualCost;
        }
        if (!enemy.TrySpendSpiritualPower(toCast.spiritualCost)) return;

        if (targetTreasure != null)
        {
            SpellUnit spell = CreateSpellUnit(toCast, targetTreasure.gridPosition, enemy);
            _activeSpells.Add(spell);
            bool treasureUsesDefense = targetTreasure.battleMode == TreasureBattleMode.Guarding;
            CollisionOutcome outcome = CollisionResolver.ResolveSpellVsTreasure(spell, targetTreasure, treasureUsesDefense);
            LogCollision("妖兽攻击(法术-法宝)", outcome);
            if (spell.IsExpired()) DestroySpell(spell);
            RemoveBrokenTreasure(targetTreasure);
            CheckBattleEnd();
            return;
        }

        if (targetPlayer != null && IsWithinRange(enemy.gridPosition, targetPlayer.gridPosition, Mathf.Clamp(enemySpellRange, 0, 1)))
        {
            SpellUnit spell = CreateSpellUnit(toCast, targetPlayer.gridPosition, enemy);
            _activeSpells.Add(spell);
            ResolveSpellHitPlayer(spell);
            CheckBattleEnd();
            return;
        }

        SetTurnText("敌人未找到可攻击目标");
        CheckBattleEnd();
    }

    void ResolveSpellHitPlayer(SpellUnit incomingSpell)
    {
        if (incomingSpell == null || player == null) return;

        TreasureUnit guard = GetValidGuardTreasure();
        if (guard != null)
        {
            CollisionOutcome outcome = CollisionResolver.ResolveSpellVsTreasure(incomingSpell, guard, true);
            LogCollision("护身拦截(法术-法宝)", outcome);
            if (incomingSpell.IsExpired()) DestroySpell(incomingSpell);
            RemoveBrokenTreasure(guard);
            CheckBattleEnd();
            return;
        }

        player.currentHP = Mathf.Max(0, player.currentHP - Mathf.Max(1, incomingSpell.attackPower));
        DestroySpell(incomingSpell);
        CheckBattleEnd();
    }

    TreasureUnit CreateTreasureUnit(MagicTreasureDefinition definition, Vector2Int spawnPos, TreasureBattleMode mode)
    {
        GameObject go = new GameObject($"Treasure_{definition.treasureName}");
        go.transform.localScale = Vector3.one;

        if (definition.visualPrefab != null)
        {
            GameObject visual = Instantiate(definition.visualPrefab, go.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one * treasureVisualScale;
        }
        else
        {
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetDefaultTreasureSprite();
            renderer.color = definition.visualColor;
            renderer.sortingOrder = 2;
            go.transform.localScale = Vector3.one * treasureVisualScale;
        }

        TreasureUnit treasure = go.AddComponent<TreasureUnit>();
        treasure.Initialize(player, definition, spawnPos, battleTilemap, mode);
        return treasure;
    }

    SpellUnit CreateSpellUnit(SpellDefinition definition, Vector2Int spawnPos, CharacterStats owner)
    {
        GameObject go = new GameObject($"Spell_{definition.spellName}");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = GetDefaultSpellSprite();
        renderer.color = definition.spellType == SpellType.Defensive ? new Color(0.4f, 0.7f, 1f) : new Color(1f, 0.5f, 0.4f);
        renderer.sortingOrder = 3;
        go.transform.localScale = Vector3.one * spellVisualScale;

        SpellUnit spell = go.AddComponent<SpellUnit>();
        spell.Initialize(owner, definition, spawnPos, battleTilemap);
        return spell;
    }

    void DestroySpell(SpellUnit spell)
    {
        if (spell == null) return;
        _activeSpells.Remove(spell);
        Destroy(spell.gameObject);
    }

    void RemoveBrokenTreasure(TreasureUnit treasure)
    {
        if (treasure == null) return;
        if (treasure.currentDurability > 0) return;
        _playerSummonedTreasures.Remove(treasure);
        _playerGuardTreasures.Remove(treasure);
        Destroy(treasure.gameObject);
    }

    bool TryCastArtifactControl()
    {
        if (playerArtifactControlSpell == null) return true;
        if (player.TrySpendSpiritualPower(playerArtifactControlSpell.spiritualCost)) return true;
        SetTurnText("灵力不足，无法施展御物术");
        return false;
    }

    bool CanTreasureAttack(Vector2Int from, Vector2Int to)
    {
        if (treasureAttackRule == TreasureAttackRule.Range1)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) <= 1;
        }

        return from == to;
    }

    SpellUnit FindSpellAt(Vector2Int pos, CharacterStats owner)
    {
        for (int i = 0; i < _activeSpells.Count; i++)
        {
            SpellUnit spell = _activeSpells[i];
            if (spell == null) continue;
            if (spell.owner != owner) continue;
            if (spell.gridPosition == pos) return spell;
        }
        return null;
    }

    TreasureUnit GetFirstAttackTreasure()
    {
        for (int i = 0; i < _playerSummonedTreasures.Count; i++)
        {
            TreasureUnit t = _playerSummonedTreasures[i];
            if (t != null && t.battleMode == TreasureBattleMode.Attacking) return t;
        }
        return null;
    }

    TreasureUnit GetValidGuardTreasure()
    {
        for (int i = 0; i < _playerGuardTreasures.Count; i++)
        {
            TreasureUnit t = _playerGuardTreasures[i];
            if (t != null && t.currentDurability > 0) return t;
        }
        return null;
    }

    TreasureUnit GetEnemyAttackableTreasureInRange(Vector2Int enemyPos, int range)
    {
        TreasureUnit nearest = null;
        int nearestDist = int.MaxValue;
        for (int i = 0; i < _playerSummonedTreasures.Count; i++)
        {
            TreasureUnit t = _playerSummonedTreasures[i];
            if (t == null || t.currentDurability <= 0) continue;
            int dist = Mathf.Abs(enemyPos.x - t.gridPosition.x) + Mathf.Abs(enemyPos.y - t.gridPosition.y);
            if (dist > Mathf.Max(1, range)) continue;
            if (dist < nearestDist)
            {
                nearest = t;
                nearestDist = dist;
            }
        }
        return nearest;
    }

    struct EnemyAttackPlan
    {
        public Vector2Int moveTo;
        public TreasureUnit targetTreasure;
        public CharacterStats targetPlayer;
    }

    bool TryPlanEnemyAttack(Vector2Int enemyPos, int moveRange, int attackRange, out EnemyAttackPlan plan)
    {
        plan = new EnemyAttackPlan
        {
            moveTo = enemyPos,
            targetTreasure = null,
            targetPlayer = null
        };

        TreasureUnit bestTreasure = null;
        int bestTreasureDist = int.MaxValue;
        Vector2Int bestTreasureMove = enemyPos;

        for (int i = 0; i < _playerSummonedTreasures.Count; i++)
        {
            TreasureUnit t = _playerSummonedTreasures[i];
            if (t == null || t.currentDurability <= 0) continue;
            int dist = Mathf.Abs(enemyPos.x - t.gridPosition.x) + Mathf.Abs(enemyPos.y - t.gridPosition.y);
            if (dist > moveRange + attackRange) continue;

            int desiredDist = Mathf.Max(0, dist - attackRange);
            int step = Mathf.Min(moveRange, desiredDist);
            Vector2Int moveTo = StepTowards(enemyPos, t.gridPosition, step);
            int finalDist = Mathf.Abs(moveTo.x - t.gridPosition.x) + Mathf.Abs(moveTo.y - t.gridPosition.y);
            if (finalDist > attackRange) continue;
            if (finalDist < bestTreasureDist)
            {
                bestTreasureDist = finalDist;
                bestTreasure = t;
                bestTreasureMove = moveTo;
            }
        }

        if (bestTreasure != null)
        {
            plan.moveTo = bestTreasureMove;
            plan.targetTreasure = bestTreasure;
            return true;
        }

        if (player != null)
        {
            int distToPlayer = Mathf.Abs(enemyPos.x - player.gridPosition.x) + Mathf.Abs(enemyPos.y - player.gridPosition.y);
            if (distToPlayer <= moveRange + attackRange)
            {
                int desiredDist = Mathf.Max(1, distToPlayer - attackRange);
                int step = Mathf.Min(moveRange, desiredDist);
                Vector2Int moveTo = StepTowards(enemyPos, player.gridPosition, step);
                if (moveTo == player.gridPosition) moveTo = enemyPos;
                int finalDist = Mathf.Abs(moveTo.x - player.gridPosition.x) + Mathf.Abs(moveTo.y - player.gridPosition.y);
                if (finalDist <= attackRange)
                {
                    plan.moveTo = moveTo;
                    plan.targetPlayer = player;
                    return true;
                }
            }
        }

        return false;
    }

    void SyncGuardTreasuresWithPlayer()
    {
        for (int i = 0; i < _playerGuardTreasures.Count; i++)
        {
            if (_playerGuardTreasures[i] != null) _playerGuardTreasures[i].SetGridPosition(player.gridPosition);
        }
    }

    Sprite GetDefaultTreasureSprite()
    {
        if (_defaultTreasureSprite != null) return _defaultTreasureSprite;
        _defaultTreasureSprite = BuildSolidSprite(new Color32(255, 255, 255, 255));
        return _defaultTreasureSprite;
    }

    Sprite GetDefaultSpellSprite()
    {
        if (_defaultSpellSprite != null) return _defaultSpellSprite;
        _defaultSpellSprite = BuildSolidSprite(new Color32(255, 255, 255, 220));
        return _defaultSpellSprite;
    }

    Sprite BuildSolidSprite(Color32 color)
    {
        const int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    Vector2Int StepTowards(Vector2Int from, Vector2Int to, int steps)
    {
        int remainingSteps = Mathf.Max(0, steps);
        Vector2Int current = from;

        while (remainingSteps > 0 && current != to)
        {
            int dx = to.x - current.x;
            int dy = to.y - current.y;

            if (Mathf.Abs(dx) >= Mathf.Abs(dy) && dx != 0)
            {
                current.x += dx > 0 ? 1 : -1;
            }
            else if (dy != 0)
            {
                current.y += dy > 0 ? 1 : -1;
            }
            else
            {
                break;
            }

            remainingSteps--;
        }

        return current;
    }

    bool IsWithinRange(Vector2Int from, Vector2Int to, int range)
    {
        return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y) <= Mathf.Max(1, range);
    }

    bool TryGetMouseGrid(out Vector2Int gridPos)
    {
        gridPos = Vector2Int.zero;
        if (_mainCamera == null)
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return false;
        }

        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        gridPos = WorldToGrid(mouseWorldPos);
        return true;
    }

    void SetCharacterGridPosition(CharacterStats character, Vector2Int gridPos)
    {
        if (character == null) return;
        character.gridPosition = gridPos;
        Vector3 worldPos = GridToWorldCenter(gridPos);
        character.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);
    }

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        if (battleTilemap != null)
        {
            Vector3Int cell = battleTilemap.WorldToCell(worldPos);
            return new Vector2Int(cell.x, cell.y);
        }

        return new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
    }

    Vector3 GridToWorldCenter(Vector2Int gridPos)
    {
        if (battleTilemap != null)
        {
            return battleTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
        }

        return new Vector3(gridPos.x, gridPos.y, 0f);
    }

    void CleanupDeadObjects()
    {
        _playerSummonedTreasures.RemoveAll(t => t == null);
        _playerGuardTreasures.RemoveAll(t => t == null || t.currentDurability <= 0);
        _activeSpells.RemoveAll(s => s == null || s.IsExpired());
    }

    void AutoBindCharactersIfNeeded()
    {
        if (player != null && enemy != null) return;

        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();
        foreach (CharacterStats stats in allCharacters)
        {
            if (stats == null) continue;

            string lower = stats.gameObject.name.ToLowerInvariant();
            if (player == null && lower.Contains("player"))
            {
                player = stats;
                continue;
            }

            if (enemy == null && lower.Contains("enemy"))
            {
                enemy = stats;
            }
        }

        if (allCharacters.Length > 0 && player == null) player = allCharacters[0];
        if (allCharacters.Length > 1 && enemy == null) enemy = allCharacters[1];
    }

    void AutoBindTilemapIfNeeded()
    {
        if (battleTilemap != null) return;
        battleTilemap = FindObjectOfType<Tilemap>();
    }

    void PlaceCharactersAtBoardStartPositions()
    {
        if (battleTilemap == null || player == null || enemy == null) return;

        BoundsInt bounds = battleTilemap.cellBounds;
        int minX = bounds.xMin;
        int maxX = bounds.xMax - 1;
        int minY = bounds.yMin;
        int maxY = bounds.yMax - 1;

        int spawnY = Mathf.RoundToInt(Mathf.Lerp(minY, maxY, startNormalizedY));
        int playerX = Mathf.RoundToInt(Mathf.Lerp(minX, maxX, playerStartNormalizedX));
        int enemyX = Mathf.RoundToInt(Mathf.Lerp(minX, maxX, enemyStartNormalizedX));

        if (enemyX == playerX)
        {
            enemyX = Mathf.Clamp(enemyX + 1, minX, maxX);
            if (enemyX == playerX)
            {
                playerX = Mathf.Clamp(playerX - 1, minX, maxX);
            }
        }

        player.gridPosition = new Vector2Int(playerX, spawnY);
        enemy.gridPosition = new Vector2Int(enemyX, spawnY);
    }

    void ApplyCharacterVisualScale()
    {
        if (player != null) player.transform.localScale = Vector3.one * playerVisualScale;
        if (enemy != null) enemy.transform.localScale = Vector3.one * enemyVisualScale;
    }

    void SnapCharactersToGrid()
    {
        if (player != null) SetCharacterGridPosition(player, player.gridPosition);
        if (enemy != null) SetCharacterGridPosition(enemy, enemy.gridPosition);
    }

    void SetTurnText(string content)
    {
        if (turnIndicator != null) turnIndicator.text = content;
    }

    void LogCollision(string label, CollisionOutcome outcome)
    {
        if (!enableCombatDebugLog) return;
        _lastCollisionSummary =
            $"{label} | 值 {outcome.leftValue}/{outcome.rightValue} | 灵损 {outcome.leftSpiritLoss}/{outcome.rightSpiritLoss} | 耐损 {outcome.leftDurabilityLoss}/{outcome.rightDurabilityLoss}";
        Debug.Log(
            $"[{label}] 值(L/R): {outcome.leftValue}/{outcome.rightValue}, " +
            $"灵力损失(L/R): {outcome.leftSpiritLoss}/{outcome.rightSpiritLoss}, " +
            $"耐久损失(L/R): {outcome.leftDurabilityLoss}/{outcome.rightDurabilityLoss}, " +
            $"销毁(L/R): {outcome.leftDestroyed}/{outcome.rightDestroyed}"
        );
    }

    void SetActionMenu(bool isVisible)
    {
        if (actionMenu != null) actionMenu.SetActive(isVisible);
        if (actionMenu != null)
        {
            Image bg = actionMenu.GetComponent<Image>();
            if (bg != null)
            {
                Color c = bg.color;
                c.a = 0f;
                bg.color = c;
            }
        }
    }

    void SetTreasureSelectMenu(bool isVisible)
    {
        if (treasureSelectMenu != null) treasureSelectMenu.SetActive(isVisible);
    }

    void SetTreasureModeMenu(bool isVisible)
    {
        if (treasureModeMenu != null) treasureModeMenu.SetActive(isVisible);
    }

    void RefreshTreasureSelectLabels()
    {
        if (treasureSelectMenu == null || player == null || player.treasureEquipment == null) return;
        IReadOnlyList<MagicTreasureDefinition> equipped = player.treasureEquipment.EquippedTreasures;
        SetButtonText(treasureSelectMenu.transform, "Btn_SelectTreasure0", equipped, 0);
        SetButtonText(treasureSelectMenu.transform, "Btn_SelectTreasure1", equipped, 1);
    }

    void SetButtonText(Transform parent, string buttonName, IReadOnlyList<MagicTreasureDefinition> equipped, int index)
    {
        Transform target = parent.Find(buttonName);
        if (target == null) return;
        TextMeshProUGUI tmp = target.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) return;
        if (equipped != null && index >= 0 && index < equipped.Count && equipped[index] != null)
        {
            tmp.text = equipped[index].treasureName;
        }
        else
        {
            tmp.text = "空";
        }
    }

    void TryBindExistingBattleMenus()
    {
        if (actionMenu == null) return;

        RectTransform parentRect = uiMenuRoot != null ? uiMenuRoot : actionMenu.transform.parent as RectTransform;
        if (parentRect == null) return;

        if (treasureSelectMenu == null)
        {
            Transform select = parentRect.Find("TreasureSelectMenu");
            if (select != null) treasureSelectMenu = select.gameObject;
        }

        if (treasureModeMenu == null)
        {
            Transform mode = parentRect.Find("TreasureModeMenu");
            if (mode != null) treasureModeMenu = mode.gameObject;
        }
    }

    void ValidatePersistentBattleMenus()
    {
        if (actionMenu == null)
        {
            Debug.LogWarning("BattleManager: 缺少 actionMenu 引用。");
            return;
        }

        if (actionMenu.transform.Find("Btn_TreasureAttack") == null)
        {
            Debug.LogWarning("BattleManager: actionMenu 下缺少 Btn_TreasureAttack（法宝攻击按钮）。");
        }
        if (treasureSelectMenu == null)
        {
            Debug.LogWarning("BattleManager: 缺少 treasureSelectMenu 引用。");
        }
        if (treasureModeMenu == null)
        {
            Debug.LogWarning("BattleManager: 缺少 treasureModeMenu 引用。");
        }
    }

    void BindPersistentBattleMenuCallbacks()
    {
        if (actionMenu != null) BindButtonClick(actionMenu.transform, "Btn_TreasureAttack", OnTreasureAttackButtonClick);
        if (treasureSelectMenu != null)
        {
            BindButtonClick(treasureSelectMenu.transform, "Btn_SelectTreasure0", () => OnSelectTreasureByIndex(0));
            BindButtonClick(treasureSelectMenu.transform, "Btn_SelectTreasure1", () => OnSelectTreasureByIndex(1));
            BindButtonClick(treasureSelectMenu.transform, "Btn_TreasureSelectBack", OnTreasureSelectBackButtonClick);
        }
        if (treasureModeMenu != null)
        {
            BindButtonClick(treasureModeMenu.transform, "Btn_DeployAttack", OnDeployModeAttackButtonClick);
            BindButtonClick(treasureModeMenu.transform, "Btn_DeployGuard", OnDeployModeGuardButtonClick);
            BindButtonClick(treasureModeMenu.transform, "Btn_TreasureModeBack", OnTreasureModeBackButtonClick);
        }
    }

    void ApplyPersistentMenuLayout()
    {
        if (actionMenu != null)
        {
            Transform treasureAttackBtn = actionMenu.transform.Find("Btn_TreasureAttack");
            if (treasureAttackBtn != null)
            {
                RectTransform attackRect = treasureAttackBtn.GetComponent<RectTransform>();
                if (attackRect != null) attackRect.anchoredPosition = treasureAttackButtonPos;
            }
        }

        if (treasureSelectMenu != null)
        {
            RectTransform selectRect = treasureSelectMenu.GetComponent<RectTransform>();
            if (selectRect != null) selectRect.anchoredPosition = treasureSelectMenuPos;
        }

        if (treasureModeMenu != null)
        {
            RectTransform modeRect = treasureModeMenu.GetComponent<RectTransform>();
            if (modeRect != null) modeRect.anchoredPosition = treasureModeMenuPos;
        }
    }

    void BindButtonClick(Transform parent, string buttonName, UnityEngine.Events.UnityAction callback)
    {
        Transform target = parent.Find(buttonName);
        if (target == null) return;
        Button btn = target.GetComponent<Button>();
        if (btn == null) return;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(callback);
    }

    void CheckBattleEnd()
    {
        if (_battleEnded || player == null || enemy == null) return;

        if (player.currentHP <= 0)
        {
            EndBattle("战斗结束：玩家失败");
        }
        else if (enemy.currentHP <= 0)
        {
            EndBattle("战斗结束：玩家胜利");
        }
    }

    void EndBattle(string resultText)
    {
        _battleEnded = true;
        _isMovePending = false;
        _isTreasureMoveMode = false;
        _isTreasureMovePending = false;
        _isDeployPositionSelecting = false;
        _isTreasureAttackSelecting = false;
        _activeTreasure = null;
        SetActionMenu(false);
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);
        SetTurnText(resultText);
        Debug.Log(resultText);
    }

    void OnGUI()
    {
        if (!showCombatDebugPanel) return;
        GUI.Box(new Rect(10f, 10f, 460f, 200f), "战斗调试面板");
        string playerInfo = player == null ? "玩家: 空" : $"玩家 HP:{player.currentHP}/{player.maxHP} MP:{player.currentMP}/{player.maxMP} 位置:{player.gridPosition}";
        string enemyInfo = enemy == null ? "敌人: 空" : $"敌人 HP:{enemy.currentHP}/{enemy.maxHP} MP:{enemy.currentMP}/{enemy.maxMP} 位置:{enemy.gridPosition}";
        TreasureUnit atk = GetFirstAttackTreasure();
        TreasureUnit guard = GetValidGuardTreasure();
        string atkInfo = atk == null ? "攻击法宝: 无" : $"攻击法宝 {atk.sourceDefinition?.treasureName} 灵力:{atk.currentSpiritualPower}/{atk.maxSpiritualPower} 耐久:{atk.currentDurability}/{atk.maxDurability} 位置:{atk.gridPosition}";
        string guardInfo = guard == null ? "护身法宝: 无" : $"护身法宝 {guard.sourceDefinition?.treasureName} 灵力:{guard.currentSpiritualPower}/{guard.maxSpiritualPower} 耐久:{guard.currentDurability}/{guard.maxDurability} 位置:{guard.gridPosition}";
        GUI.Label(new Rect(20f, 35f, 440f, 20f), $"状态:{state}  战斗结束:{_battleEnded}");
        GUI.Label(new Rect(20f, 55f, 440f, 20f), playerInfo);
        GUI.Label(new Rect(20f, 75f, 440f, 20f), enemyInfo);
        GUI.Label(new Rect(20f, 95f, 440f, 20f), atkInfo);
        GUI.Label(new Rect(20f, 115f, 440f, 20f), guardInfo);
        GUI.Label(new Rect(20f, 135f, 440f, 40f), $"最近对撞: {_lastCollisionSummary}");
        GUI.Label(new Rect(20f, 175f, 440f, 20f), $"已召唤法宝数: {_summonedTreasureDefs.Count}");
    }

    [ContextMenu("在Canvas下生成常驻战斗菜单UI")]
    void BuildPersistentMenusInScene()
    {
        if (actionMenu == null)
        {
            Debug.LogWarning("BattleManager: 请先绑定 actionMenu。");
            return;
        }

        RectTransform root = uiMenuRoot != null ? uiMenuRoot : actionMenu.transform.parent as RectTransform;
        if (root == null)
        {
            Debug.LogWarning("BattleManager: 未找到可用UI根节点，请设置 uiMenuRoot。");
            return;
        }

        GameObject attackBtn = CreateButtonIfMissing(actionMenu.transform, "Btn_TreasureAttack", "法宝攻击", treasureAttackButtonPos, new Vector2(160f, 30f));
        treasureSelectMenu = CreatePanelIfMissing(root, "TreasureSelectMenu", treasureSelectMenuPos, new Vector2(170f, 120f));
        treasureModeMenu = CreatePanelIfMissing(root, "TreasureModeMenu", treasureModeMenuPos, new Vector2(170f, 120f));

        CreateButtonIfMissing(treasureSelectMenu.transform, "Btn_SelectTreasure0", "法宝1", new Vector2(80f, 75f), new Vector2(160f, 30f));
        CreateButtonIfMissing(treasureSelectMenu.transform, "Btn_SelectTreasure1", "法宝2", new Vector2(80f, 45f), new Vector2(160f, 30f));
        CreateButtonIfMissing(treasureSelectMenu.transform, "Btn_TreasureSelectBack", "返回", new Vector2(80f, 15f), new Vector2(160f, 30f));

        CreateButtonIfMissing(treasureModeMenu.transform, "Btn_DeployAttack", "攻击祭出", new Vector2(80f, 75f), new Vector2(160f, 30f));
        CreateButtonIfMissing(treasureModeMenu.transform, "Btn_DeployGuard", "护身祭出", new Vector2(80f, 45f), new Vector2(160f, 30f));
        CreateButtonIfMissing(treasureModeMenu.transform, "Btn_TreasureModeBack", "返回", new Vector2(80f, 15f), new Vector2(160f, 30f));

        if (attackBtn != null && turnIndicator != null && turnIndicator.font != null)
        {
            TextMeshProUGUI btnText = attackBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.font = turnIndicator.font;
        }

        BindPersistentBattleMenuCallbacks();
        ApplyPersistentMenuLayout();
        SetTreasureSelectMenu(false);
        SetTreasureModeMenu(false);

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        EditorUtility.SetDirty(actionMenu);
        if (treasureSelectMenu != null) EditorUtility.SetDirty(treasureSelectMenu);
        if (treasureModeMenu != null) EditorUtility.SetDirty(treasureModeMenu);
#endif
    }

    GameObject CreatePanelIfMissing(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        Image bg = panel.GetComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.35f);
        return panel;
    }

    GameObject CreateButtonIfMissing(Transform parent, string name, string label, Vector2 anchoredPos, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);
        RectTransform rect = buttonGo.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        Image bg = buttonGo.GetComponent<Image>();
        bg.color = Color.white;

        GameObject textGo = new GameObject("Text (TMP)", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(buttonGo.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color32(50, 50, 50, 255);
        if (turnIndicator != null && turnIndicator.font != null) tmp.font = turnIndicator.font;

        return buttonGo;
    }

    void EnsureChineseFontSupport()
    {
        if (_runtimeChineseFont == null)
        {
            Font osFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimHei", "SimSun", "Arial Unicode MS" },
                32
            );
            if (osFont != null)
            {
                _runtimeChineseFont = TMP_FontAsset.CreateFontAsset(
                    osFont, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true
                );
                if (_runtimeChineseFont != null)
                {
                    _runtimeChineseFont.name = "RuntimeChineseDynamic";
                }
            }
        }

        if (_runtimeChineseFont == null) return;

        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null) continue;
            texts[i].font = _runtimeChineseFont;
        }

        if (turnIndicator != null)
        {
            turnIndicator.font = _runtimeChineseFont;
        }
    }


    void AutoEquipDemoTreasures()
    {
        if (!autoEquipDemoTreasures || player == null || player.treasureEquipment == null)
        {
            return;
        }

        MagicTreasureDefinition shield = Resources.Load<MagicTreasureDefinition>(shieldResourcePath);
        MagicTreasureDefinition sword = Resources.Load<MagicTreasureDefinition>(swordResourcePath);

        if (shield != null && !player.treasureEquipment.Contains(shield))
        {
            player.treasureEquipment.Equip(shield);
        }

        if (sword != null && !player.treasureEquipment.Contains(sword))
        {
            player.treasureEquipment.Equip(sword);
        }
    }

    void AutoLoadDemoSpell()
    {
        if (autoLoadArtifactControlSpell && playerArtifactControlSpell == null)
        {
            playerArtifactControlSpell = Resources.Load<SpellDefinition>(artifactControlSpellPath);
        }

        if (enemyAttackSpell == null && !string.IsNullOrEmpty(enemyAttackSpellPath))
        {
            enemyAttackSpell = Resources.Load<SpellDefinition>(enemyAttackSpellPath);
        }
    }
}
