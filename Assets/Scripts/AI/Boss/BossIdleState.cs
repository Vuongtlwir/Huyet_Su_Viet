using BossAI.Core;
using UnityEngine;
using UnityEngine.AI;

namespace BossAI.Boss
{
    public class BossIdleState : IFsmState<BossContext>
    {
        private const float MIN_IDLE_DISTANCE_FLOOR = 0.5f;
        private const float DISTANCE_TOLERANCE = 0.35f;
        private const float MIN_DIRECTION_SQR = 0.0001f;
        private const float NAVMESH_SAMPLE_RADIUS = 2f;
        private const float REPATH_DISTANCE = 0.5f;
        private const float PATH_RETRY_INTERVAL = 0.5f;

        private readonly NavMeshPath navPath = new NavMeshPath();

        private bool hasDestination;
        private Vector3 lastDesiredPosition;
        private float nextPathRetryTime;
        private float reactionTimer;
        private bool reactionArmed;
        private int strafeSide = 1;

        public string StateName => "Idle";

        public void OnEnter(BossContext ctx)
        {
            hasDestination = false;
            nextPathRetryTime = 0f;
            reactionTimer = 0f;
            reactionArmed = false;
            strafeSide = -strafeSide;

            if (ctx.agent != null)
            {
                if (ctx.agent.hasPath)
                    ctx.agent.ResetPath();

                ctx.agent.stoppingDistance = 0f;
                ctx.agent.isStopped = false;
            }

            RecordIdleDistance(ctx);
        }

        public void Tick(BossContext ctx)
        {
            if (ctx.target == null || ctx.phaseData == null)
            {
                StopIdleMovement(ctx);
                return;
            }

            if (ctx.bossTransform == null || ctx.agent == null)
                return;

            ctx.controller.FaceTarget();

            if (!IsUsableDistance(ctx.idleTargetDistance))
                RecordIdleDistance(ctx);

            Vector3 away = FlatOffsetXZ(
                ctx.target.position,
                ctx.bossTransform.position);

            float flatDistance = away.magnitude;

            BossPhaseData data = ctx.phaseData;

            if (flatDistance < data.dMelee)
            {
                BossSkillData melee = ctx.controller.SelectAttack(ctx);

                if (melee != null)
                {
                    ctx.currentAttack = melee;
                    StopIdleMovement(ctx);
                    ctx.RequestState(
                        BossStateId.Attack,
                        BossTransitionPriority.Normal);
                    return;
                }
            }

            bool wantsChase =
                flatDistance >= data.dMelee && !ctx.insideMeleeBand;
            bool wantsApproach =
                flatDistance > ctx.idleTargetDistance + DISTANCE_TOLERANCE;

            if (wantsChase || wantsApproach)
            {
                if (!reactionArmed)
                {
                    reactionArmed = true;
                    reactionTimer = data.idleReactionDelay;
                }

                reactionTimer -= ctx.deltaTime;

                if (reactionTimer > 0f)
                {
                    StopIdleMovement(ctx);
                    return;
                }
            }
            else
            {
                reactionArmed = false;
                reactionTimer = 0f;
            }

            BossSkillData attack = ctx.controller.SelectAttack(ctx);

            if (attack != null)
            {
                ctx.currentAttack = attack;
                StopIdleMovement(ctx);
                ctx.RequestState(
                    BossStateId.Attack,
                    BossTransitionPriority.Normal);
                return;
            }

            if (wantsChase)
            {
                ctx.RequestState(
                    BossStateId.Chase,
                    BossTransitionPriority.Normal);
                return;
            }

            MaintainDistance(ctx, away, flatDistance);

            if (flatDistance < data.dMelee &&
                ctx.teleportCooldown <= 0f &&
                !ctx.lastActionWasTeleport)
            {
                StopIdleMovement(ctx);
                ctx.RequestState(
                    BossStateId.Teleport,
                    BossTransitionPriority.Normal);
            }
        }

        public void OnExit(BossContext ctx)
        {
            StopIdleMovement(ctx);
        }

        private void MaintainDistance(
            BossContext ctx,
            Vector3 away,
            float flatDistance)
        {
            if (away.sqrMagnitude < MIN_DIRECTION_SQR)
            {
                away = ctx.bossTransform.forward;
                away.y = 0f;

                if (away.sqrMagnitude < MIN_DIRECTION_SQR)
                    away = Vector3.forward;
            }

            away.Normalize();

            float distanceError = flatDistance - ctx.idleTargetDistance;

            if (Mathf.Abs(distanceError) <= DISTANCE_TOLERANCE)
            {
                if (hasDestination)
                    StopIdleMovement(ctx);

                return;
            }

            Vector3 bossPosition = ctx.bossTransform.position;
            Vector3 targetPosition = ctx.target.position;

            Vector3 radial = Quaternion.Euler(
                0f,
                ctx.phaseData.idleStrafeAngle * strafeSide,
                0f) * away;

            Vector3 desiredPosition =
                targetPosition + radial * ctx.idleTargetDistance;
            desiredPosition.y = bossPosition.y;

            if (hasDestination)
            {
                Vector3 moved = desiredPosition - lastDesiredPosition;
                moved.y = 0f;

                if (moved.sqrMagnitude < REPATH_DISTANCE * REPATH_DISTANCE &&
                    !NeedsNewPath(ctx))
                {
                    return;
                }
            }

            if (ctx.time < nextPathRetryTime)
                return;

            NavMeshHit hit;

            if (!NavMesh.SamplePosition(
                    desiredPosition,
                    out hit,
                    NAVMESH_SAMPLE_RADIUS,
                    NavMesh.AllAreas))
            {
                StopIdleMovement(ctx);
                nextPathRetryTime = ctx.time + PATH_RETRY_INTERVAL;
                return;
            }

            if (!ctx.agent.CalculatePath(hit.position, navPath) ||
                navPath.status != NavMeshPathStatus.PathComplete)
            {
                StopIdleMovement(ctx);
                nextPathRetryTime = ctx.time + PATH_RETRY_INTERVAL;
                return;
            }

            if (ctx.agent.isStopped)
                ctx.agent.isStopped = false;

            ctx.agent.SetDestination(hit.position);

            lastDesiredPosition = desiredPosition;
            hasDestination = true;
        }

        private void RecordIdleDistance(BossContext ctx)
        {
            BossPhaseData data = ctx.phaseData;

            if (data == null ||
                ctx.target == null ||
                ctx.bossTransform == null)
            {
                ctx.idleTargetDistance = MIN_IDLE_DISTANCE_FLOOR;
                return;
            }

            float entryDistance = FlatOffsetXZ(
                ctx.target.position,
                ctx.bossTransform.position).magnitude;

            float minIdleDistance = Mathf.Max(
                MIN_IDLE_DISTANCE_FLOOR,
                data.dMelee);

            float maxIdleDistance = Mathf.Max(
                minIdleDistance,
                data.dSweep - DISTANCE_TOLERANCE);

            ctx.idleTargetDistance = Mathf.Clamp(
                entryDistance,
                minIdleDistance,
                maxIdleDistance);
        }

        private static Vector3 FlatOffsetXZ(Vector3 from, Vector3 to)
        {
            Vector3 offset = to - from;
            offset.y = 0f;
            return offset;
        }

        private static bool NeedsNewPath(BossContext ctx)
        {
            if (ctx.agent.pathPending)
                return false;

            return !ctx.agent.hasPath ||
                   ctx.agent.pathStatus != NavMeshPathStatus.PathComplete;
        }

        private void StopIdleMovement(BossContext ctx)
        {
            hasDestination = false;

            if (ctx.agent == null)
                return;

            if (ctx.agent.hasPath)
                ctx.agent.ResetPath();

            ctx.agent.isStopped = true;
        }

        private static bool IsUsableDistance(float distance)
        {
            return !float.IsNaN(distance) &&
                   !float.IsInfinity(distance) &&
                   distance > 0f;
        }
    }
}
