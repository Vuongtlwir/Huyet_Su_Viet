namespace BossAI.Core
{
    public interface IFsmState<TContext>
    {
        string StateName { get; }

        void OnEnter(TContext context);

        void Tick(TContext context);

        void OnExit(TContext context);
    }
}
