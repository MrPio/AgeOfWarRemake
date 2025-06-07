using UnityEngine;

namespace Model.State.Unit
{
    public class AttackingState:IState
    {
        private float _lastAttack;
        public void Enter(Prefabs.Unit unit)
        {
            unit.Animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model;
            if (Time.time - _lastAttack > model.attackRate)
            {
                _lastAttack = Time.time;
                // TODO: the animation trigger the damage event.
                unit.Animator.ResetTrigger(Prefabs.Unit.AttackTrigger);
                unit.Animator.SetTrigger(Prefabs.Unit.AttackTrigger);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}