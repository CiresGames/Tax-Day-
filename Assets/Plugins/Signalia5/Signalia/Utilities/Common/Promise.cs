using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using DG.Tweening;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Promise utility class for managing promise-based execution flows in Signalia.
    /// It allows the user to chain actions to be executed in steps.
    /// PromiseFlow uses manual flow. TimePromise uses time-based flow.
    /// </summary>
    public static class Promise
    {
        /// <summary>
        /// A promise that progresses by manual signal per step.
        /// </summary>
        public class PromiseFlow
        {
            /// <summary>
            /// Used to define a step in a promise. Only for PromiseFlow type.
            /// </summary>
            public delegate void PromiseStep(Action finished);

            private readonly List<PromiseStep> steps = new();
            private readonly List<Tween> tweenQueue = new();
            private readonly List<Radio.Listener> activeListeners = new();
            private bool isRunning = false;
            private bool isDisposed = false;
            private int currentStepIndex = 0;
            private int? requestedStepIndex = null;

            public static PromiseFlow Begin()
            {
                return new PromiseFlow();
            }

            /// <summary>
            /// Queue a normal manually progressive step.
            /// </summary>
            /// <param name="step"></param>
            /// <returns></returns>
            public PromiseFlow NQ(PromiseStep step)
            {
                if (isDisposed)
                    return this;

                steps.Add(step);
                TryRunNext();
                return this;
            }

            /// <summary>
            /// Queue a wait step that delays for the specified time before proceeding.
            /// </summary>
            public PromiseFlow NQWait(float time, bool unscaled = false)
            {
                if (isDisposed)
                    return this;

                steps.Add(finished =>
                {
                    tweenQueue.Add(SIGS.DoIn(time, () =>
                    {
                        if (isDisposed) return;
                        finished?.Invoke();
                    }, unscaled));
                });
                TryRunNext();
                return this;
            }

            /// <summary>
            /// Queues a step that waits for an event call to make it pass.
            /// The promise will pause at this step until the specified event is triggered.
            /// Uses the approved SIGS.Listener pipeline for proper event handling.
            /// </summary>
            /// <param name="eventString">The event string to listen for</param>
            /// <returns>This PromiseFlow instance for method chaining</returns>
            public PromiseFlow NQListen(string eventString)
            {
                if (isDisposed)
                    return this;

                steps.Add(finished =>
                {
                    // Create a one-time listener using the approved SIGS.Listener pipeline
                    var listener = SIGS.Listener(eventString, () =>
                    {
                        // Continue the promise when the event is received
                        finished?.Invoke();
                    }, oneShot: true);

                    // Track the listener for cleanup
                    activeListeners.Add(listener);
                });

                TryRunNext();
                return this;
            }

            /// <summary>
            /// Queue an instant step that runs immediately and advances the flow.
            /// </summary>
            public PromiseFlow NQNow(Action step)
            {
                if (isDisposed)
                    return this;

                return NQ(finished =>
                {
                    step?.Invoke();
                    finished?.Invoke();
                });
            }

            /// <summary>
            /// Move the flow to a specific step index.
            /// </summary>
            public PromiseFlow StepTo(int stepIndex)
            {
                if (isDisposed)
                    return this;

                if (stepIndex < 0 || stepIndex >= steps.Count)
                    return this;

                if (isRunning)
                {
                    requestedStepIndex = stepIndex;
                    return this;
                }

                currentStepIndex = stepIndex;
                TryRunNext();
                return this;
            }

            /// <summary>
            /// Move the flow to the next step.
            /// </summary>
            public PromiseFlow StepNext()
            {
                return StepTo(currentStepIndex + 1);
            }

            /// <summary>
            /// Move the flow to the previous step.
            /// </summary>
            public PromiseFlow StepPrevious()
            {
                return StepTo(currentStepIndex - 1);
            }

            private void TryRunNext()
            {
                if (isRunning || isDisposed)
                    return;

                if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
                    return;

                isRunning = true;
                var next = steps[currentStepIndex];
                next(() =>
                {
                    isRunning = false;
                    if (requestedStepIndex.HasValue)
                    {
                        currentStepIndex = requestedStepIndex.Value;
                        requestedStepIndex = null;
                    }
                    else
                    {
                        currentStepIndex++;
                    }
                    TryRunNext();
                });
            }

            public void Dispose()
            {
                isDisposed = true;
                steps.Clear();
                currentStepIndex = 0;
                requestedStepIndex = null;
                
                // Clean up all active tweens
                tweenQueue.ForEach(t => t?.Kill());
                tweenQueue.Clear();
                
                // Clean up all active listeners using the approved disposal method
                foreach (var listener in activeListeners)
                {
                    listener?.Dispose();
                }
                activeListeners.Clear();
            }

            public bool IsDisposed => isDisposed;
            public int CurrentStepIndex => currentStepIndex;
            public int StepCount => steps.Count;
        }

        /// <summary>
        /// A promise that progresses automatically after a fixed delay per step.
        /// </summary>
        public class TimePromise
        {
            private readonly Queue<(float delay, Action step, bool unscaled)> steps = new();
            private readonly List<Tween> tweenQueue = new();
            private bool isRunning = false;
            private bool isDisposed = false;
            private readonly float defaultDelay;

            private TimePromise(float defaultTime)
            {
                defaultDelay = defaultTime;
            }

            /// <summary>
            /// Begins a TimePromise with a required default wait time.
            /// </summary>
            public static TimePromise Begin(float defaultTime)
            {
                return new TimePromise(defaultTime);
            }

            /// <summary>
            /// Queue a step. If time is null, the default promise time is used.
            /// </summary>
            public TimePromise NQ(Action step, float? time = null, bool unscaled = false)
            {
                if (isDisposed)
                    return this;

                float finalTime = time ?? defaultDelay;
                steps.Enqueue((finalTime, step, unscaled));
                TryRunNext();
                return this;
            }

            private void TryRunNext()
            {
                if (isRunning || steps.Count == 0 || isDisposed)
                    return;

                isRunning = true;

                var (time, step, unscaled) = steps.Dequeue();
                tweenQueue.Add(SIGS.DoIn(time, () =>
                {
                    if (isDisposed) return;

                    step?.Invoke();
                    isRunning = false;
                    TryRunNext();
                }, unscaled));
            }

            public void Dispose()
            {
                isDisposed = true;
                steps.Clear();
                tweenQueue.ForEach(t => t?.Kill());
            }

            public bool IsDisposed => isDisposed;
        }
    }
}
