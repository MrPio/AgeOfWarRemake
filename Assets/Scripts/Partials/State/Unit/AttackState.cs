using Managers;
using Managers.Statics;
using UnityEngine;

namespace Partials.State.Unit
{
    public class AttackState : IState
    {
        public float LastAttack;

        public override bool Equals(object obj) => obj is AttackState;
        public override int GetHashCode() => 0;

        public void Enter(Prefabs.Unit unit)
        {
            var model = unit.Model.Value;
            unit.PlayAnimation(Prefabs.Unit.IdleTrigger);
            if (DataManager.IsMultiplayer)
            {
                // Add up to 2 seconds of delay
                var toWait = (float)new System.Random().NextDouble() * 1.5f;
                LastAttack = Time.time - model.AttackDuration + toWait;
            }
            else
                LastAttack = 0;
        }

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            var attackDuration = model.AttackDuration;
            // Check if this unit benefits from the speed powerup
            if (unit.Sm.PowerupManager.SpeedPowerupCollectedTime.TryGetValue(unit.Owner,
                    out var speedPowerupCollectedTime))
                if (Time.time - speedPowerupCollectedTime < PowerupManager.SpeedPowerupDuration)
                    attackDuration *= 0.75f;

            if (Time.time - LastAttack > attackDuration)
            {
                LastAttack = Time.time;
                unit.PlayAnimation(Prefabs.Unit.AttackTrigger);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}