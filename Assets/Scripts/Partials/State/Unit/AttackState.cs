using Managers;
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
                var toWait = (float)new System.Random().NextDouble() * 2f;
                LastAttack = Time.time - model.AttackDuration + toWait;
            }
            else
                LastAttack = 1;
        }

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - LastAttack > model.AttackDuration)
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