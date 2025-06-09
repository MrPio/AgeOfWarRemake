using Managers;
using UnityEngine;

namespace Partials.State.Unit
{
    public class AttackingState : IState
    {
        private float _lastAttack = Time.time - (float)new System.Random().NextDouble() * 0.75f;

        public void Enter(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;
            // unit.Animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            // SceneManager.Instance.logger.Log($"{(unit.IsOwner ? "Ally" : "Enemy")}=IDLE");
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;

            // unit.Animator.CrossFade(Prefabs.Unit.IdleTrigger, 0.2f);

            // Prevent "coffin-exchange"
            // var model = unit.Model.Value;
            // _lastAttack = Time.time - model.AttackRate +
            //               (float)new System.Random().NextDouble() * model.AttackRate * MaxRandomAdvance;
        }

        public void Update(Prefabs.Unit unit)
        {
            if (!unit.IsOwner) return;
            var model = unit.Model.Value;
            if (!model.HasValue) return;

            if (Time.time - _lastAttack > model.AttackRate)
            {
                _lastAttack = Time.time;
                // unit.Animator.ResetTrigger(Prefabs.Unit.AttackTrigger);
                // SceneManager.Instance.logger.Log($"{(unit.IsOwner ? "Ally" : "Enemy")}=ATTACK");
                unit.Animator.SetTrigger(Prefabs.Unit.AttackTrigger);
                unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;
                unit.PlayingAnimation.Value = Prefabs.Unit.AttackTrigger;
                // unit.Animator.CrossFade(Prefabs.Unit.AttackTrigger, 0.2f);
            }
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}