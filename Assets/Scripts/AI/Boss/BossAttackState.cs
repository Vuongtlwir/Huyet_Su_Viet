using BossAI.Core;
using UnityEngine;

namespace BossAI.Boss
{
    public class BossAttackState : IFsmState<BossContext>
    {
        private readonly SkillManager skillManager = new SkillManager();

        private bool comboUsed;
        private Vector3 dashDirection;
        private float dashRemaining;

        public string StateName => "Attack";

        public void OnEnter(BossContext ctx)
        {
            if (ctx.currentAttack == null)
            {
                ctx.RequestState(
                    BossStateId.Idle,
                    BossTransitionPriority.Normal);
                return;
            }

            comboUsed = false;
            ctx.lastActionWasTeleport = false;
            ctx.nextAttackTime = ctx.time + ctx.phaseData.attackInterval;

            BossSkillData skill = ctx.currentAttack;
            ctx.RecordSkillUse(skill.skillId);

            if (skill.isDash)
            {
                ctx.nextDashTime = ctx.time + skill.cooldown;

                Vector3 direction = ctx.target != null
                    ? ctx.target.position - ctx.bossTransform.position
                    : ctx.bossTransform.forward;

                direction.y = 0f;

                if (direction.sqrMagnitude < 0.0001f)
                    direction = ctx.bossTransform.forward;

                dashDirection = direction.normalized;
                dashRemaining = skill.dashDistance;

                ctx.agent.ResetPath();
                ctx.agent.isStopped = false;
            }
            else
            {
                ctx.agent.isStopped = true;
            }

            skillManager.Begin(skill);
            PlaySkillClip(ctx, skill);
        }

        public void Tick(BossContext ctx)
        {
            BossSkillData skill = ctx.currentAttack;

            if (skill == null)
            {
                ctx.RequestState(
                    BossStateId.Idle,
                    BossTransitionPriority.Normal);
                return;
            }

            skillManager.Tick(ctx.deltaTime);

            if (skill.isDash)
            {
                ctx.controller.FaceDirection(dashDirection);

                if (skillManager.Phase == BossSkillPhase.Active)
                    MoveDash(ctx, skill);
            }
            else if (skillManager.Phase == BossSkillPhase.Windup)
            {
                ctx.controller.FaceTarget();
            }

            if (!comboUsed &&
                skill == ctx.phaseData.attackB &&
                ctx.phaseData.attackC != null &&
                skillManager.Phase == BossSkillPhase.Active)
            {
                float normalized = skillManager.NormalizedTime;

                if (normalized >= ctx.phaseData.comboWindowStart &&
                    normalized <= ctx.phaseData.comboWindowEnd &&
                    ctx.distanceToTarget <= ctx.phaseData.dMelee)
                {
                    comboUsed = true;
                    ctx.currentAttack = ctx.phaseData.attackC;
                    ctx.RecordSkillUse(ctx.currentAttack.skillId);
                    skillManager.Begin(ctx.currentAttack);
                    PlaySkillClip(ctx, ctx.currentAttack);
                    return;
                }
            }

            if (skillManager.IsDone)
            {
                ctx.RequestState(
                    BossStateId.Recover,
                    BossTransitionPriority.Normal);
            }
        }

        public void OnExit(BossContext ctx)
        {
            skillManager.Cancel();
            ctx.agent.isStopped = true;
        }

        private static void PlaySkillClip(BossContext ctx, BossSkillData skill)
        {
            if (ctx.animator == null || skill == null || skill.clip == null)
                return;

            float total = skill.windup + skill.active + skill.recover;

            float timeScale = total > 0f ? skill.clip.length / total : 1f;

            ctx.animator.PlayAction(skill.clip, timeScale);
        }

        private void MoveDash(BossContext ctx, BossSkillData skill)
        {
            if (dashRemaining <= 0f || skill.dashSpeed <= 0f)
                return;

            float step = skill.dashSpeed * ctx.deltaTime;

            if (step > dashRemaining)
                step = dashRemaining;

            ctx.agent.Move(dashDirection * step);
            dashRemaining -= step;
        }
    }
}
