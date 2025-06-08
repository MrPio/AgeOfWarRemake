using Unity.Netcode;

namespace Partials.State.Unit
{
    public class DyingState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            unit.Animator.ResetTrigger(Prefabs.Unit.DieTrigger);
            unit.Animator.SetTrigger(Prefabs.Unit.DieTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}