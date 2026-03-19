using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.SaveSystem.Examples
{
    public class SavingExample : MonoBehaviour
    {
        public Transform playerTransform;
        private float health, mana, stamina;

        private void Awake()
        {
            InitListeners();
        }

        private void Start()
        {
            Load();
        }

        private void InitListeners()
        {
            SIGS.Listener("SaveAction", () =>
            {
                Save();
            });

            // increase listeners
            SIGS.Listener("AddHealth", () =>
            {
                Mathf.Max(health += 25, 100);
                "adj_health".SendEvent(25f);
            });
            SIGS.Listener("AddMana", () =>
            {
                Mathf.Max(mana += 25, 100);
                "adj_mana".SendEvent(25f);
            });
            SIGS.Listener("AddStamina", () =>
            {
                Mathf.Max(stamina += 25, 100);
                "adj_stamina".SendEvent(25f);
            });

            // decrease listeners
            SIGS.Listener("RemoveHealth", () =>
            {
                health = Mathf.Max(health - 25, 0);
                "adj_health".SendEvent(-25f);
            });
            SIGS.Listener("RemoveMana", () =>
            {
                mana = Mathf.Max(mana - 25, 0);
                "adj_mana".SendEvent(-25f);
            });
            SIGS.Listener("RemoveStamina", () =>
            {
                stamina = Mathf.Max(stamina - 25, 0);
                "adj_stamina".SendEvent(-25f);
            });
        }

        private void Load()
        {
            playerTransform.position = SIGS.LoadData("PlayerPosition", "Example/PlayerData", playerTransform.position);
            health = SIGS.LoadData("PlayerHealth", "Example/PlayerData", 100f);
            mana = SIGS.LoadData("PlayerMagicka", "Example/PlayerData", 50f);
            stamina = SIGS.LoadData("PlayerStamina", "Example/PlayerData", 100f);

            // set (true = silent)
            "set_health".SendEvent(health, true);
            "set_mana".SendEvent(mana, true);
            "set_stamina".SendEvent(stamina, true);
        }

        private void Save()
        {
            SIGS.SaveData("PlayerPosition", playerTransform.position, "Example/PlayerData");
            SIGS.SaveData("PlayerHealth", health, "Example/PlayerData");
            SIGS.SaveData("PlayerMagicka", mana, "Example/PlayerData");
            SIGS.SaveData("PlayerStamina", stamina, "Example/PlayerData");
        }

        public void ResetScene()
        {
            SIGS.DeleteSaveFile("Example/PlayerData");
        }
    }
}
