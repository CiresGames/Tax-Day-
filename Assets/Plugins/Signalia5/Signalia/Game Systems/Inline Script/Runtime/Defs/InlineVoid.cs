using System;
using UnityEngine;
using AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    /// <summary>
    /// Use to add a scripting environment. Comes prepared with all common imports. To use, call ExecuteCode.
    /// </summary>
    [Serializable]
    public class InlineVoid : ISB_TopLayer
    {
        public static InlineScriptTypeProfile TypeProfile { get; } =
            InlineScriptTypeProfile.CreateVoidProfile(typeof(ISB_VOID), InlineScriptConstants.VoidBoilerPlateClass);

        protected override InlineScriptTypeProfile GetProfile()
        {
            return TypeProfile;
        }

        /// <summary>
        /// Used from non-MonoBehaviour contexts to execute the inline script. Does not provide a GameObject context.
        /// Do not expect your code to work if it relies on MonoBehaviour features like transform or gameObject.
        /// </summary>
        public void ExecuteNonMono()
        {
            ExecuteBehaviour(behaviour =>
            {
                if (behaviour is ISB_VOID voidBehaviour)
                {
                    voidBehaviour.ExecuteCode();
                }
                else
                {
                    Debug.LogError($"InlineScript: Behaviour '{behaviour.GetType().FullName}' cannot be cast to {typeof(ISB_VOID).FullName}.");
                }
            }, nameof(ExecuteNonMono));
        }

        /// <summary>
        /// Used from MonoBehaviour contexts to execute the inline script with a GameObject context.
        /// Allows you to use MonoBehaviour features like transform or gameObject within the context of the gameobject that is passed.
        /// </summary>
        /// <param name="gameObject"></param>
        public void ExecuteMono(GameObject gameObject)
        {
            ExecuteBehaviour(behaviour =>
            {
                if (behaviour is ISB_VOID voidBehaviour)
                {
                    voidBehaviour.ExecuteCode(gameObject);
                }
                else
                {
                    Debug.LogError($"InlineScript: Behaviour '{behaviour.GetType().FullName}' cannot be cast to {typeof(ISB_VOID).FullName}.");
                }
            }, nameof(ExecuteMono));
        }

        public new ISB_VOID GetPrefabBehaviour()
        {
            return base.GetPrefabBehaviour() as ISB_VOID;
        }
    }
}
