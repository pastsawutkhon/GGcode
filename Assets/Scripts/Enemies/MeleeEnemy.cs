namespace ATM.Gameplay
{
    public sealed class MeleeEnemy : Enemy
    {
        protected override CharacterStats CreateDefaultStats() =>
            new(maxHP: 48f, moveSpeed: 2.35f, attackSpeed: 1.1f, attackDamage: 12f,
                defense: 0f, projectileRange: 12f, projectileCount: 1);

        protected override void Act() => ChaseAndAttack();
    }
}
