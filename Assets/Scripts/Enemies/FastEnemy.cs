namespace ATM.Gameplay
{
    public sealed class FastEnemy : Enemy
    {
        protected override CharacterStats CreateDefaultStats() =>
            new(maxHP: 32f, moveSpeed: 3.6f, attackSpeed: 1.25f, attackDamage: 9f,
                defense: 0f, projectileRange: 12f, projectileCount: 1);

        protected override void Act() => ChaseAndAttack();
    }
}
