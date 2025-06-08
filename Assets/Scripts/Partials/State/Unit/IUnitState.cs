namespace Partials.State.Unit
{
    public interface IUnitState
    {
        void OnEnemyCollide(Prefabs.Unit unit, Prefabs.Unit enemy);
    }
}