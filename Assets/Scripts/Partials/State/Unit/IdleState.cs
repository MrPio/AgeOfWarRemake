using System;
using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class IdleState : IState
    {
        public readonly bool Shooting;
        public float LastShoot = Time.time + 0.25f;

        public IdleState(bool shooting)
        {
            Shooting = shooting;
        }

        public override bool Equals(object obj) => obj is IdleState state && Shooting == state.Shooting;
        public override int GetHashCode() => Shooting.GetHashCode();

        public void Enter(Prefabs.Unit unit) =>
            unit.PlayAnimation(Prefabs.Unit.IdleTrigger);

        public void Update(Prefabs.Unit unit)
        {
            if (!Shooting) return;
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - LastShoot > 1 / model.ShootRate)
            {
                LastShoot = Time.time;
                unit.PlayAnimation(Prefabs.Unit.ShootTrigger);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}