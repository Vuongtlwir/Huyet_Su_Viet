using UnityEngine;

namespace BossAI.Boss
{
    public enum BossSkillPhase
    {
        Idle = 0,
        Windup = 1,
        Active = 2,
        Done = 3
    }

    public class SkillManager
    {
        private BossSkillData skill;
        private BossSkillPhase phase;
        private float phaseElapsed;
        private float totalElapsed;

        public BossSkillData CurrentSkill => skill;
        public BossSkillPhase Phase => phase;
        public bool IsDone => phase == BossSkillPhase.Done;

        public float NormalizedTime
        {
            get
            {
                if (skill == null)
                    return 1f;

                float total = skill.windup + skill.active;

                return total > 0f
                    ? Mathf.Clamp01(totalElapsed / total)
                    : 1f;
            }
        }

        public void Begin(BossSkillData newSkill)
        {
            skill = newSkill;
            phase = BossSkillPhase.Windup;
            phaseElapsed = 0f;
            totalElapsed = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (skill == null ||
                phase == BossSkillPhase.Idle ||
                phase == BossSkillPhase.Done)
                return;

            totalElapsed += deltaTime;
            phaseElapsed += deltaTime;

            if (phase == BossSkillPhase.Windup &&
                phaseElapsed >= skill.windup)
            {
                phaseElapsed -= skill.windup;
                phase = BossSkillPhase.Active;
            }

            if (phase == BossSkillPhase.Active &&
                phaseElapsed >= skill.active)
            {
                phase = BossSkillPhase.Done;
            }
        }

        public void Cancel()
        {
            skill = null;
            phase = BossSkillPhase.Idle;
            phaseElapsed = 0f;
            totalElapsed = 0f;
        }
    }
}
