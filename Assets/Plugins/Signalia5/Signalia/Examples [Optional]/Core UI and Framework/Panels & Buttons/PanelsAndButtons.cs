using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.UI.Examples
{
    public class PanelsAndButtons : MonoBehaviour
    {
        [SerializeField] private string popUp;
        [SerializeField] private string popUp2;
        [SerializeField] private string swordHud;
        [SerializeField] private float popUpTime;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)
            && !SIGS.IsUIViewVisible("PauseMenu"))
            {
                SIGS.UIViewControl("PauseMenu", true);
            }
        }

        public void ShowPopup()
        {
            popUp.ShowPopUp(popUpTime, false);
        }

        public void ShowPopup2()
        {
            popUp2.ShowPopUp(popUpTime, false);
        }

        public void EquipSword()
        {
            swordHud.ShowMenu();
        }

        public void UnEquipSword()
        {
            swordHud.HideMenu();
        }
    }
}