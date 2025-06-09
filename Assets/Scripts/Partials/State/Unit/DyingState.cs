using Managers;
using Unity.Netcode;

namespace Partials.State.Unit
{
    public class DyingState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            // Free waiting units before running animation
            // unit.Observable.Notify("death");
            (unit.IsOwner ? unit.Sm.GameManager.UnitsAlly : unit.Sm.GameManager.UnitsEnemy).Remove(unit);


            if (!unit.IsOwner) return;

            // unit.Animator.ResetTrigger(Prefabs.Unit.DieTrigger);
            // SceneManager.Instance.logger.Log($"{(unit.IsOwner ? "Ally" : "Enemy")}=DIE");
            unit.Animator.SetTrigger(Prefabs.Unit.DieTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.DieTrigger;

            // unit.Animator.CrossFade(Prefabs.Unit.DieTrigger, 0.1f);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}