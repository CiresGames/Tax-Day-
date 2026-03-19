using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Utilities.SIGInput
{
    /// <summary>
    /// Inherit this class to create a custom input wrapper which will use Signalia's action mapping. You can make use of this to disable/enable or have granular control over input actions.
    /// This wrapper is just a glorified Update method that executes on -100 order. So you can make your own without inheriting this so long as it is on [DefaultExecutionOrder(-100)]
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public abstract class SignaliaInputWrapper : InstancerSingleton<SignaliaInputWrapper>
    {
        /// <summary>
        /// At least one instance is true.
        /// </summary>
        public static bool Exists => Instance != null;

        /// <summary>
        /// Add your input system calls here and forward them using SIGS.Pass...(). Make sure to forward all Down, Held, and Up states, or it won't work.
        /// Will take some effort and boilerplate to set up.
        /// Todo: create inherent wrappers for common input systems out there: Rewired, Unity New Input System. So the developer doesn't need to make them.
        /// </summary>
        protected abstract void PollInput();

        private void Update()
        {
            PollInput();
        }
    }
}
