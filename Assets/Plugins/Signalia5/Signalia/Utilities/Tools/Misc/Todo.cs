using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// A simple component to attach TODO notes to GameObjects.
    /// </summary>
    [Icon("Assets/AHAKuo Creations/Signalia/Framework/Graphics/Icons/SIGS_EDITOR_ICON_TODO.png")]
    [AddComponentMenu("Signalia/Utilities/Signalia | Todo")]
    public class Todo : MonoBehaviour
    {
        [SerializeField, TextArea(4, 12)]
        private string description = "Describe the todo item...";

        public string Description => description;
    }
}
