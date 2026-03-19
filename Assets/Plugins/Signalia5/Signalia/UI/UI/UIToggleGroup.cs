using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    [AddComponentMenu("Signalia/UI/Signalia | UI Toggle Group")]
    public class UIToggleGroup : MonoBehaviour
    {
        [SerializeField] private List<UIButton> buttons = new();

        [SerializeField] private int maxSelected = 1;

        [SerializeField] private bool allChildren = false;

        private bool maxSelectedIsOne => maxSelected == 1;

        [SerializeField] private bool alwaysOneSelected = false;

        [SerializeField] private bool initialSelection = false;

        [SerializeField] private int[] indexes = new[] {0};

        private bool ValidInitialSelection => (maxSelected == indexes.Length) && initialSelection;

        public bool AlwaysOneSelected => maxSelected == 1 && alwaysOneSelected;

        public delegate void ToggleInGroup();
        public event ToggleInGroup OnToggleInGroup;

        public void SetMaxLimit(int i)
        {
            maxSelected = i;
        }

        public UIButton[] CurrentSelections()
        {
            return buttons.Where(button => button.Toggle_IsOn).ToArray();
        }

        private void GetChildren()
        {
            if (allChildren)
            {
                // get all children
                buttons.Clear();
                buttons.AddRange(GetComponentsInChildren<UIButton>());
                return;
            }

            buttons.Clear();
            foreach (Transform child in transform)
            {
                if (child.TryGetComponent<UIButton>(out var button))
                {
                    buttons.Add(button);
                }
            }
        }

        private void Awake()
        {
            foreach (UIButton button in buttons)
            {
                button.AssignGroup(this);
            }
        }

        private void Start()
        {
            if (initialSelection)
            {
                foreach (int index in indexes)
                {
                    ManualSelect(index);
                }
            }
        }

        /// <summary>
        /// Look for new buttons in the group.
        /// </summary>
        public void RefreshGroup()
        {
            GetChildren();
            foreach (UIButton button in buttons)
            {
                button.AssignGroup(this);
            }
        }

        public void ManualSelect(int index)
        {
            if (index < 0 || index >= buttons.Count)
            {
                Debug.LogError("Index out of range.");
                return;
            }

            buttons[index].SetToggleWithoutNotify(true);
        }

        public void NotifyToggle(UIButton uIButton)
        {
            // Ensure the button list is up-to-date
            RefreshGroup();

            // If maxSelected is 1, disable all other buttons when toggling
            if (maxSelected == 1)
            {
                foreach (UIButton button in buttons)
                {
                    if (button != uIButton)
                    {
                        button.SetToggleWithoutNotify(false);
                    }
                }
            }
            else
            {
                // Collect all currently selected buttons
                List<UIButton> selectedButtons = buttons.Where(button => button.Toggle_IsOn).ToList();

                // If the number of selected buttons exceeds maxSelected, deselect the oldest toggled button(s)
                if (selectedButtons.Count > maxSelected)
                {
                    foreach (UIButton button in selectedButtons.Take(selectedButtons.Count - maxSelected))
                    {
                        button.SetToggleWithoutNotify(false);
                    }
                }
            }

            OnToggleInGroup?.Invoke(); // let subscribers know that a button has been toggled in the group
        }

        public List<UIButton> Buttons => buttons;
    }
}