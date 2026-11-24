using BossAI.Core;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace BossAI.Boss
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(BossHealth))]
    public class BossController : MonoBehaviour
    {
        private const float DISTANCE_BAND_HYSTERESIS = 0.4f;
        private const float MIN_FACING_SQR = 0.0001f;

        private const float SKILL_RANGE_WEIGHT = 3f;
        private const float SKILL_REPEAT_PENALTY = 10f;
        private const float SKILL_SECOND_LAST_PENALTY = 2f;
        private const float SKILL_THIRD_LAST_PENALTY = 0.75f;
        private const float SKILL_RANDOM_JITTER = 0.35f;

        [SerializeField] private Transform target;
        [SerializeField] private BossPhaseData phase1Data;
        [SerializeField] private BossPhaseData phase2Data = null;
        [SerializeField] private BossPhaseData phase3Data = null;
        [SerializeField] private float stunDuration = 1.5f;
        [SerializeField] private float turnSpeed = 540f;
        

        private BossContext context;
        private Fsm<BossContext> fsm;

        public string CurrentStateName =>
            fsm != null ? fsm.CurrentStateName : "None";

        private void Awake()
        {
            if (phase1Data == null)
                phase1Data = new BossPhaseData();

            phase1Data.phase = BossPhase.Phase1;

            if (phase2Data != null)
                phase2Data.phase = BossPhase.Phase2;

            if (phase3Data != null)
                phase3Data.phase = BossPhase.Enrage;
        }

        private void Start()
        {
            if (target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");

                if (player != null)
                    target = player.transform;
            }

            context = new BossContext
            {
                controller = this,
                bossTransform = transform,
                target = target,
                agent = GetComponent<NavMeshAgent>(),
                health = GetComponent<BossHealth>(),
                animator = GetComponent<BossAnimator>(),
                currentPhase = BossPhase.Phase1,
                phaseData = phase1Data,
                stunDuration = stunDuration
            };

            BossStateSet states = new BossStateSet();
            context.states = states;

            fsm = new Fsm<BossContext>(context);
            context.fsm = fsm;

            fsm.Initialize(states.Get(BossStateId.Idle));
            context.agent.updateRotation = false;
        }

        private void Update()
        {
            if (context == null || fsm == null)
                return;

            context.deltaTime = Time.deltaTime;
            context.time = Time.time;

            context.distanceToTarget = target != null
                ? Vector3.Distance(transform.position, target.position)
                : float.PositiveInfinity;

            UpdateMeleeBand();

            context.teleportCooldown = Mathf.Max(
                0f,
                context.teleportCooldown - Time.deltaTime);

            ApplyGlobalRequests();
            fsm.Tick();
        }

        private void ApplyGlobalRequests()
        {
            if (context.isStunned ||
                context.isInvulnerable ||
                context.isPhaseTransitionRecover ||
                context.isDead)
                return;

            if (CanPhaseTransition())
            {
                context.RequestState(
                    BossStateId.PhaseTransition,
                    BossTransitionPriority.PhaseTransition);
            }
        }

        public bool CanPhaseTransition()
        {
            if (context == null || context.health == null)
                return false;

            float threshold = context.health.GetThreshold(context.currentPhase);

            return threshold > 0f &&
                   context.health.NormalizedHp <= threshold;
        }

        private void UpdateMeleeBand()
        {
            if (target == null || context.phaseData == null)
                return;

            Vector3 flat = target.position - transform.position;
            flat.y = 0f;

            context.UpdateMeleeBand(
                flat.magnitude,
                context.phaseData.dMelee,
                DISTANCE_BAND_HYSTERESIS);
        }

        public void FaceTarget()
        {
            if (target == null)
                return;

            FaceDirection(target.position - transform.position);
        }

        public void FaceDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < MIN_FACING_SQR)
                return;

            Quaternion look = Quaternion.LookRotation(direction, Vector3.up);

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                look,
                turnSpeed * Time.deltaTime);
        }

        public BossSkillData SelectAttack(BossContext ctx)
        {
            if (ctx == null || ctx.phaseData == null)
                return null;

            BossSkillData best = null;
            float bestScore = float.NegativeInfinity;

            BossPhaseData data = ctx.phaseData;

            EvaluateSkill(ctx, data.attackB, ref best, ref bestScore);
            EvaluateSkill(ctx, data.attackC, ref best, ref bestScore);
            EvaluateSkill(ctx, data.attackDash, ref best, ref bestScore);

            return best;
        }

        private static void EvaluateSkill(
            BossContext ctx,
            BossSkillData skill,
            ref BossSkillData best,
            ref float bestScore)
        {
            float score = ScoreSkill(ctx, skill);

            if (score > bestScore)
            {
                bestScore = score;
                best = skill;
            }
        }

        private static float ScoreSkill(BossContext ctx, BossSkillData skill)
        {
            if (skill == null)
                return float.NegativeInfinity;

            if (skill.isDash)
            {
                if (ctx.time < ctx.nextDashTime)
                    return float.NegativeInfinity;

                if (ctx.distanceToTarget < ctx.phaseData.dMelee ||
                    ctx.distanceToTarget > ctx.phaseData.dSweep)
                {
                    return float.NegativeInfinity;
                }
            }
            else
            {
                if (ctx.time < ctx.nextAttackTime)
                    return float.NegativeInfinity;

                if (ctx.distanceToTarget > skill.range)
                    return float.NegativeInfinity;
            }

            float score = DistanceScore(ctx, skill);

            if (skill.skillId == ctx.lastUsedSkillId)
                score -= SKILL_REPEAT_PENALTY;
            else if (skill.skillId == ctx.previousSkillId)
                score -= SKILL_SECOND_LAST_PENALTY;
            else if (skill.skillId == ctx.thirdLastSkillId)
                score -= SKILL_THIRD_LAST_PENALTY;

            score += UnityEngine.Random.value * SKILL_RANDOM_JITTER;
            return score;
        }

        private static float DistanceScore(
            BossContext ctx,
            BossSkillData skill)
        {
            BossPhaseData data = ctx.phaseData;
            float distance = ctx.distanceToTarget;

            if (skill.isDash)
            {
                float mid = (data.dMelee + data.dSweep) * 0.5f;
                float half = Mathf.Max(
                    0.01f,
                    (data.dSweep - data.dMelee) * 0.5f);

                float fit = 1f - Mathf.Abs(distance - mid) / half;
                return Mathf.Clamp01(fit) * SKILL_RANGE_WEIGHT;
            }

            float range = Mathf.Max(0.01f, skill.range);
            float nearFit = 1f - Mathf.Clamp01(distance / range);
            return nearFit * SKILL_RANGE_WEIGHT;
        }

        public BossPhaseData GetPhaseData(BossPhase phase)
        {
            if (phase == BossPhase.Phase1)
                return phase1Data;

            if (phase == BossPhase.Phase2)
                return phase2Data;

            if (phase == BossPhase.Enrage)
                return phase3Data;

            return null;
        }

        public void ResetCooldownsForNewPhase()
        {
            if (context == null)
                return;

            context.nextAttackTime = context.time;
            context.nextDashTime = context.time;
            context.teleportCooldown = 0f;
            context.lastActionWasTeleport = false;
        }

        public void ApplyStun(float duration = -1f)
        {
            if (context == null || context.isDead)
                return;

            if (context.isInvulnerable ||
                context.isPhaseTransitionRecover ||
                context.isStunned)
                return;

            context.stunDuration = duration > 0f ? duration : stunDuration;
            context.isStunned = true;

            context.RequestState(
                BossStateId.Stunned,
                BossTransitionPriority.Stunned);
        }

        public void TakeDamage(float amount)
        {
            if (context == null || context.isDead || context.isInvulnerable)
                return;

            context.health.TakeDamage(amount, context.currentPhase);
        }

        [ContextMenu("Test Damage 100")]
        private void TestDamage()
        {
            TakeDamage(100f);
        }

        [ContextMenu("Test Stun")]
        private void TestStun()
        {
            ApplyStun();
        }

        [ContextMenu("Log Current State")]
        private void LogCurrentState()
        {
            Debug.Log(CurrentStateName);
        }
    }
}
