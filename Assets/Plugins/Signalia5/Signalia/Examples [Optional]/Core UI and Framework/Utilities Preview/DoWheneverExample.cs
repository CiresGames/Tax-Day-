using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Utilities;
using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class DoWheneverExample : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI DOWHENEVER_activeText;
        [SerializeField] private TextMeshProUGUI DOWHENEVER_healthText;
        [SerializeField] private TextMeshProUGUI DOWHENEVER_alertText;
        [SerializeField] private ParticleSystem lowHealthEffect;

        private int Health = 25;
        private int MaxHealth = 25;
        private int LowHealthThreshold = 10;
        private List<Tween> tweens = new();

        private void Awake()
        {
            DOWHENEVER_alertText.gameObject.SetActive(false);

            // Update UI when effect starts/stops
            SIGS.DoWhenever(() => tweens.HasValue() && tweens.All(x => x.active), DOWHILE_SetActiveText);
            SIGS.DoWhenever(() => tweens.Empty() || tweens.All(x => !x.active), DOWHILE_SetInactiveText);
        }

        private void DOWHILE_SetActiveText()
        {
            DOWHENEVER_activeText.text = "Active";
            DOWHENEVER_activeText.color = Color.green;
        }

        private void DOWHILE_SetInactiveText()
        {
            DOWHENEVER_activeText.text = "Inactive";
            DOWHENEVER_activeText.color = Color.red;
        }

        public void StartTweens()
        {
            KillTweens();

            // Trigger alert effect whenever health is low, store it to kill later
            tweens.Add(SIGS.DoWhenever(() => Health < LowHealthThreshold, TriggerLowHealthEffect));

            // Remove alert when health is back to normal, store it to kill later
            tweens.Add(SIGS.DoWhenever(() => Health >= LowHealthThreshold, StopLowHealthEffect));
        }

        public void KillTweens()
        {
            if (tweens == null) return;

            foreach (var tween in tweens)
            {
                tween.Kill();
            }

            tweens.Clear();
        }

        public void TakeDamage()
        {
            if (Health <= 0) return;
            Health -= 5;
            Health = Mathf.Max(0, Health); // Ensure health doesn't go negative
            DOWHENEVER_healthText.text = $"{Health}/{MaxHealth}";

            Debug.Log("DoWhenever: Player took damage.");
        }

        public void Heal()
        {
            Health += 5;
            Health = Mathf.Min(Health, MaxHealth); // Ensure health doesn't exceed max
            DOWHENEVER_healthText.text = $"{Health}/{MaxHealth}";

            Debug.Log("DoWhenever: Player healed.");
        }

        private void TriggerLowHealthEffect()
        {
            DOWHENEVER_alertText.gameObject.SetActive(true);
            DOWHENEVER_alertText.text = "Warning: Low Health!";
            DOWHENEVER_alertText.color = Color.red;
            lowHealthEffect.Play();

            Debug.Log("DoWhenever: Low health warning triggered!");
        }

        private void StopLowHealthEffect()
        {
            DOWHENEVER_alertText.gameObject.SetActive(false);
            DOWHENEVER_alertText.text = "";
            lowHealthEffect.Stop();

            Debug.Log("DoWhenever: Low health warning removed.");
        }
    }
}