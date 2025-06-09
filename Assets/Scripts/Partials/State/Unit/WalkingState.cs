using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class WalkingState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;

            // unit.Animator.ResetTrigger(Prefabs.Unit.WalkTrigger);
            // SceneManager.Instance.logger.Log($"{(unit.IsOwner ? "Ally" : "Enemy")}=WALK");
            unit.Animator.SetTrigger(Prefabs.Unit.WalkTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.WalkTrigger;

            // unit.Animator.CrossFade(Prefabs.Unit.WalkTrigger, 0.2f);
        }

        public void Update(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;

            // Owner only
            var model = unit.Model.Value;
            if (!model.HasValue) return;
            var dx = model.MoveSpeed * Time.deltaTime;
            unit.DeltaX.Value += dx;
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}