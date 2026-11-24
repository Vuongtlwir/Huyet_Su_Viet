using BossAI.Core;

namespace BossAI.Boss
{
    public class BossStunnedState : IFsmState<BossContext>
    {
        private float timer;

        public string StateName => "Stunned";

        public void OnEnter(BossContext ctx)
        {
            ctx.isStunned = true;
            ctx.agent.isStopped = true;
            timer = ctx.stunDuration;
        }

        public void Tick(BossContext ctx)
        {
            timer -= ctx.deltaTime;

            if (timer > 0f)
                return;

            ctx.isStunned = false;

            if (ctx.controller.CanPhaseTransition())
            {
                ctx.RequestState(
                    BossStateId.PhaseTransition,
                    BossTransitionPriority.PhaseTransition);
                return;
            }

            ctx.RequestState(
                BossStateId.Idle,
                BossTransitionPriority.Normal);
        }

        public void OnExit(BossContext ctx)
        {
            ctx.isStunned = false;
        }
    }
}
