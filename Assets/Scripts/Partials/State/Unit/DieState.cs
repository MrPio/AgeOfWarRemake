using Partials.AI;

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
            unit.Sm.musicManager.PlayDie(unit.AllyBase.Model.Value.Age, unit.Model.Value.Level);
            unit.DelayedDestroy();

            // Add money to the enemy
            var enemyBaseModel = unit.EnemyBase.Model.Value;
            if (unit.EnemyBase.IsBot.Value)
                enemyBaseModel.Money += (int)(unit.Model.Value.Revenue * BotAI.BotIncomeMultiplier);
            else
                enemyBaseModel.Money += unit.Model.Value.Revenue;
            enemyBaseModel.Exp += unit.Model.Value.Revenue * 2;
            unit.EnemyBase.Model.Value = enemyBaseModel;

            var allyBaseModel = unit.AllyBase.Model.Value;
            allyBaseModel.Exp += (int)(unit.Model.Value.Revenue * 0.5);
            unit.AllyBase.Model.Value = allyBaseModel;
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}