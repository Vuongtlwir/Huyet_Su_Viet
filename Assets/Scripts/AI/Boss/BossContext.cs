using BossAI.Core;
using UnityEngine;
using UnityEngine.AI;

namespace BossAI.Boss
{
    public class BossContext
    {
        public BossController controller;
        public Transform bossTransform;
        public Transform target;
        public NavMeshAgent agent;
        public BossHealth health;
        public BossAnimator animator;

        public Fsm<BossContext> fsm;
        public BossStateSet states;

        public BossPhase currentPhase;
        public BossPhaseData phaseData;
        public BossSkillData currentAttack;

        public int lastUsedSkillId = -1;
        public int previousSkillId = -1;
        public int thirdLastSkillId = -1;

        public bool insideMeleeBand;

        private bool distanceBandsInitialized;

        public float nextAttackTime;
        public float nextDashTime;
        public float teleportCooldown;
        public float stunDuration = 1.5f;

        public float distanceToTarget = float.PositiveInfinity;
        public float idleTargetDistance;
        public float time;
        public float deltaTime;

        public bool isStunned;
        public bool isInvulnerable;
        public bool isPhaseTransitionRecover;
        public bool lastActionWasTeleport;
        public bool isDead;

        public void RequestState(BossStateId id, int priority)
        {
            if (fsm == null || states == null)
                return;

            IFsmState<BossContext> state = states.Get(id);

            if (state != null)
                fsm.RequestTransition(state, priority);
        }

        public void RecordSkillUse(int skillId)
        {
            if (skillId <= 0)
                return;

            thirdLastSkillId = previousSkillId;
            previousSkillId = lastUsedSkillId;
            lastUsedSkillId = skillId;
        }

        public void UpdateMeleeBand(
            float distance,
            float dMelee,
            float hysteresis)
        {
            if (!distanceBandsInitialized)
            {
                insideMeleeBand = distance < dMelee;
                distanceBandsInitialized = true;
                return;
            }

            if (insideMeleeBand)
            {
                if (distance > dMelee + hysteresis)
                    insideMeleeBand = false;
            }
            else if (distance < dMelee - hysteresis)
            {
                insideMeleeBand = true;
            }
        }
    }
}
