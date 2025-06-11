using Managers;
using Unity.Netcode;

namespace Partials.State.Unit
{
    public class DieState : IState
    {
        public void Enter(Prefabs.Unit unit)
        {
            // Free waiting units before running animation
            (unit.IsOwner ? unit.Sm.GameManager.UnitsAlly : unit.Sm.GameManager.UnitsEnemy).Remove(unit);

            if (!unit.IsOwner) return;
            unit.Animator.SetTrigger(Prefabs.Unit.DieTrigger);
            unit.PlayingAnimation.Value = Prefabs.Unit.DieTrigger;
        }

        public void Update(Prefabs.Unit unit)
        {
        }

        public void Exit(Prefabs.Unit unit)
        {
        }
    }
}