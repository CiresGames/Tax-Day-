using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Signalia/UI/Signalia | UI Element")]
    /// <summary>
    /// A class to mark and use UI elements in Signalia.
    /// </summary>
    public class UIElement : MonoBehaviour
    {
        public RectTransform RectTransform { get; private set; }
        [SerializeField] private string elementName;
        public string ElementName => elementName;
        public void GenerateUniqueElementName()
        {
            if (string.IsNullOrEmpty(elementName))
            {
                elementName = gameObject.name;
            }
        }

        private void Awake()
        {
            RectTransform = GetComponent<RectTransform>();

            RuntimeValues.TrackedValues.LogElementRegistry(this);
        }

        private void OnDestroy()
        {
            RuntimeValues.TrackedValues.LogRemoveElementRegistry(this);
        }
    }
}