using UnityEngine;

namespace BossAI.Boss
{
    public class BossHealth : MonoBehaviour
    {
        [SerializeField] private float maxHp = 1000f;
        [SerializeField] private float phase1Threshold = 0.7f;
        [SerializeField] private float phase2Threshold = 0.4f;

        public float MaxHp => maxHp;
        public float CurrentHp { get; private set; }

        public float NormalizedHp =>
            maxHp > 0f ? CurrentHp / maxHp : 0f;

        private void Awake()
        {
            CurrentHp = maxHp;
        }

        public float GetThreshold(BossPhase phase)
        {
            if (phase == BossPhase.Phase1)
                return phase1Threshold;

            if (phase == BossPhase.Phase2)
                return phase2Threshold;

            return 0f;
        }

        public float TakeDamage(float amount, BossPhase phase)
        {
            if (amount <= 0f || CurrentHp <= 0f)
                return 0f;

            float minHp = maxHp * GetThreshold(phase);
            float actual = amount;

            if (CurrentHp - amount < minHp)
                actual = CurrentHp - minHp;

            if (actual < 0f)
                actual = 0f;

            CurrentHp -= actual;

            if (CurrentHp < 0f)
                CurrentHp = 0f;

            return actual;
        }
    }
}
