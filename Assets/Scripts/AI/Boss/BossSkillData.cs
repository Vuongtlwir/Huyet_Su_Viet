using System;
using UnityEngine;

namespace BossAI.Boss
{
    [Serializable]
    public class BossSkillData
    {
        public int skillId;
        public string displayName;
        public AnimationClip clip;

        public float windup = 0.45f;
        public float active = 0.15f;
        public float recover = 0.6f;

        public float range = 3f;
        public float damage = 10f;

        public bool isDash;
        public float dashSpeed = 12f;
        public float dashDistance = 4f;

        public float cooldown;
    }
}
