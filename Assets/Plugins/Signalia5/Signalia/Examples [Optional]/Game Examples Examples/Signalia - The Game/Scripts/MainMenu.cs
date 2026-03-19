using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using DG.Tweening;
using UnityEngine;

namespace AHAKuo.Signalia.Examples.SignaliaTheGame
{
    public class MainMenu : MonoBehaviour
    {
        public const string reviewUrl = "https://assetstore.unity.com/packages/slug/311320";

        private void Awake()
        {
            SIGS.Listener("Open Review Page", ReviewPageOpen);
            SIGS.Listener("GoBackToStarter", GoBackToStarter, true);
        }

        private void GoBackToStarter()
        {
            // fade out
            "Main Menu".StopAudio(true, 1f);
            // fade black
            "Black Fade".ShowMenu();
            SIGS.DoIn(1, () => SIGS.LoadSceneAsync("Start Here"));
        }

        private void ReviewPageOpen()
        {
            Application.OpenURL(reviewUrl);
        }

        private void Start()
        {
            var sequence = DOTween.Sequence();

            sequence.Append(SIGS.DoIn(1, () => SIGS.UIViewControl("Black Fade", false)))
            .Append(SIGS.DoIn(1, () => SIGS.UIViewControl("Main Menu", true)))
            .Append(SIGS.DoIn(1, () => SIGS.UIViewControl("HUD", true)))
            .Append(SIGS.DoIn(1, () => 
            "Main Menu".PlayAudio(new FadeIn(2))));

            sequence.Play();
        }
    }
}
