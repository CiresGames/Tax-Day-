using TMPro;
using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.Examples
{
    public class TextCycler : MonoBehaviour
    {
        public TMP_Text tmpText;
        public string[] lines;

        private int currentLineIndex = 0;

        private void Awake()
        {
            if (lines == null || lines.Length == 0 || tmpText == null) return;

            // Write the first line immediately
            tmpText.text = lines[currentLineIndex];

            // Start the interval loop
            SIGS.DoEveryInterval(GetIntervalForCurrentLine(), () =>
            {
                currentLineIndex = (currentLineIndex + 1) % lines.Length;
                tmpText.text = lines[currentLineIndex];
            });
        }

        private float GetIntervalForCurrentLine()
        {
            return lines[currentLineIndex].Length * 0.05f;
        }
    }
}