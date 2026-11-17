namespace ATM.Gameplay
{
    public sealed class BossEnemy : Enemy
    {
        protected override CharacterStats CreateDefaultStats() =>
            new(maxHP: 230f, moveSpeed: 1.6f, attackSpeed: .85f, attackDamage: 20f,
                defense: 3f, projectileRange: 12f, projectileCount: 1);

        protected override void Act() => ChaseAndAttack();
    }
}
