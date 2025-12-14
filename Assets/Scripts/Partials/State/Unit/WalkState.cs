using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class WalkState : IState
    {
        public readonly bool Shooting;
        public float LastShoot = Time.time + 0.25f;
        private float _xEnd, _xStart, _basesDistance;

        public WalkState(bool shooting)
        {
            Shooting = shooting;
        }

        public override bool Equals(object obj) =>
            obj is WalkState state && Shooting == state.Shooting;

        public override int GetHashCode() => Shooting.GetHashCode();

        public void Enter(Prefabs.Unit unit)
        {
            unit.PlayAnimation(Prefabs.Unit.WalkTrigger);

            // Initialize base distances
            _xStart = unit.AllyBase.BasePrefab.unitSpawnPointX.position.x;
            _xEnd = unit.EnemyBase.BasePrefab.unitSpawnPointX.position.x;
            _basesDistance = Mathf.Abs(_xEnd - _xStart);
            unit.IsWalking.Value = true;
        }

        public void Update(Prefabs.Unit unit)
        {
            var model = unit.Model.Value;
            if (!model.HasValue) return;
            if (unit.Sm.GameManager.IsGamePaused) return;

            var speed = model.MoveSpeed;
            var shootRate = model.ShootRate;

            // Check if this unit benefits from the speed powerup
            if (unit.Sm.PowerupManager.SpeedPowerupCollectedTime.TryGetValue(unit.Owner,
                    out var speedPowerupCollectedTime))
                if (Time.time - speedPowerupCollectedTime < PowerupManager.SpeedPowerupDuration)
                {
                    speed *= 1.4f;
                    shootRate *= 1.4f;
                }

            #region Walking

            // It doesn't matter if the NetVar is updated more frequently than the tick rate.
            // The doc states that "during each network tick, changes to NetworkVariables are collected".
            var dx = speed * Time.deltaTime / _basesDistance;
            unit.Movement.X.Value += dx;

            // The server sets the position immediately
            var x = Mathf.Lerp(_xStart, _xEnd, unit.Movement.X.Value);
            unit.transform.position = new Vector3(x, 0, unit.Movement.zPos);

            #endregion

            #region Shooting

            if (Shooting && Time.time - LastShoot > 1 / shootRate)
            {
                LastShoot = Time.time;
                unit.PlayAnimation(Prefabs.Unit.ShootTrigger);
            }

            #endregion
        }

        public void Exit(Prefabs.Unit unit)
        {
            unit.IsWalking.Value = false;
        }
    }
}