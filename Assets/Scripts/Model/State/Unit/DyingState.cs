namespace Model.State.Unit
{
    public class DyingState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            unit.animator.ResetTrigger(Prefabs.Unit.DieTrigger);
            unit.animator.SetTrigger(Prefabs.Unit.DieTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}