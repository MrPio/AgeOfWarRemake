namespace Partials.State
{
    /// <summary>
    /// Server-only State Design Pattern.
    /// The server-only constraint is delegated to the client code.
    /// From the perspective of a singleton game state that can only be edited by the server,
    /// the state of the game entities should only produce side effects on the server-side versions.
    /// </summary>
    /// <remarks>
    /// According to the pattern, changing the <c>IState</c> should be done as:
    /// <code>
    ///     state?.Exit(this);
    ///     state = newState;
    ///     state?.Enter(this);
    /// </code>
    /// The <c>Update()</c> method should be invoked in the <c>Update()</c> method of the game entity.
    /// </remarks>
    public interface IState
    {
        void Enter(Prefabs.Unit unit);
        void Update(Prefabs.Unit unit);
        void Exit(Prefabs.Unit unit);
    }
}