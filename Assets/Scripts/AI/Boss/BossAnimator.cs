using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace BossAI.Boss
{
    [RequireComponent(typeof(Animator))]
    public class BossAnimator : MonoBehaviour
    {
        private const int LOOP_INPUT = 0;
        private const int ACTION_INPUT = 1;
        private const int INPUT_COUNT = 2;

        private const float LOOP_FADE = 0.15f;
        private const float ACTION_FADE = 0.08f;
        private const float MOVE_SPEED_SQR = 0.04f;
        private const float MIN_TIME_SCALE = 0.01f;

        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip idleClip;
        [SerializeField] private AnimationClip moveClip;

        private PlayableGraph graph;
        private AnimationMixerPlayable mixer;
        private AnimationClipPlayable loopPlayable;
        private AnimationClipPlayable actionPlayable;
        private NavMeshAgent agent;

        private AnimationClip currentLoopClip;
        private AnimationClip currentActionClip;
        private float loopWeight = 1f;
        private float actionWeight;
        private bool actionPlaying;

        public bool IsActionPlaying => actionPlaying;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponent<Animator>();

            agent = GetComponent<NavMeshAgent>();

            if (animator == null)
            {
                Debug.LogWarning("BossAnimator: thiếu Animator", this);
                enabled = false;
                return;
            }

            graph = PlayableGraph.Create("BossAnimator");
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

            mixer = AnimationMixerPlayable.Create(graph, INPUT_COUNT);

            AnimationPlayableOutput output =
                AnimationPlayableOutput.Create(graph, "BossOutput", animator);
            output.SetSourcePlayable(mixer);

            mixer.SetInputWeight(LOOP_INPUT, loopWeight);
            mixer.SetInputWeight(ACTION_INPUT, actionWeight);

            graph.Play();
        }

        private void Start()
        {
            SetLoopClip(idleClip);
        }

        private void Update()
        {
            if (!graph.IsValid())
                return;

            UpdateLoopClip();
            WrapLoopTime();
            UpdateAction();

            FadeWeights(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (graph.IsValid())
                graph.Destroy();
        }

        public void PlayAction(AnimationClip clip, float timeScale = 1f)
        {
            if (clip == null || !graph.IsValid())
                return;

            if (actionPlayable.IsValid())
                graph.Disconnect(mixer, ACTION_INPUT);

            currentActionClip = clip;
            actionPlaying = true;

            actionPlayable = AnimationClipPlayable.Create(graph, clip);
            actionPlayable.SetSpeed(Mathf.Max(MIN_TIME_SCALE, timeScale));
            actionPlayable.SetTime(0.0);
            actionPlayable.SetApplyFootIK(true);

            graph.Connect(actionPlayable, 0, mixer, ACTION_INPUT);
        }

        public void StopAction()
        {
            actionPlaying = false;
        }

        private void UpdateLoopClip()
        {
            bool moving = agent != null &&
                          agent.velocity.sqrMagnitude > MOVE_SPEED_SQR;

            SetLoopClip(moving && moveClip != null ? moveClip : idleClip);
        }

        private void SetLoopClip(AnimationClip clip)
        {
            if (clip == null || clip == currentLoopClip)
                return;

            if (loopPlayable.IsValid())
                graph.Disconnect(mixer, LOOP_INPUT);

            currentLoopClip = clip;

            loopPlayable = AnimationClipPlayable.Create(graph, clip);
            loopPlayable.SetApplyFootIK(true);
            loopPlayable.SetTime(0.0);

            graph.Connect(loopPlayable, 0, mixer, LOOP_INPUT);
        }

        private void WrapLoopTime()
        {
            if (!loopPlayable.IsValid() || currentLoopClip == null)
                return;

            double length = currentLoopClip.length;

            if (length <= 0.0)
                return;

            double time = loopPlayable.GetTime();

            if (time >= length)
                loopPlayable.SetTime(time % length);
        }

        private void UpdateAction()
        {
            if (!actionPlaying)
                return;

            if (currentActionClip == null || !actionPlayable.IsValid())
            {
                actionPlaying = false;
                return;
            }

            if (actionPlayable.GetTime() >= currentActionClip.length)
                actionPlaying = false;
        }

        private void FadeWeights(float deltaTime)
        {
            float targetLoop = actionPlaying ? 0f : 1f;
            float targetAction = actionPlaying ? 1f : 0f;

            loopWeight = Mathf.MoveTowards(
                loopWeight,
                targetLoop,
                deltaTime / LOOP_FADE);

            actionWeight = Mathf.MoveTowards(
                actionWeight,
                targetAction,
                deltaTime / ACTION_FADE);

            mixer.SetInputWeight(LOOP_INPUT, loopWeight);
            mixer.SetInputWeight(ACTION_INPUT, actionWeight);
        }
    }
}
