using UnityEngine;

namespace Model.State.Unit
{
    public class WalkingState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            unit.animator.ResetTrigger(Prefabs.Unit.WalkTrigger);
            unit.animator.SetTrigger(Prefabs.Unit.WalkTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
            var dx = unit.Model.MoveSpeed * Time.deltaTime;
            var dir = unit.IsEnemy ? Vector3.left : Vector3.right;
            unit.transform.Translate(dir * dx);
        }

        public void Exit(Prefabs.Unit unit)
        {
        }

    }
}