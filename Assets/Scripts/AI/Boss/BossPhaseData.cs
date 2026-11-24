using System;

namespace BossAI.Boss
{
    [Serializable]
    public class BossPhaseData
    {
        public BossPhase phase = BossPhase.Phase1;

        public float attackInterval = 1.1f;

        public float dMelee = 3f;
        public float dSweep = 6f;

        public float idleReactionDelay = 3f;
        public float idleStrafeAngle = 20f;
        public float chaseStrafeDistance = 1.5f;
        public float chaseStrafeFlipInterval = 2f;

        public float teleportCooldown = 8f;
        public float teleportRecover = 0.35f;
        public float teleportDistance = 5f;

        public float retreatDuration = 0.5f;
        public float retreatDistance = 3f;

        public float phaseTransitionDuration = 5f;
        public float phaseTransitionRecoverDuration = 1f;

        public float recoverFallback = 0.6f;

        public float comboWindowStart = 0.45f;
        public float comboWindowEnd = 0.8f;

        public BossSkillData attackB = new BossSkillData
        {
            skillId = 1,
            displayName = "Sword_Regular_B",
            windup = 0.45f,
            active = 0.15f,
            recover = 0.6f,
            range = 3f,
            damage = 10f
        };

        public BossSkillData attackC = new BossSkillData
        {
            skillId = 2,
            displayName = "Sword_Regular_C",
            windup = 0.55f,
            active = 0.15f,
            recover = 0.7f,
            range = 3f,
            damage = 12f
        };

        public BossSkillData attackDash = new BossSkillData
        {
            skillId = 3,
            displayName = "Sword_Dash_RM",
            windup = 0.3f,
            active = 0.2f,
            recover = 0.5f,
            range = 6f,
            damage = 15f,
            isDash = true,
            dashSpeed = 12f,
            dashDistance = 4f,
            cooldown = 6f
        };
    }
}
