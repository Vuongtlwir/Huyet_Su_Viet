using BossAI.Core;
using UnityEngine;
using UnityEngine.AI;

namespace BossAI.Boss
{
    public class BossTeleportState : IFsmState<BossContext>
    {
        private float timer;

        public string StateName => "Teleport";

        public void OnEnter(BossContext ctx)
        {
            ctx.agent.isStopped = true;

            BossPhaseData data = ctx.phaseData;

            ctx.teleportCooldown = data.teleportCooldown;
            ctx.lastActionWasTeleport = true;
            timer = data.teleportRecover;

            Vector3 direction = ctx.target != null
                ? ctx.bossTransform.position - ctx.target.position
                : ctx.bossTransform.forward;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = ctx.bossTransform.forward;

            Vector3 destination =
                ctx.bossTransform.position +
                direction.normalized * data.teleportDistance;

            NavMeshHit hit;
            // Cần lưu ý vì nếu kh tìm được vị trí thích hợp thì kh dịch chuyển nhưng vẫn cooldown cần lưu ý xử lý sau 
            if (NavMesh.SamplePosition(
                    destination,
                    out hit,
                    2f,
                    NavMesh.AllAreas))
            {
                ctx.agent.Warp(hit.position);
            }
        }

        public void Tick(BossContext ctx)
        {
            ctx.controller.FaceTarget();

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
            ctx.agent.isStopped = false;
        }
    }
}
