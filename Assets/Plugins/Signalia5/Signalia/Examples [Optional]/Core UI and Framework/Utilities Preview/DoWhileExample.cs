using AHAKuo.Signalia.Framework;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class DoWhileExample : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI DOWHILE_activeText;
        [SerializeField] private ParticleSystem effectParticle;

        private Tween effectLooper;
        private bool isEffectActive = false; // Switch to control the particle effect

        private void Awake()
        {
            // Update UI when effect starts/stops
            SIGS.DoWhenever(() => effectLooper.IsActive(), DOWHILE_SetActiveText);
            SIGS.DoWhenever(() => !effectLooper.IsActive(), DOWHILE_SetInactiveText);
        }

        private void DOWHILE_SetActiveText()
        {
            DOWHILE_activeText.text = "Active";
            DOWHILE_activeText.color = Color.green;
        }

        private void DOWHILE_SetInactiveText()
        {
            DOWHILE_activeText.text = "Inactive";
            DOWHILE_activeText.color = Color.red;
        }

        public void SetEffectActive(bool y)
        {
            isEffectActive = y;
        }

        public void StartEffectLoop()
        {
            effectLooper?.Kill();

            effectLooper = SIGS.DoWhile(
                condition: () => isEffectActive, // Keep running while the switch is active
                action: PlayEffect,
                waitTimeAfterLock: 0, // No delay after stopping
                locker: () => !isEffectActive, // Stop when switch is turned off
                debugSteps: true
            );
        }

        public void StopEffectLoop()
        {
            effectLooper?.Kill();
        }

        private void PlayEffect()
        {
            effectParticle.Play();
            Debug.Log("DoWhile: Playing Particle Effect");
        }
    }
}