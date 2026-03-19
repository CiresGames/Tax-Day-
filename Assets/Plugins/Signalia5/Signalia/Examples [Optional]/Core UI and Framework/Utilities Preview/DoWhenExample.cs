using AHAKuo.Signalia.Framework;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class DoWhenExample : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI DOWHEN_healthText;
        [SerializeField] private TextMeshProUGUI DOWHEN_activeText;
        Tween healthChecker;
        int Health = 25;

        private void Awake()
        {
            SIGS.DoWhenever(() => healthChecker.IsActive(), DOWHEN_SetActiveText);
            SIGS.DoWhenever(() => !healthChecker.IsActive(), DOWHEN_SetInactiveText);
        }

        private void DOWHEN_SetActiveText()
        {
            DOWHEN_activeText.text = DOWHEN_activeText.text = "Active";
            DOWHEN_activeText.color = Color.green;
        }

        private void DOWHEN_SetInactiveText()
        {
            DOWHEN_activeText.text = DOWHEN_activeText.text = "Inactive";
            DOWHEN_activeText.color = Color.red;
        }

        public void DOWHEN_DoWhenInit()
        {
            healthChecker?.Kill();

            healthChecker = SIGS.DoWhen(() => Health == 0, Refill);

            Debug.Log("DoWhen: Health Checker Initialized");
        }

        public void DOWHEN_DoWhenKill()
        {
            healthChecker?.Kill();

            Debug.Log("DoWhen: Health Checker Killed");
        }

        public void DOWHEN_DamageHealth()
        {
            if (Health == 0) return;
            Health -= 5;
            DOWHEN_healthText.text = $"{Health}/25";

            Debug.Log("DoWhen: Health Damaged");
        }

        private void Refill()
        {
            Health = 25;
            DOWHEN_healthText.text = $"{Health}/25";
            Debug.Log("DoWhen: Health Refilled and checker killed");
        }
    }
}
