using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// A simple component to add notes to GameObjects in the Inspector. Can be used however. Literally just a sophisticated string.
    /// </summary>
    [AddComponentMenu("Signalia/Utilities/Signalia | Notes")]
    public class Notes : MonoBehaviour
    {
        /// <summary>
        /// The notes content. The TextArea attribute provides basic multi-line editing
        /// even if the custom editor is not present, and hints to the custom editor.
        /// </summary>
        [SerializeField, TextArea(10, 20)] // Increased TextArea size slightly
        private string noteContent = "Enter your notes here...";
        public string NoteContent => noteContent;

//        /// <summary>
//        /// If true, this component will attempt to move itself to the top
//        /// of the component list when added or reset.
//        /// </summary>
//        [SerializeField]
//        private bool moveToTop = false;

//        public bool ShouldMoveToTop => moveToTop; // Public getter if needed

//#if UNITY_EDITOR
//        private void Reset()
//        {
//            // Use CallDelayed to ensure the component is fully added before moving
//            EditorApplication.delayCall += () =>
//            {
//                // Check if the component still exists (might be removed quickly)
//                if (this == null || !this.gameObject) return;

//                if (moveToTop)
//                {
//                    // Keep moving up until it's at the top
//                    while (ComponentUtility.MoveComponentUp(this)) { }
//                }
//            };
//        }
//#endif << work in progress
    }
}