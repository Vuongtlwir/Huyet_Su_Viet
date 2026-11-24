using BossAI.Core;
using UnityEngine;

namespace BossAI.Boss
{
    public class BossPhaseTransitionState : IFsmState<BossContext>
    {
        private float timer;
        private BossPhase nextPhase;

        public string StateName => "PhaseTransition";

        public void OnEnter(BossContext ctx)
        {
            ctx.isInvulnerable = true;
            ctx.agent.isStopped = true;
            ctx.currentAttack = null;

            BossPhaseData data = ctx.phaseData;
            timer = data.phaseTransitionDuration;
            nextPhase = ctx.currentPhase == BossPhase.Phase1
                ? BossPhase.Phase2
                : BossPhase.Enrage;
        }

        public void Tick(BossContext ctx)
        {
            timer -= ctx.deltaTime;

            if (timer > 0f)
                return;

            BossPhaseData nextData = ctx.controller.GetPhaseData(nextPhase);


            ctx.currentPhase = nextPhase;
            ctx.phaseData = nextData;

            ctx.controller.ResetCooldownsForNewPhase();
            ctx.isInvulnerable = false;

            ctx.RequestState(
                BossStateId.PhaseTransitionRecover,
                BossTransitionPriority.Normal);
        }

        public void OnExit(BossContext ctx)
        {
            ctx.isInvulnerable = false;
        }
    }
}
