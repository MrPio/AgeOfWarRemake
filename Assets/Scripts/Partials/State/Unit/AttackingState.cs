using UnityEngine;

namespace Partials.State.Unit
{
    public class AttackingState : IState
    {
        private float _lastAttack;

        public void Enter(Prefabs.Unit unit)
        {
            unit.Animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
            // FIXME: This should be made in a network variable change
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - _lastAttack > model.AttackRate)
            {
                _lastAttack = Time.time;
                // TODO: the animation trigger the damage event. Only if isOwner
                unit.Animator.ResetTrigger(Prefabs.Unit.AttackTrigger);
                unit.Animator.SetTrigger(Prefabs.Unit.AttackTrigger);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}