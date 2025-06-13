using Managers;
using Unity.Netcode;

namespace Partials.State.Unit
{
    public class DieState : IState
    {
        public override bool Equals(object obj) => obj is DieState;
        public override int GetHashCode() => 0;

        public void Enter(Prefabs.Unit unit)
        {
            // Free waiting units before running animation
            (unit.IsOwner ? unit.Sm.GameManager.UnitsAlly : unit.Sm.GameManager.UnitsEnemy).Remove(unit);
            unit.PlayAnimation(Prefabs.Unit.DieTrigger);
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}