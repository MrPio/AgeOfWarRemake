namespace Partials.State.Unit
{
    public class IdleState:IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            unit.Animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            unit.Animator.SetTrigger(Prefabs.Unit.IdleTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }

    }
}