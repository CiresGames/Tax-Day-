using AHAKuo.Signalia.Framework;
using DG.Tweening;
using System;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class DoInExample : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI DOIN_Timer;
        [SerializeField] private ParticleSystem DOIN_Particle;
        [SerializeField] private TextMeshProUGUI DOIN_ActiveText;
        Tween timer;

        private void Awake()
        {
            SIGS.DoWhenever(() => timer.IsActive(), DOIN_SetActiveText);
            SIGS.DoWhenever(() => !timer.IsActive(), DOIN_SetInactiveText);
        }

        private void DOIN_SetActiveText()
        {
            DOIN_ActiveText.text = DOIN_ActiveText.text = "Active";
            DOIN_ActiveText.color = Color.green;
        }

        private void DOIN_SetInactiveText()
        {
            DOIN_ActiveText.text = DOIN_ActiveText.text = "Inactive";
            DOIN_ActiveText.color = Color.red;
        }

        public void PlayEffectUnscaledInTime(float t)
        {
            timer?.Kill();
            timer = SIGS.DoIn(t, PlayEffect).OnUpdate(() =>
                DOIN_Timer.text = $"{(int)timer.Elapsed()}:{(timer.Elapsed() % 1) * 1000:000}");
        }

        public void KillTimer()
        {
            timer?.Kill();
        }

        private void PlayEffect()
        {
            DOIN_Particle.Play();
            Debug.Log("DoIn: Particle Played");
        }
    }
}