public interface ICombatAlgorithm
{
    int Evaluate(int power, int spiritValue);
    int RestoreSpiritCost(int incomingDamage, int power);
}
