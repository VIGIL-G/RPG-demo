using UnityEngine;

public class AdditionCombatAlgorithm : ICombatAlgorithm
{
    public int Evaluate(int power, int spiritValue)
    {
        return Mathf.Max(0, power) + Mathf.Max(0, spiritValue);
    }

    public int RestoreSpiritCost(int incomingDamage, int power)
    {
        return Mathf.Max(0, incomingDamage - Mathf.Max(0, power));
    }
}
