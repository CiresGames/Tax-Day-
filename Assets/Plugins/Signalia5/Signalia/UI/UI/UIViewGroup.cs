using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    [AddComponentMenu("Signalia/UI/Signalia | UI View Group")]
    public class UIViewGroup : MonoBehaviour
    {
        [SerializeField] private List<UIView> views = new();

        private void GetChildren()
        {
            views.Clear();
            foreach (Transform child in transform)
            {
                UIView view = child.GetComponent<UIView>();
                if (view != null)
                {
                    views.Add(view);
                }
            }
        }

        private void Awake()
        {
            foreach (UIView view in views)
            {
                view.AssignGroup(this);
            }
        }

        public List<UIView> Views => views;
    }
}