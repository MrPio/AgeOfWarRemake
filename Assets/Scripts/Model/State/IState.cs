namespace Model.State
{
    public interface IState
    {
        void Enter(Prefabs.Unit unit);
        void Update(Prefabs.Unit unit);
        void Exit(Prefabs.Unit unit);
    }
}