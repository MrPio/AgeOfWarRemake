using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class WalkState : IState
    {
        public bool Shooting;
        public float LastShoot = Time.time + 0.25f;

        public WalkState(bool shooting)
        {
            Shooting = shooting;
        }

        public void Enter(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;
            unit.Animator.SetTrigger(Prefabs.Unit.WalkTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.WalkTrigger;
        }

        public void Update(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;

            // Walking
            var model = unit.Model.Value;
            if (!model.HasValue) return;
            var dx = model.MoveSpeed * Time.deltaTime;
            unit.DeltaX.Value += dx;

            // Shooting
            if (Shooting)
            {
                if (Time.time - LastShoot > 1/ model.ShootRate)
                {
                    LastShoot = Time.time;
                    unit.Animator.SetTrigger(Prefabs.Unit.ShootTrigger);
                    unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;
                    unit.PlayingAnimation.Value = Prefabs.Unit.ShootTrigger;
                }
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}