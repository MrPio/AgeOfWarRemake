using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class AttackState : IState
    {
        private float _lastAttack = Time.time - (float)new System.Random().NextDouble() * 0.5f;

        public void Enter(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;
        }

        public void Update(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - _lastAttack > model.AttackRate)
            {
                _lastAttack = Time.time;
                unit.Animator.SetTrigger(Prefabs.Unit.AttackTrigger);
                unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;
                unit.PlayingAnimation.Value = Prefabs.Unit.AttackTrigger;
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}