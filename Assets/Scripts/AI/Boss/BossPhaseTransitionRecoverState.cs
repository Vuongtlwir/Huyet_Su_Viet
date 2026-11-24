using BossAI.Core;

namespace BossAI.Boss
{
    public class BossPhaseTransitionRecoverState : IFsmState<BossContext>
    {
        private float timer;

        public string StateName => "PhaseTransitionRecover";

        public void OnEnter(BossContext ctx)
        {
            ctx.isPhaseTransitionRecover = true;
            ctx.isInvulnerable = false;
            ctx.agent.isStopped = true;
            timer = ctx.phaseData.phaseTransitionRecoverDuration;
        }

        public void Tick(BossContext ctx)
        {
            timer -= ctx.deltaTime;

            if (timer <= 0f)
            {
                ctx.RequestState(
                    BossStateId.Idle,
                    BossTransitionPriority.Normal);
            }
        }

        public void OnExit(BossContext ctx)
        {
            ctx.isPhaseTransitionRecover = false;
        }
    }
}
