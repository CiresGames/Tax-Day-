using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.External
{
    /// <summary>
    /// A ScriptableObject that contains a single InlineVoid script for non-MonoBehaviour execution.
    /// Useful for creating reusable scripts that can be executed from anywhere in your project.
    /// </summary>
    [CreateAssetMenu(fileName = "New Inline Script", menuName = "Inline Script/Inline Script")]
    public class InlineScriptableObject : ScriptableObject
    {
        [SerializeField] private InlineVoid inlineScript;

        /// <summary>
        /// Gets the inline script.
        /// </summary>
        public InlineVoid InlineScript => inlineScript;

        /// <summary>
        /// Executes the inline script using ExecuteNonMono.
        /// </summary>
        public void Execute()
        {
            if (inlineScript == null)
            {
                Debug.LogWarning($"InlineScriptableObject '{name}' has no inline script to execute.");
                return;
            }

            inlineScript.ExecuteNonMono();
        }

        /// <summary>
        /// Sets the inline script.
        /// </summary>
        /// <param name="script">The InlineVoid script to set.</param>
        public void SetScript(InlineVoid script)
        {
            inlineScript = script;
        }

        /// <summary>
        /// Clears the inline script.
        /// </summary>
        public void ClearScript()
        {
            inlineScript = null;
        }
    }
}
