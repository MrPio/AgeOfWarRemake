namespace Partials.State.Unit
{
    public class DieState : IState
    {
        public override bool Equals(object obj) => obj is DieState;
        public override int GetHashCode() => 0;

        public void Enter(Prefabs.Unit unit)
        {
            // Free waiting units before running animation
            (unit.IsLeft ? unit.Sm.GameManager.UnitsAlly : unit.Sm.GameManager.UnitsEnemy).Remove(unit);
            unit.PlayAnimation(Prefabs.Unit.DieTrigger);
            unit.Sm.musicManager.PlayDie(unit.AllyBase.Model.Value.Level, unit.Model.Value.Level);
            unit.Die();
            
            // Add money to the enemy
            var enemyBaseModel = unit.EnemyBase.Model.Value;
            enemyBaseModel.Money += unit.Model.Value.Revenue;
            unit.EnemyBase.Model.Value = enemyBaseModel;
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}