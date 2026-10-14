namespace ExpiryGame
{
    public readonly struct FoodStats
    {
        public readonly float Heal;
        public readonly float Damage;
        public readonly int MaxDurability;
        public readonly float RottenHeal;
        public readonly float RottenDamage;

        public FoodStats(float heal, float damage, int maxDurability, float rottenHeal, float rottenDamage)
        {
            Heal = heal;
            Damage = damage;
            MaxDurability = maxDurability;
            RottenHeal = rottenHeal;
            RottenDamage = rottenDamage;
        }
    }
}
