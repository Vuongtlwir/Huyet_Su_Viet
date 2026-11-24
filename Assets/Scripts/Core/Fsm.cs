using System;

namespace BossAI.Core
{
    public class Fsm<TContext>
    {
        private readonly TContext context;

        private IFsmState<TContext> currentState;
        private IFsmState<TContext> pendingState;

        private int pendingPriority;

        private bool hasPendingTransition;
        private bool isTransitioning;
        private bool isTicking;

        public IFsmState<TContext> CurrentState => currentState;

        public string CurrentStateName =>
            currentState != null ? currentState.StateName : "None";

        public Fsm(TContext context)
        {
            this.context = context;
        }

        public void Initialize(IFsmState<TContext> initialState)
        {
            if (initialState == null)
                throw new ArgumentNullException(nameof(initialState));

            if (currentState != null)
                throw new InvalidOperationException(
                    "FSM has already been initialized.");

            currentState = initialState;
            currentState.OnEnter(context);
        }

        public void RequestTransition(
            IFsmState<TContext> nextState,
            int priority = 0)
        {
            if (nextState == null || currentState == null)
                return;

            if (ReferenceEquals(nextState, currentState))
                return;

            if (!hasPendingTransition || priority > pendingPriority)
            {
                pendingState = nextState;
                pendingPriority = priority;
                hasPendingTransition = true;
            }
        }

        public void Tick()
        {
            if (currentState == null || isTicking || isTransitioning)
                return;

            isTicking = true;

            try
            {
                ApplyPendingTransition();

                currentState.Tick(context);
            }
            finally
            {
                isTicking = false;
            }
        }

        private void ApplyPendingTransition()
        {
            if (!hasPendingTransition)
                return;

            IFsmState<TContext> nextState = pendingState;

            pendingState = null;
            pendingPriority = 0;
            hasPendingTransition = false;

            if (nextState == null ||
                ReferenceEquals(nextState, currentState))
                return;

            isTransitioning = true;

            try
            {
                currentState.OnExit(context);

                currentState = nextState;

                currentState.OnEnter(context);
            }
            finally
            {
                isTransitioning = false;
            }
        }

        public void ClearPendingTransition()
        {
            pendingState = null;
            pendingPriority = 0;
            hasPendingTransition = false;
        }
    }
}
