using UnityEngine;

public struct CollisionOutcome
{
    public int leftValue;
    public int rightValue;
    public int leftSpiritLoss;
    public int rightSpiritLoss;
    public int leftDurabilityLoss;
    public int rightDurabilityLoss;
    public bool leftDestroyed;
    public bool rightDestroyed;
}

public static class CollisionResolver
{
    private static ICombatAlgorithm _algorithm = new AdditionCombatAlgorithm();

    public static CollisionOutcome ResolveTreasureVsTreasure(TreasureUnit attacker, TreasureUnit defender, bool defenderUsesDefense)
    {
        CollisionOutcome outcome = new CollisionOutcome();
        if (attacker == null || defender == null) return outcome;

        int leftPower = Mathf.Max(0, attacker.attack);
        int rightPower = Mathf.Max(0, defenderUsesDefense ? defender.defense : defender.attack);

        outcome.leftValue = _algorithm.Evaluate(leftPower, attacker.spiritualCostPerUse);
        outcome.rightValue = _algorithm.Evaluate(rightPower, defender.spiritualCostPerUse);

        int attackerSpiritBeforeBase = attacker.currentSpiritualPower;
        int defenderSpiritBeforeBase = defender.currentSpiritualPower;
        attacker.SpendSpirit(attacker.spiritualCostPerUse);
        defender.SpendSpirit(defender.spiritualCostPerUse);
        outcome.leftSpiritLoss += attackerSpiritBeforeBase - attacker.currentSpiritualPower;
        outcome.rightSpiritLoss += defenderSpiritBeforeBase - defender.currentSpiritualPower;

        int diff = Mathf.Abs(outcome.leftValue - outcome.rightValue);
        if (diff <= 0)
        {
            outcome.leftDestroyed = attacker.currentDurability <= 0;
            outcome.rightDestroyed = defender.currentDurability <= 0;
            return outcome;
        }

        if (outcome.leftValue > outcome.rightValue)
        {
            ApplyDiff(defender, diff, rightPower, ref outcome.rightSpiritLoss, ref outcome.rightDurabilityLoss, ref outcome.rightDestroyed);
        }
        else
        {
            ApplyDiff(attacker, diff, leftPower, ref outcome.leftSpiritLoss, ref outcome.leftDurabilityLoss, ref outcome.leftDestroyed);
        }

        return outcome;
    }

    public static CollisionOutcome ResolveSpellVsSpell(SpellUnit left, SpellUnit right)
    {
        CollisionOutcome outcome = new CollisionOutcome();
        if (left == null || right == null) return outcome;

        int leftPower = left.GetCombatPower();
        int rightPower = right.GetCombatPower();
        outcome.leftValue = _algorithm.Evaluate(leftPower, left.currentSpiritualPower);
        outcome.rightValue = _algorithm.Evaluate(rightPower, right.currentSpiritualPower);

        if (outcome.leftValue == outcome.rightValue)
        {
            outcome.leftSpiritLoss = left.currentSpiritualPower;
            outcome.rightSpiritLoss = right.currentSpiritualPower;
            left.currentSpiritualPower = 0;
            right.currentSpiritualPower = 0;
            outcome.leftDestroyed = true;
            outcome.rightDestroyed = true;
            return outcome;
        }

        SpellUnit winner = outcome.leftValue > outcome.rightValue ? left : right;
        SpellUnit loser = winner == left ? right : left;
        int loseValue = winner == left ? outcome.rightValue : outcome.leftValue;

        int loserSpiritBefore = loser.currentSpiritualPower;
        loser.currentSpiritualPower = 0;
        int spiritLoss = _algorithm.RestoreSpiritCost(loseValue, winner.GetCombatPower());
        int winnerSpiritBefore = winner.currentSpiritualPower;
        winner.currentSpiritualPower = Mathf.Max(0, winner.currentSpiritualPower - spiritLoss);
        int winnerConsumed = winnerSpiritBefore - winner.currentSpiritualPower;
        int loserConsumed = loserSpiritBefore;

        if (winner == left)
        {
            outcome.leftSpiritLoss = winnerConsumed;
            outcome.rightSpiritLoss = loserConsumed;
        }
        else
        {
            outcome.leftSpiritLoss = loserConsumed;
            outcome.rightSpiritLoss = winnerConsumed;
        }

        outcome.leftDestroyed = left.currentSpiritualPower <= 0;
        outcome.rightDestroyed = right.currentSpiritualPower <= 0;
        return outcome;
    }

    public static CollisionOutcome ResolveSpellVsTreasure(SpellUnit spell, TreasureUnit treasure, bool treasureUsesDefense)
    {
        CollisionOutcome outcome = new CollisionOutcome();
        if (spell == null || treasure == null) return outcome;

        int spellPower = spell.GetCombatPower();
        int treasurePower = Mathf.Max(0, treasureUsesDefense ? treasure.defense : treasure.attack);
        outcome.leftValue = _algorithm.Evaluate(spellPower, spell.currentSpiritualPower);
        outcome.rightValue = _algorithm.Evaluate(treasurePower, treasure.spiritualCostPerUse);

        int treasureSpiritBeforeBase = treasure.currentSpiritualPower;
        treasure.SpendSpirit(treasure.spiritualCostPerUse);
        outcome.rightSpiritLoss += treasureSpiritBeforeBase - treasure.currentSpiritualPower;

        int diff = Mathf.Abs(outcome.leftValue - outcome.rightValue);
        if (outcome.leftValue >= outcome.rightValue)
        {
            ApplyDiff(treasure, diff, treasurePower, ref outcome.rightSpiritLoss, ref outcome.rightDurabilityLoss, ref outcome.rightDestroyed);
            outcome.leftSpiritLoss = spell.currentSpiritualPower;
            spell.currentSpiritualPower = 0;
            outcome.leftDestroyed = true;
        }
        else
        {
            int loss = _algorithm.RestoreSpiritCost(diff, spellPower);
            int spellSpiritBefore = spell.currentSpiritualPower;
            spell.currentSpiritualPower = Mathf.Max(0, spell.currentSpiritualPower - loss);
            outcome.leftSpiritLoss = spellSpiritBefore - spell.currentSpiritualPower;
            outcome.leftDestroyed = spell.currentSpiritualPower <= 0;
            outcome.rightDestroyed = treasure.currentDurability <= 0;
        }

        return outcome;
    }

    private static void ApplyDiff(TreasureUnit target, int diffDamage, int power, ref int spiritLoss, ref int durabilityLoss, ref bool destroyed)
    {
        int spiritNeed = _algorithm.RestoreSpiritCost(diffDamage, power);
        int before = target.currentSpiritualPower;
        target.SpendSpirit(spiritNeed);
        int consumed = before - target.currentSpiritualPower;
        spiritLoss += consumed;
        if (consumed < spiritNeed)
        {
            int lack = spiritNeed - consumed;
            target.currentDurability = Mathf.Max(0, target.currentDurability - lack);
            durabilityLoss += lack;
        }
        destroyed = target.currentDurability <= 0;
    }
}
