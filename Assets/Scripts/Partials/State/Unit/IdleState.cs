using Managers;

namespace Partials.State.Unit
{
    public class IdleState:IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            if(!unit.IsOwner) return;

            // unit.Animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            // SceneManager.Instance.logger.Log($"{(unit.IsOwner ? "Ally" : "Enemy")}=IDLE");
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;

            // unit.Animator.CrossFade(Prefabs.Unit.IdleTrigger, 0.2f);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }

    }
}