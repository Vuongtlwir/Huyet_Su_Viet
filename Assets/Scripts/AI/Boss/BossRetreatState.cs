using BossAI.Core;
using UnityEngine;
using UnityEngine.AI;

namespace BossAI.Boss
{
    public class BossRetreatState : IFsmState<BossContext>
    {
        private float timer;

        public string StateName => "Retreat";

        public void OnEnter(BossContext ctx)
        {
            ctx.agent.isStopped = false;

            BossPhaseData data = ctx.phaseData;
            timer = data.retreatDuration;

            Vector3 direction = ctx.target != null
                ? ctx.bossTransform.position - ctx.target.position
                : ctx.bossTransform.forward;

            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = ctx.bossTransform.forward;

            Vector3 destination =
                ctx.bossTransform.position +
                direction.normalized * data.retreatDistance;

            NavMeshHit hit;

            if (NavMesh.SamplePosition(
                    destination,
                    out hit,
                    2f,
                    NavMesh.AllAreas))
            {
                ctx.agent.SetDestination(hit.position);
            }
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
            ctx.agent.isStopped = true;
        }
    }
}
