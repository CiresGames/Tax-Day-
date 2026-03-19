using System;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using DG.Tweening;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    public static class SSUtility
    {
        private static System.Random _rng = new();

        /// <summary>
        /// Performs a method after a set amount of time.
        /// </summary>
        /// <param name="time"></param>
        public static Tween DoIn(float time, System.Action methodToSequence, bool unscaled = true)
        {
            var tween = DOVirtual.DelayedCall(time, () => methodToSequence.Invoke(), unscaled);
            return tween;
        }


        /// <summary>
        /// Performs a method every frame for a set amount of time.
        /// </summary>
        /// <param name="duration"></param>
        /// <param name="frequency"></param>
        /// <param name="methodToLoop"></param>
        /// <returns></returns>
        public static Tween DoEveryIntervalFor(float duration, float frequency, System.Action methodToLoop, bool unscaled = true)
        {
            var sequence = DOTween.Sequence();
            sequence.Append
            (
                DOTween.Sequence()
                    .AppendInterval(frequency)
                    .AppendCallback(() => methodToLoop.Invoke())
                    .SetLoops(Mathf.CeilToInt(duration / frequency), LoopType.Restart)
                    .SetUpdate(unscaled)
            );
            return sequence;
        }

        /// <summary>
        /// Do a callback when a condition is met. Can be stored in a variable and killed when needed. If condition is already met, callback is called immediately.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="callback"></param>
        public static Tween DoWhen(Func<bool> condition, Action callback, float expiry = -1)
        {
            if (condition())
            {
                callback?.Invoke();
                return null;
            }
            // Store a reference to the tween
            Tween tw = null;
            // Create a tween that updates every frame
            tw = DOTween.To(() => 0, x => { }, 0, float.MaxValue)
                .OnUpdate(() =>
                {
                    try
                    {
                        if (condition())
                        {
                            callback?.Invoke();
                            tw.Complete(); // Kill only this tween
                        }
                    }
                    catch (Exception)
                    {
                        // don't really throw an error, just stop the tween
                        tw?.Kill();
                    }
                });

            if (expiry > 0)
                SIGS.DoIn(expiry, () =>
                {
                    tw?.Kill();
                });
            return tw;
        }

        /// <summary>
        /// Does the callback while the condition is true. Requires a locker boolean that must become false before the callback can be called again so it can be controlled.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Tween DoWhile(Func<bool> condition, Action callback, float waitTimeAfterLock, Func<bool> locker, bool debugSteps = false)
        {
            // Store a reference to the tween
            Tween tw = null;
            // Create a tween that updates every frame
            tw = DOTween.To(() => 0, x => { }, 0, float.MaxValue)
                .OnUpdate(() =>
                {
                    try
                    {
                        if (condition())
                        {
                            if (debugSteps)
                            {
                                Debug.Log("DoWhile condition is true for callback: " + callback.Method.ToString());
                            }
                            callback?.Invoke();
                            tw.Pause();
                            if (debugSteps)
                            {
                                Debug.Log($"DoWhile paused for callback: {callback.Method} and waiting for locker {locker.Method}");
                            }
                            // wait until locker is false then resume. Very intricate stuff!
                            DoWhen(() => !locker(), () => DoIn(waitTimeAfterLock, () =>
                            {
                                tw.Play();
                                if (debugSteps)
                                {
                                    Debug.Log($"DoWhile has unlocked for callback: {callback.Method} and will execute again!");
                                }
                            }));
                        }
                    }
                    catch (Exception)
                    {
                        // don't really throw an error, just stop the tween
                        tw?.Kill();
                    }

                });
            return tw;
        }

        /// <summary>
        /// Does the callback until the condition is true.
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public static Tween DoUntil(Func<bool> condition, Action callback)
        {
            // if already true, do not do anything
            if (condition())
            {
                return null;
            }

            // Store a reference to the tween
            Tween tw = null;

            // Create a tween that updates every frame
            tw = DOTween.To(() => 0, x => { }, 0, float.MaxValue)
                .OnUpdate(() =>
                {
                    try
                    {
                        if (condition())
                        {
                            tw.Complete(); // Kill only this tween
                        }

                        callback?.Invoke();
                    }
                    catch (Exception)
                    {
                        // don't really throw an error, just stop the tween
                        tw?.Kill();
                    }

                });
            return tw;
        }

        /// <summary>
        /// Like DoWhen, but does the method every time the condition becomes true (on transition from false to true).
        /// </summary>
        /// <param name="condition">The condition to evaluate.</param>
        /// <param name="callback">The action to invoke when the condition becomes true.</param>
        /// <returns>A Tween that handles the update loop.</returns>
        public static Tween DoWhenever(Func<bool> condition, Action callback, Action alternativeCallback = null)
        {
            // Store a reference to the tween
            Tween tw = null;

            // Track the previous condition state
            bool wasConditionTrue = false;

            // Create a tween that updates every frame
            tw = DOTween.To(() => 0, x => { }, 0, float.MaxValue)
                .SetUpdate(true) // Ensures it runs even if the timeScale is 0
                .OnUpdate(() =>
                {
                    try
                    {
                        bool currentCondition = condition();

                        // Trigger the callback only when transitioning from false to true
                        if (currentCondition && !wasConditionTrue)
                        {
                            callback?.Invoke();
                        }

                        // If an alternative callback is provided, trigger it when transitioning from true to false
                        if (!currentCondition && wasConditionTrue)
                        {
                            alternativeCallback?.Invoke();
                        }

                        // Update the previous condition state
                        wasConditionTrue = currentCondition;
                    }
                    catch (Exception)
                    {
                        // don't really throw an error, just stop the tween
                        tw?.Kill();
                    }
                });

            return tw;
        }

        /// <summary>
        /// Queues up multiple (delay, action) steps in sequence. 
        /// Example usage:
        /// <code>
        /// DoChained(
        ///     (1f, () => Debug.Log("Step 1 after 1 second")),
        ///     (2f, () => Debug.Log("Step 2 after additional 2 seconds"))
        /// );
        /// </code>
        /// </summary>
        // public static Tween DoChained(params (float delay, Action action)[] steps)
        // {
        //     var sequence = DOTween.Sequence();
        //     foreach (var step in steps)
        //     {
        //         // Wait for step.delay, then call step.action
        //         sequence.AppendInterval(step.delay).AppendCallback(() => step.action?.Invoke());
        //     }
        //     return sequence;
        // } dont use, garbage method that currently isn't useful

        /// <summary>
        /// Performs a callback at random intervals within [minDelay, maxDelay] for 'totalDuration'.
        /// If totalDuration <= 0, it will run indefinitely (until the tween is killed).
        /// </summary>
        /// <param name="minDelay">Minimum random delay.</param>
        /// <param name="maxDelay">Maximum random delay.</param>
        /// <param name="callback">Action to invoke each time the random interval completes.</param>
        /// <param name="totalDuration">How long to keep firing random callbacks. If 0 or less, runs forever.</param>
        /// <returns></returns>
        public static Tween DoRandomly(float minDelay, float maxDelay, Action callback, float totalDuration = 0f)
        {
            // If totalDuration is positive, we track time. Otherwise run indefinitely until killed.
            float elapsed = 0f;
            System.Random rng = new System.Random();
            float nextInterval = (float)(rng.NextDouble() * (maxDelay - minDelay) + minDelay);

            // We'll create an "infinite" tween, but handle the duration ourselves (if totalDuration > 0)
            Tween tw = null;

            tw = DOTween.To(() => elapsed, x => elapsed = x, float.MaxValue, float.MaxValue)
                .OnUpdate(() =>
                {
                    // If totalDuration > 0, stop if we've hit or exceeded it
                    if (totalDuration > 0f && elapsed >= totalDuration)
                    {
                        tw.Kill();
                        return;
                    }

                    // Once we've reached this random interval, fire callback & pick a new interval
                    if (elapsed >= nextInterval)
                    {
                        callback?.Invoke();
                        // pick next interval
                        nextInterval = elapsed + (float)(rng.NextDouble() * (maxDelay - minDelay) + minDelay);
                    }
                });

            return tw;
        }

        /// <summary>
        /// Performs a callback at fixed intervals indefinitely, until you kill the tween.
        /// This is like DoEveryIntervalFor() but without a duration limit.
        /// </summary>
        /// <param name="interval">Time between callbacks.</param>
        /// <param name="callback">Action to invoke.</param>
        /// <param name="unscaled">Use unscaled time?</param>
        public static Tween DoEveryInterval(float interval, Action callback, bool unscaled = true)
        {
            var sequence = DOTween.Sequence().SetUpdate(unscaled);
            sequence.AppendInterval(interval)
                    .AppendCallback(() => callback?.Invoke())
                    .SetLoops(-1, LoopType.Restart); // infinite loops
            return sequence;
        }

        /// <summary>
        /// Executes an action every frame using DOTween's update loop.
        /// </summary>
        /// <param name="callback">Action to invoke each frame.</param>
        /// <param name="unscaled">Use unscaled time? False means regular frame time.</param>
        public static Tween DoFrameUpdate(Action callback, bool unscaled = false)
        {
            if (callback is null)
            {
                return null;
            }

            Tween tw = null;
            tw = DOTween.To(() => 0f, _ => { }, 0f, float.MaxValue)
                .SetUpdate(unscaled)
                .OnUpdate(() =>
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception)
                    {
                        tw?.Kill();
                    }
                });

            return tw;
        }

        /// <summary>
        /// Attempts a callback that can succeed or fail. Retries up to 'maxRetries' times with optional delay between attempts.
        /// Succeeds on the first time 'tryAction' returns true, else fails if all attempts fail.
        /// </summary>
        /// <param name="tryAction">A function that returns true if succeeded, false if failed.</param>
        /// <param name="maxRetries">Number of attempts before giving up.</param>
        /// <param name="delayBetweenAttempts">Delay between each attempt.</param>
        /// <param name="onSuccess">Invoked when an attempt succeeds for the first time.</param>
        /// <param name="onFailure">Invoked if all attempts fail.</param>
        /// <returns>A tween reference.</returns>
        public static Tween DoRetries(Func<bool> tryAction, int maxRetries, float delayBetweenAttempts,
                                      Action onSuccess = null, Action onFailure = null)
        {
            var sequence = DOTween.Sequence();
            int attemptCount = 0;
            bool success = false;

            // We'll loop attempts in the Sequence
            for (int i = 0; i < maxRetries; i++)
            {
                sequence.AppendCallback(() =>
                {
                    attemptCount++;
                    if (tryAction())
                    {
                        success = true;
                    }
                });

                // If succeeded, break early
                sequence.AppendCallback(() =>
                {
                    if (success)
                    {
                        // On success, we can kill the rest of the sequence
                        onSuccess?.Invoke();
                    }
                });

                // Wait a bit if not last attempt (and not yet successful)
                if (i < maxRetries - 1)
                {
                    sequence.AppendInterval(delayBetweenAttempts);
                }

                // We'll stop appending intervals if we succeeded
                sequence.Join
                (
                    DOTween.To(() => 0, x => { }, 0, 0.01f).OnComplete(() =>
                    {
                        if (success)
                        {
                            // Kill the sequence from inside
                            sequence.Complete();
                        }
                    })
                );
            }

            // If we exit the loop (all attempts used) but never succeeded
            sequence.OnComplete(() =>
            {
                if (!success)
                {
                    onFailure?.Invoke();
                }
            });

            return sequence;
        }

        /// <summary>
        /// Performs an action in the next frame.
        /// This is equivalent to DoAfterFrames(1, callback) but provides a more semantic API.
        /// </summary>
        /// <param name="callback"></param>
        public static Tween DoNext(Action callback)
        {
            if (callback is null)
            {
                return null;
            }

            return DoAfterFrames(1, callback);
        }

        /// <summary>
        /// Do an action after a certain number of frames.
        /// </summary>
        /// <param name="frames"></param>
        /// <param name="callback"></param>
        public static Tween DoAfterFrames(int frames, Action callback)
        {
            if (frames <= 0)
            {
                callback?.Invoke();
                return null;
            }

            int frameCount = 0;
            return DOTween.To(() => frameCount, x => frameCount = x, frames, Time.deltaTime * frames)
                .OnComplete(() => callback?.Invoke());
        }

        /// <summary>
        /// Do an action but it's cooled down automatically and won't do anything if it's on cooldown.
        /// Example Usage:
        /// <code>
        /// DoActionWithCooldown(() => Debug.Log("Action!"), 1f, "test"); // everytime this is called, it will only be executable once per second.
        /// </code>
        /// </summary>
        /// <param name="action"></param>
        /// <param name="time"></param>
        /// <param name="key"></param>
        public static void DoActionWithCooldown(Action action, float time, string key, bool unscaled = false)
        {
            if (IsOnCooldown(time, key, unscaled))
            {
                return;
            }

            action?.Invoke();
        }

        /// <summary>
        /// Returns true if a random value (0 to 1) is less than the provided chance.
        /// For example, DoChance(0.3f) has a 30% probability of returning true.
        /// </summary>
        /// <param name="chance01">Probability in range [0, 1].</param>
        public static bool ThrowDice(float chance01)
        {
            if (chance01 <= 0f) return false;
            if (chance01 >= 1f) return true;
            // If you prefer UnityEngine.Random, you can swap out the next line:
            // return UnityEngine.Random.value < chance01;
            return (float)_rng.NextDouble() < chance01;
        }

        private static readonly Dictionary<string, Tween> _cooldowns = new();

        /// <summary>
        /// Return true if the cooldown is not active, and start a cooldown for 't' seconds. key = some random key, can be anything but stay consistent.
        /// Example Usage:
        /// <code>
        /// if (DoCooldown(1f))
        /// {
        ///     Debug.Log("This message will only appear once per second.");
        /// }
        /// </code>
        /// </summary>
        public static bool IsOnCooldown(float t, string key, bool unscaled = false)
        {
            if (t.Zero())
                return false;

            if (!_cooldowns.ContainsKey(key))
            {
                _cooldowns[key] = DoIn(t, () => _cooldowns.Remove(key), unscaled);
                return false;
            }

            // make sure it's not a dead tween
            if (_cooldowns[key] == null || !_cooldowns[key].active)
            {
                _cooldowns.Remove(key);
                return IsOnCooldown(t, key);
            }

            return true;
        }
        
        /// <summary>
        /// Checks if a cooldown gate is open for a specific key and cooldown time. This method is different from `IsOnCooldown` in that it will also set the cooldown if the gate is open.
        /// </summary>
        /// <code>
        /// if (CooldownGate("myKey", 2f))
        /// {
        ///     Debug.Log("Cooldown gate is open, action can be performed.");
        /// }
        /// </code>
        /// <param name="key"></param>
        /// <param name="cooldownTime"></param>
        /// <param name="unscaled"></param>
        /// <returns></returns>
        public static bool CooldownGate(string key, float cooldownTime, bool unscaled = false)
        {
            if (!IsOnCooldown(cooldownTime, key, unscaled))
            {
                DoActionWithCooldown(() => { }, cooldownTime, key, unscaled); // perform an empty action to set the cooldown
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks if there is a cooldown gate closed for a key, and if yes, deletes it so the gate can
        /// be reset.
        /// </summary>
        /// <param name="v"></param>
        public static void KillCooldownGate(string v)
        {
            _cooldowns.TryGetValue(v, out Tween t);
            if (t != null && t.IsActive())
            {
                _cooldowns[v].Kill();
                _cooldowns.Remove(v);
            }
        }

        private static readonly Dictionary<string, Tween> _holdOns = new();

        /// <summary>
        /// Temporarily pauses execution flow by returning true during a delay period, then returning false once complete.
        /// This is like a reversed CooldownGate - it starts closed (returns true) and then opens (returns false).
        /// 
        /// Use this in a method that needs to wait before executing its logic:
        /// - Returns true if still waiting (delay active) - method should return early
        /// - Returns false if delay has passed - method can proceed with execution
        /// 
        /// Example Usage:
        /// <code>
        /// private void AssignPlayer()
        /// {
        ///     if (SIGS.HoldOn(1f, "player_getter_buffer")) return; // Return if still waiting
        ///     
        ///     // This code only executes after the 1 second delay
        ///     var playerPos = SIGS.GetDeadValue&lt;Vector3&gt;("PlayerPosition");
        ///     playerPosition = playerPos;
        /// }
        /// </code>
        /// </summary>
        /// <param name="delayTime">Duration to pause execution in seconds</param>
        /// <param name="key">Unique identifier for this hold operation</param>
        /// <param name="unscaled">Use unscaled time (default: false)</param>
        /// <returns>True if still waiting, false if delay has passed</returns>
        public static bool HoldOn(float delayTime, string key, bool unscaled = false)
        {
            if (delayTime <= 0f)
                return false; // No delay needed, proceed immediately

            // Check if a hold is already active for this key
            if (_holdOns.ContainsKey(key))
            {
                // Verify the tween is still active
                if (_holdOns[key] != null && _holdOns[key].IsActive())
                {
                    return true; // Still waiting
                }
                
                // Tween expired or was killed, clean up and allow execution
                _holdOns.Remove(key);
                return false; // Delay passed, can proceed
            }

            // First call: Start the delay timer
            _holdOns[key] = DoIn(delayTime, () => _holdOns.Remove(key), unscaled);
            return true; // Started waiting
        }

        /// <summary>
        /// Kills an active HoldOn delay, allowing execution to proceed immediately on the next call.
        /// Use this to manually cancel a hold operation before its delay expires.
        /// </summary>
        /// <param name="key">The unique key identifying the HoldOn to kill</param>
        public static void KillHoldOn(string key)
        {
            _holdOns.TryGetValue(key, out Tween t);
            if (t != null && t.IsActive())
            {
                _holdOns[key].Kill();
                _holdOns.Remove(key);
            }
        }
    }
}