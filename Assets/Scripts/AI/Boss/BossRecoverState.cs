using BossAI.Core;

namespace BossAI.Boss
{
    public class BossRecoverState : IFsmState<BossContext>
    {
        private float timer;

        public string StateName => "Recover";

        public void OnEnter(BossContext ctx)
        {
            ctx.agent.isStopped = true;

            BossSkillData attack = ctx.currentAttack;

            timer = attack != null
                ? attack.recover
                : ctx.phaseData.recoverFallback;
        }

        public void Tick(BossContext ctx)
        {
            ctx.controller.FaceTarget();

            timer -= ctx.deltaTime;

            if (timer > 0f)
                return;

            BossPhaseData data = ctx.phaseData;

            if (ctx.distanceToTarget < data.dMelee &&
                ctx.teleportCooldown <= 0f &&
                !ctx.lastActionWasTeleport)
            {
                ctx.RequestState(
                    BossStateId.Teleport,
                    BossTransitionPriority.Normal);
                return;
            }

            if (ctx.distanceToTarget < data.dMelee)
            {
                ctx.RequestState(
                    BossStateId.Retreat,
                    BossTransitionPriority.Normal);
                return;
            }

            ctx.RequestState(
                BossStateId.Idle,
                BossTransitionPriority.Normal);
        }

        public void OnExit(BossContext ctx)
        {
            ctx.currentAttack = null;
        }
    }
}
