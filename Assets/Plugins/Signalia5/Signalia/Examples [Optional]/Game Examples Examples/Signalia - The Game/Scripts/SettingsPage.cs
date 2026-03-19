using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace AHAKuo.Signalia.Examples.SignaliaTheGame
{
    public class SettingsPage : MonoBehaviour
    {
        [Serializable]
        private struct AudioSlider
        {
            public Slider slider;
            public AudioMixerGroup audioMixerGroup;
        }

        [SerializeField] private List<AudioSlider> audioSliders = new();

        private UIView view;
        private Listener setSettings;

        private void Awake()
        {
            view = GetComponentInParent<UIView>();
            view.OnShowStart += SetApplyMethod;
            view.OnHideStart += RemoveApplyMethod;
        }

        private void Start()
        {
            audioSliders.ForEach(a => a.slider.onValueChanged.AddListener(v => Effector.UpdateMixerGroup(a.audioMixerGroup, v)));
        }

        private void LoadAudio()
        {
            var loaded = Effector.CurrentVolumes();

            // set the sliders to the loaded values based on mixer group
            foreach (var l in loaded)
            {
                audioSliders.Where(a => a.audioMixerGroup == l.mixerGroup)
                    .ToList()
                    .ForEach(a =>
                    {
                        a.slider.value = l.vol;
                    });
            }
        }

        private void RemoveApplyMethod()
        {
            setSettings?.Dispose();
        }

        private void SetApplyMethod()
        {
            LoadAudio();
            setSettings = new("ApplySettings", SaveSettings, true);
        }

        public void SaveSettings()
        {
            Effector.SaveMixerSettings();

            SIGS.ShowPopUp("SettingsSaved", 1, true);
        }
    }
}