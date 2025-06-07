namespace Model.State.Unit
{
    public class IdleState:IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            unit.animator.ResetTrigger(Prefabs.Unit.IdleTrigger);
            unit.animator.SetTrigger(Prefabs.Unit.IdleTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }

    }
}