using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using AHAKuo.Signalia.Utilities;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    /// <summary>
    /// Demonstrates PromiseFlow usage with progressive text appending.
    /// </summary>
    public class PromiseflowExample : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        private Promise.PromiseFlow currentFlow;

        public void StartFlow()
        {
            // Dispose any existing flow before starting a new one
            currentFlow?.Dispose();
            
            text.SetText("");
            currentFlow = SIGS.BeginPromiseFlow();
            
            "ButtonView".HideMenu();
            "ButtonView-main".HideMenu();
            
            currentFlow.NQ((s) => { text.text = "Step 1: Starting flow..."; s(); })
                .NQWait(0.5f)
                .NQ((s) => { text.text += "\nStep 2: Processing..."; s(); })
                .NQWait(0.5f)
                .NQ((s) => { text.text += "\nStep 3: Ready for interaction!"; s(); })
                .NQWait(1f)
                .NQNow(() => text.text += "\nInstant step executed!")
                .NQWait(1)
                .NQ((s) =>
                {
                    text.text = "Press the button above to continue...";
                    "ButtonView".ShowMenu();
                    s();
                })
                .NQListen("ButtonPressed")
                .NQ((s) =>
                {
                    "ButtonView".HideMenu();
                    text.text = "Great! Button 1 pressed. Waiting for second button...";
                    "ButtonView".ShowMenu();
                    s();
                })
                .NQListen("ButtonPressed")
                .NQ((s) =>
                {
                    "ButtonView".HideMenu();
                    text.text = "Excellent! Both buttons pressed. Flow continues...";
                    s();
                })
                .NQWait(0.5f)
                .NQ((s) => { text.text += "\nStep 4: Almost done..."; s(); })
                .NQWait(0.3f)
                .NQ((s) =>
                {
                    text.text += "\nFinal step: Press one more time!";
                    "ButtonView".ShowMenu();
                    s();
                })
                .NQListen("ButtonPressed")
                .NQ((s) =>
                {
                    "ButtonView".HideMenu();
                    text.text = "Perfect! Flow complete. All interactions handled.";
                    s();
                })
                .NQ((s) =>
                {
                    "ButtonView-main".ShowMenu();
                    s();
                });
        }
        
        private void OnDestroy()
        {
            // Clean up flow on destroy to ensure listeners are disposed
            currentFlow?.Dispose();
        }
    }
}