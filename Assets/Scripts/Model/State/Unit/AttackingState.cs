using UnityEngine;

namespace Model.State.Unit
{
    public class AttackingState:IState
    {
        private float _lastAttack;
        public void Enter(Prefabs.Unit unit)
        {
            unit.animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            unit.animator.SetTrigger(Prefabs.Unit.IdleTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model;
            if (Time.time - _lastAttack > model.AttackRate)
            {
                _lastAttack = Time.time;
                // TODO: the animation trigger the damage event.
                unit.animator.ResetTrigger(Prefabs.Unit.AttackTrigger);
                unit.animator.SetTrigger(Prefabs.Unit.AttackTrigger);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}