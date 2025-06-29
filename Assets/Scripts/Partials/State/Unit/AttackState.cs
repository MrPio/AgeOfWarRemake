using UnityEngine;

namespace Partials.State.Unit
{
    public class AttackState : IState
    {
        public float LastAttack = Time.time - (float)new System.Random().NextDouble() * 0.5f;

        public override bool Equals(object obj) => obj is AttackState;
        public override int GetHashCode() => 0;

        public void Enter(Prefabs.Unit unit) =>
            unit.PlayAnimation(Prefabs.Unit.IdleTrigger);

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - LastAttack > model.AttackRate)
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