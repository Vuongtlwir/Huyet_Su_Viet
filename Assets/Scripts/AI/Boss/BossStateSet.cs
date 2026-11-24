using BossAI.Core;

namespace BossAI.Boss
{
    public class BossStateSet
    {
        public readonly IFsmState<BossContext> Idle;
        public readonly IFsmState<BossContext> Chase;
        public readonly IFsmState<BossContext> Attack;
        public readonly IFsmState<BossContext> Recover;
        public readonly IFsmState<BossContext> Retreat;
        public readonly IFsmState<BossContext> Teleport;
        public readonly IFsmState<BossContext> Stunned;
        public readonly IFsmState<BossContext> PhaseTransition;
        public readonly IFsmState<BossContext> PhaseTransitionRecover;

        public BossStateSet()
        {
            Idle = new BossIdleState();
            Chase = new BossChaseState();
            Attack = new BossAttackState();
            Recover = new BossRecoverState();
            Retreat = new BossRetreatState();
            Teleport = new BossTeleportState();
            Stunned = new BossStunnedState();
            PhaseTransition = new BossPhaseTransitionState();
            PhaseTransitionRecover = new BossPhaseTransitionRecoverState();
        }

        public IFsmState<BossContext> Get(BossStateId id)
        {
            switch (id)
            {
                case BossStateId.Idle: return Idle;
                case BossStateId.Chase: return Chase;
                case BossStateId.Attack: return Attack;
                case BossStateId.Recover: return Recover;
                case BossStateId.Retreat: return Retreat;
                case BossStateId.Teleport: return Teleport;
                case BossStateId.Stunned: return Stunned;
                case BossStateId.PhaseTransition: return PhaseTransition;
                case BossStateId.PhaseTransitionRecover: return PhaseTransitionRecover;
                default: return null;
            }
        }
    }
}
