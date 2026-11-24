using BossAI.Core;
using UnityEngine;

namespace BossAI.Boss
{
    public class BossChaseState : IFsmState<BossContext>
    {
        private const float MIN_DIRECTION_SQR = 0.0001f;
        private const float STRAFE_STOP_DISTANCE = 0.5f;

        private float strafeTimer;
        private int strafeSide = 1;

        public string StateName => "Chase";

        public void OnEnter(BossContext ctx)
        {
            ctx.agent.isStopped = false;
            ctx.agent.stoppingDistance = STRAFE_STOP_DISTANCE;

            strafeSide = -strafeSide;
            strafeTimer = Mathf.Max(
                0.01f,
                ctx.phaseData.chaseStrafeFlipInterval);
        }

        public void Tick(BossContext ctx)
        {
            if (ctx.target == null || ctx.phaseData == null)
            {
                ctx.agent.isStopped = true;
                return;
            }

            ctx.controller.FaceTarget();

            BossPhaseData data = ctx.phaseData;
            BossSkillData skill = ctx.controller.SelectAttack(ctx);

            if (skill != null)
            {
                ctx.currentAttack = skill;
                ctx.RequestState(
                    BossStateId.Attack,
                    BossTransitionPriority.Normal);
                return;
            }

            if (ctx.insideMeleeBand)
            {
                ctx.RequestState(
                    BossStateId.Idle,
                    BossTransitionPriority.Normal);
                return;
            }

            strafeTimer -= ctx.deltaTime;

            if (strafeTimer <= 0f)
            {
                strafeTimer = Mathf.Max(
                    0.01f,
                    data.chaseStrafeFlipInterval);
                strafeSide = -strafeSide;
            }

            Vector3 toTarget =
                ctx.target.position - ctx.bossTransform.position;
            toTarget.y = 0f;

            Vector3 aim = ctx.target.position;

            if (toTarget.sqrMagnitude > MIN_DIRECTION_SQR)
            {
                Vector3 direction = toTarget.normalized;
                Vector3 side =
                    Vector3.Cross(Vector3.up, direction) * strafeSide;

                aim += side * data.chaseStrafeDistance;
            }

            ctx.agent.SetDestination(aim);
        }

        public void OnExit(BossContext ctx)
        {
            ctx.agent.isStopped = true;
        }
    }
}
