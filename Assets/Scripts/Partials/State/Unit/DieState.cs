using System.Collections.Generic;
using Managers.Statics;
using Partials.AI;

namespace Partials.State.Unit
{
    public class DieState : IState
    {
        private readonly Dictionary<GameMode, float> expRevenueAlly = new()
        {
            { GameMode.Singleplayer, 0.5f },
            { GameMode.Multiplayer, 1f },
        };

        private readonly Dictionary<GameMode, float> expRevenueEnemy = new()
        {
            { GameMode.Singleplayer, 2f },
            { GameMode.Multiplayer, 1.85f },
        };

        public override bool Equals(object obj) => obj is DieState;
        public override int GetHashCode() => 0;

        public void Enter(Prefabs.Unit unit)
        {
            // Free waiting units before running animation
            (unit.IsLeft ? unit.Sm.GameManager.UnitsAlly : unit.Sm.GameManager.UnitsEnemy).Remove(unit);
            unit.PlayAnimation(Prefabs.Unit.DieTrigger);
            unit.PlaySoundRpc(1);
            unit.DelayedDestroy();
            unit.BoxCollider.enabled = false; // Prevent collision with bullets and collecting powerups

            // Add money/exp to the enemy
            var enemyBaseModel = unit.EnemyBase.Model.Value;
            if (unit.EnemyBase.IsBot.Value)
                enemyBaseModel.Money += (int)(unit.Model.Value.Revenue * BotAI.BotIncomeMultiplier);
            else
                enemyBaseModel.Money += unit.Model.Value.Revenue;
            enemyBaseModel.Exp += (int)(unit.Model.Value.Revenue * expRevenueEnemy[DataManager.GameMode]);
            unit.EnemyBase.Model.Value = enemyBaseModel;

            // Add exp to the ally
            var allyBaseModel = unit.AllyBase.Model.Value;
            allyBaseModel.Exp +=
                (int)(unit.Model.Value.Revenue * expRevenueAlly[DataManager.GameMode]);
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