using Managers;

namespace Partials.State.Unit
{
    public class IdleState:IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            if(!unit.IsOwner) return;
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.IdleTrigger;
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }

    }
}