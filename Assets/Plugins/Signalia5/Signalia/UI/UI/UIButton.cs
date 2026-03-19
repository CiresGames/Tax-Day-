using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Reflection;
using System;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Framework;
using AHAKuo.Signalia.Radio;
using TMPro;
using System.Linq;

namespace AHAKuo.Signalia.UI
{
    /// <summary>
    /// Adds more functionality to the button.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    [AddComponentMenu("Signalia/UI/Signalia | UI Button")]
    public class UIButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler, IPointerExitHandler, IDeselectHandler
    {
        private UnityEngine.UI.Button unityButton;

        [SerializeField] private string buttonName;
        public string ButtonName => buttonName;
        public string GenerateButtonName()
        {
            return gameObject.name;
        }

        [SerializeField] private UIAnimationAsset clickAnimation;

        [SerializeField] private UIAnimationAsset hoverAnimation;

        [SerializeField] private UIAnimationAsset unhoverAnimation;

        [SerializeField] private UIAnimationAsset selectAnimation;

        [SerializeField] private UIAnimationAsset deselectAnimation;

        public UIAnimationAsset ClickAnimationAsset => clickAnimation;
        public UIAnimationAsset HoverAnimationAsset => hoverAnimation;
        public UIAnimationAsset UnhoverAnimationAsset => unhoverAnimation;
        public UIAnimationAsset SelectAnimationAsset => selectAnimation;
        public UIAnimationAsset DeselectAnimationAsset => deselectAnimation;

        [SerializeField] private bool disableWhileHovering;

        [SerializeField] private bool disableWhileAnimating;

        [SerializeField] private bool disableWhileSelecting;

        [SerializeField] private bool disableWithTime;

        [SerializeField] private float disableTime = 0.5f;

        [SerializeField] private bool actionsAfterAnimation = false;

        [SerializeField] private bool hideParentView;
        public void SetHideParentView(bool hide)
        {
            hideParentView = hide;
        }

        [SerializeField] private string[] menusToShow = new string[0]; // this avoids null reference exceptions
        [SerializeField] private string[] menusToShowAsPopUp = new string[0]; // this avoids null reference exceptions
        [SerializeField] private string[] menusToHide = new string[0]; // this avoids null reference exceptions

        [SerializeField] private string interactiblityBinding;
        [SerializeField] private float popUpHideDelay = 0.5f;

        [SerializeField] private bool useToggling;
        [SerializeField] private string savingKey;
        [SerializeField] private bool autoSaveToggle;
        [SerializeField] private bool autoLoadToggle;
        [SerializeField] private string eventOnLoadToggle;
        [SerializeField] private string toggleEventSender;

        private UIToggleGroup toggleGroup;
        public void AssignGroup(UIToggleGroup grp) => toggleGroup = grp;

        [SerializeField] private Image toggleCheckImage;

        [SerializeField] private bool changeToggleSprite;

        [SerializeField] private Sprite toggleOnSprite;

        [SerializeField] private Sprite toggleOffSprite;

        [SerializeField] private UnityEvent<bool> toggleEvent;

        [SerializeField] private UnityEvent toggleEventOn;

        [SerializeField] private UnityEvent toggleEventOff;

        private void UpdateToggleGraphic() 
        { 
            if (toggleCheckImage != null)
            {
                if (changeToggleSprite)
                {
                    toggleCheckImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;
                }
                else
                {
                    toggleCheckImage.enabled = isOn;
                }
            }
        }

        [SerializeField] private bool isOn;
        public bool Toggle_IsOn => isOn;

        private bool inited;
        private bool disabledWithTime;
        public Button _button => unityButton;
        private List<Action> actions = new();
        private CanvasGroup canvasGroup;
        private UIView parentView;
        public RectTransform RectTransform { get; private set; }

        // Track hover and select states for animation restoration
        private bool _isHovered;
        private bool _isSelected;

        /// <summary>
        /// A helpful field to store a value that can be assigned to this button so we can retrieve it during things like toggle lists where a toggle represents a value.
        /// </summary>
        public object assignedValue;

        /// <summary>
        /// Enabled while the button is selected.
        /// </summary>
        [SerializeField] private GameObject selectionObject;
        [SerializeField] private bool selectionAffectedByHover;

        [SerializeField] private bool treatHoverAsSelection;

        #region Init Params
        private List<(Graphic graphic, Color original, Action dimOper, Action undimOper)> _cachedVisuals = new();
        #endregion

        public void Reinitialize()
        {
            unityButton.onClick.RemoveAllListeners();
            inited = false;
            actions.Clear();
            OnEnable();
        }

        public void ClearActions()
        {
            actions.Clear();
        }

        public void ClearUnityEvents()
        {
            unityButton.onClick.RemoveAllListeners();
        }

        private void InitializeAnimations()
        {
            if (clickAnimation != null)
            {
                clickAnimation = clickAnimation.CreateInstance();
            }

            if (hoverAnimation != null)
            {
                hoverAnimation = hoverAnimation.CreateInstance();
            }

            if (unhoverAnimation != null)
            {
                unhoverAnimation = unhoverAnimation.CreateInstance();
            }

            if (selectAnimation != null)
            {
                selectAnimation = selectAnimation.CreateInstance();
            }

            if (deselectAnimation != null)
            {
                deselectAnimation = deselectAnimation.CreateInstance();
            }
        }

        private void CacheInteractionVisuals()
        {
            // get all possible color containing graphics under this button that usually are not dimmed.
            // TMPro text, and images
            var graphics = GetComponentsInChildren<Graphic>(true);

            if (graphics == null || graphics.Length == 0) { return; }

            if (graphics.Length >= 2)
            {
                graphics = graphics.Where(g => g is TMP_Text || g is Image || g is RawImage || g is Text).ToArray();

                if (graphics.Length == 0) { return; }

                foreach (var graphic in graphics)
                {
                    // cache the original color
                    var originalColor = graphic.color;
                    // create dim and undim operations
                    Action dimOper = () => graphic.color = new Color(originalColor.r, originalColor.g, originalColor.b, canvasGroupFade);
                    Action undimOper = () => graphic.color = originalColor;
                    _cachedVisuals.Add((graphic, originalColor, dimOper, undimOper));
                }
            }
        }

        private void Awake()
        {
            // init rect transform
            RectTransform = GetComponent<RectTransform>();

            // log button name
            RuntimeValues.TrackedValues.LogButtonRegistry(this);

            // init anim
            InitializeAnimations();

            // init grouping
            if (useToggling)
            {
                toggleGroup = GetComponentInParent<UIToggleGroup>();
            }

            // init canvas group
            canvasGroup = GetComponent<CanvasGroup>();

            // init parent view
            parentView = GetComponentInParent<UIView>();

            // disable selection object
            if (selectionObject != null)
            {
                selectionObject.SetActive(false);
            }

            // cache interaction visuals for smart dimming
            if (canvasGroup == null)
            {
                CacheInteractionVisuals();
            }

            // disable button blockers
            if (ConfigReader.GetConfig().DisableButtonBlockers_TMPText)
            {
                var tmpTexts = GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmpText in tmpTexts)
                {
                    // disable button blockers
                    tmpText.raycastTarget = false;
                }
            }
        }

        private void Start()
        {
            // binding
            if (!string.IsNullOrEmpty(interactiblityBinding))
            {
                this.Bind(() => unityButton.interactable, () => SimpleRadio.ReceiveLiveKeyValue<bool>(interactiblityBinding));
            }

            // load toggle state
            if (useToggling && autoLoadToggle)
            {
                var loaded = PlayerPrefs.GetInt(savingKey, 0) == 1;
                SetToggleWithoutNotify(loaded);
                UpdateToggleGraphic();
                if (eventOnLoadToggle != null)
                {
                    SIGS.Send(eventOnLoadToggle, loaded);
                }
            }
        }

        private void Update()
        {
            SetInteractionVisuals();
            UpdateToggleGraphic();
        }

        private void SetInteractionVisuals()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = unityButton.interactable ? 1 : canvasGroupFade;
            }
            else
            {
                // if we do not have a canvas group, we will dim the visuals ourselves
                foreach (var graphic in _cachedVisuals)
                {
                    if (unityButton.interactable)
                    {
                        graphic.undimOper();
                    }
                    else
                    {
                        graphic.dimOper();
                    }
                }
            }
        }

        #region Enabling
        /// <summary>
        /// Sets the button enable state to a new one.
        /// </summary>
        public void SetEnabled(bool newStatus)
        {
            unityButton.interactable = newStatus;
        }

        public void EnableAfterTime(float time)
        {
            SSUtility.DoIn(time, () => SetEnabled(true));
        }

        public void DisableAfterTime(float time)
        {
            SSUtility.DoIn(time, () => SetEnabled(false));
        }
        #endregion

        #region Runtime Action Changing
        /// <summary>
        /// Adds a new action to the button.
        /// </summary>
        /// <param name="unityAction"></param>
        public void AddNewAction(Action unityAction)
        {
            actions.Add(unityAction);
        }

        /// <summary>
        /// Overrides the button action with a new one.
        /// </summary>
        /// <param name="unityAction"></param>
        public void SetAction(Action unityAction)
        {
            actions.Clear();
            AddNewAction(unityAction);
        }

        public void SetEventSent(string newSender)
        {
            eventSenders = new[] { newSender };
        }
        
        /// <summary>
        /// Overrides sent signalia events with new ones.
        /// </summary>
        /// <param name="newSenders"></param>
        public void SetEventSenders_Click(string[] newSenders)
        {
            eventSenders = newSenders;
        }
        #endregion

        /// <summary>
        /// Takes away the default button event and places it on this UIButton. Useful for turning off the default button event.
        /// </summary>
        public void OvertakeDefaultButtonEvent()
        {
            var c = unityButton.onClick.GetPersistentEventCount();

            for (int i = 0; i < c; i++)
            {
                // disable it on the button itself
                unityButton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);

                var target = unityButton.onClick.GetPersistentTarget(i);
                var method = unityButton.onClick.GetPersistentMethodName(i);

                // retrieve method info
                MethodInfo methodInfo = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (methodInfo != null)
                {
                    // determine if method has parameters
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    UnityAction action;

                    if (parameters.Length == 0)
                    {
                        // no parameters
                        action = (UnityAction)Delegate.CreateDelegate(typeof(UnityAction), target, methodInfo);
                    }
                    else
                    {
                        // with parameters
                        action = () =>
                        {
                            // construct parameter array with default values or specific ones if you have them
                            object[] parameterValues = new object[parameters.Length];
                            for (int j = 0; j < parameters.Length; j++)
                            {
                                parameterValues[j] = GetDefaultValue(parameters[j].ParameterType);
                            }

                            methodInfo.Invoke(target, parameterValues);
                        };
                    }

                    AddNewAction(() => action?.Invoke());
                }
            }

            static object GetDefaultValue(System.Type type)
            {
                if (type.IsValueType)
                {
                    return System.Activator.CreateInstance(type);
                }
                return null;
            }
        }

        private void OnEnable()
        {
            if (inited) { return; }

            inited = true;

            unityButton = GetComponentInChildren<UnityEngine.UI.Button>();

            if (unityButton != null) 
            { 
                unityButton.onClick.AddListener(ButtonAction); 
            }
        }

        private void OnDisable()
        {
            // Reset hover and selection states
            _isHovered = false;
            _isSelected = false;
            
            // Disable selection object
            if (selectionObject != null)
            {
                selectionObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            RuntimeValues.TrackedValues.LogRemoveButtonRegistry(this);
        }

        public void PerformClick()
        {
            ButtonAction();
        }

        public void PerformSelect()
        {
            OnSelect(null);
        }

        public void PerformHover()
        {
            OnPointerEnter(null);
        }

        public void SetInteractive(bool inter)
        {
            unityButton.interactable = inter;
        }

        private void ButtonAction()
        {
            if (!ButtonCanBeClicked())
            {
                return;
            }

            if (!useToggling && !(toggleOnAudio.HasValue() || toggleOffAudio.HasValue()))
            {
                SIGS.PlayAudio(clickAudio);
                if (clickHaptics.Enabled)
                    SIGS.TriggerHaptic(clickHaptics);
            }

            DoActions();
            ClickAnimation();
            EventSend();
            DoUnityEventOnClick();
            DoToggleSwitch();

            // save toggle
            if (useToggling && autoSaveToggle)
            {
                PlayerPrefs.SetInt(savingKey, isOn ? 1 : 0);
                PlayerPrefs.Save();
            }

            InvokeMenus();

            if (invokeBackButton)
            {
                SIGS.Clickback();
            }

            RuntimeValues.Config.UIButtonClicked.SendEvent(gameObject);

            RuntimeValues.UIConfig.CoolDownButtons();

            if (disableWithTime)
            {
                disabledWithTime = true;
                SSUtility.DoIn(disableTime, () => { disabledWithTime = false; });
            }

            // invoke clickanywhere
            RuntimeValues.UIConfig.InvokeClickAnywhere();
        }
        
        public void SetToggleWithoutNotify(bool n)
        {
            isOn = n;
        }

        private void DoToggleSwitch()
        {
            if (!useToggling) return;

            if (toggleGroup != null)
            {
                // if non optional group, do not allow toggling off
                var alwaysOneSelected = toggleGroup.AlwaysOneSelected;

                if (Toggle_IsOn && alwaysOneSelected)
                {
                    return;
                }
            }

            isOn = !isOn;
            toggleEvent?.Invoke(isOn);
            if (useToggling && !string.IsNullOrEmpty(toggleEventSender))
            {
                toggleEventSender.SendEvent(isOn);
            }
            if (isOn) {
                toggleEventOn?.Invoke();
                SIGS.PlayAudio(toggleOnAudio);
                if (toggleOnHaptics.Enabled)
                    SIGS.TriggerHaptic(toggleOnHaptics);
            }
            if (!isOn) { 
                toggleEventOff?.Invoke(); 
                SIGS.PlayAudio(toggleOffAudio);
                if (toggleOffHaptics.Enabled)
                    SIGS.TriggerHaptic(toggleOffHaptics);
            }
            
            // if we are in a group, notify the group
            if (toggleGroup != null)
            {
                toggleGroup.NotifyToggle(this);
            }
        }
        
        private bool ParentViewHidden => 
            parentView != null && (parentView.IsHidden || parentView.IsHiding);

        private bool ButtonCanBeClicked(bool locally = false)
        {
            if (!RuntimeValues.UIConfig.ButtonsCanBeClicked
                && !locally)
            {
                return false;
            }

            // smart check if parent view is hidden
            if (ParentViewHidden)
            {
                return false;
            }

            if (disableWithTime
                && disabledWithTime)
            {
                return false;
            }


            if (disableWhileAnimating && clickAnimation != null && clickAnimation.Performing && !clickAnimation.HasAnInfiniteLoop())
            {
                return false;
            }

            if (disableWhileHovering && hoverAnimation != null && hoverAnimation.Performing && !hoverAnimation.HasAnInfiniteLoop())
            {
                return false;
            }

            if (disableWhileSelecting && selectAnimation != null && selectAnimation.Performing && !selectAnimation.HasAnInfiniteLoop())
            {
                return false;
            }

            if (!unityButton.interactable) { return false; }

            return true;
        }

        private void DoActions()
        {
            actions.ForEach(x => x());
        }

        public void ClickAnimation()
        {
            if (ParentViewHidden){return;}

            if(clickAnimation!= null)
            {
                // Store current hover and select states before playing click animation
                bool wasHovered = _isHovered;
                bool wasSelected = _isSelected;

                clickAnimation.PerformAnimation(() =>
                {
                    // After click animation completes, restore hover or select animation if still applicable
                    RestoreHoverOrSelectAnimation(wasHovered, wasSelected);
                }, this.gameObject, false);
            }
            else
            {
                // If no click animation, still restore hover/select states
                RestoreHoverOrSelectAnimation(_isHovered, _isSelected);
            }
        }
        
        /// <summary>
        /// Only applicable for animations that have infinite loops.
        /// </summary>
        /// <param name="wasHovered"></param>
        /// <param name="wasSelected"></param>
        private void RestoreHoverOrSelectAnimation(bool wasHovered, bool wasSelected)
        {
            if (ParentViewHidden) { return; }

            // Restore select animation if button is still selected and was selected before click
            if (wasSelected && _isSelected && selectAnimation != null && selectAnimation.HasAnInfiniteLoop())
            {
                SelectAnimation();
            }
            // Restore hover animation if button is hovered and not selected (select takes priority)
            else if (wasHovered && _isHovered && hoverAnimation != null && hoverAnimation.HasAnInfiniteLoop())
            {
                HoverAnimation();
            }
        }

        public void HoverAnimation()
        {
            if (ParentViewHidden) {return;}

            if (hoverAnimation != null)
            {
                hoverAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        public void UnhoverAnimation()
        {
            if (ParentViewHidden) {return;}

            if (unhoverAnimation != null)
            {
                unhoverAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        public void SelectAnimation()
        {
            if (ParentViewHidden){return;}

            if (selectAnimation != null)
            {
                selectAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        public void DeselectAnimation()
        {
            if (ParentViewHidden){return;}


            if (deselectAnimation != null)
            {
                deselectAnimation.PerformAnimation(null, this.gameObject, false);
            }
        }

        public void EventSend()
        {
            if (actionsAfterAnimation && clickAnimation != null)
            {
                SSUtility.DoIn(clickAnimation.FullEndTime(), () =>
                {
                    foreach (var ev in eventSenders)
                    {
                        SimpleRadio.SendEventByContext(ev, gameObject);
                    }
                });
                return;
            }

            if (eventSenders == null) { return; }

            foreach (var ev in eventSenders)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }
        }

        public void InvokeMenus()
        {
            foreach (var item in menusToShow)
            {
                item.ShowMenu();
            }

            foreach (var item in menusToShowAsPopUp)
            {
                item.ShowPopUp(popUpHideDelay, false);
            }

            foreach (var item in menusToHide)
            {
                item.HideMenu();
            }

            if (hideParentView && parentView != null)
            {
                parentView.Hide();
            }
        }

        #region Events

        private void DoEvent()
        {
            foreach (var effect in eventSenders)
            {
                SimpleRadio.SendEventByContext(effect, gameObject);
            }
        }

        [SerializeField] private string[] eventSenders = new string[0]; // this avoids null reference exceptions
        #endregion

        #region Signalia Tools
        [SerializeField] private bool invokeBackButton = false;
        [SerializeField] private float canvasGroupFade = 0.5f;
        #endregion

        #region Unity Events
        private void DoUnityEventOnClick()
        {
            if (unityEventAfterClickAnimation && clickAnimation != null)
            {
                SSUtility.DoIn(clickAnimation.FullEndTime(), () =>
                {
                    unityEventOnClick?.Invoke();
                });
                return;
            }

            unityEventOnClick?.Invoke();
        }

        public UnityEvent unityEventOnClick;
        [SerializeField] private bool unityEventAfterClickAnimation = false;

        private void DoUnityEventOnUnhover()
        {
            if (unityEventAfterHoverAnimation && unhoverAnimation != null)
            {
                SSUtility.DoIn(unhoverAnimation.FullEndTime(), () =>
                {
                    unityEventOnUnhover?.Invoke();
                });
                return;
            }

            unityEventOnUnhover?.Invoke();
        }

        public UnityEvent unityEventOnUnhover;
        [SerializeField] private bool unityEventAfterHoverAnimation = false;

        private void DoUnityEventOnHover()
        {
            if (unityEventAfterHoverAnimation && hoverAnimation != null)
            {
                SSUtility.DoIn(hoverAnimation.FullEndTime(), () =>
                {
                    unityEventOnHover?.Invoke();
                });
                return;
            }

            unityEventOnHover?.Invoke();
        }

        public UnityEvent unityEventOnHover;

        private void DoUnityEventOnSelect()
        {
            if (unityEventAfterSelectAnimation && selectAnimation != null)
            {
                SSUtility.DoIn(selectAnimation.FullEndTime(), () =>
                {
                    unityEventOnSelect?.Invoke();
                });
                return;
            }

            unityEventOnSelect?.Invoke();
        }

        public UnityEvent unityEventOnSelect;
        [SerializeField] private bool unityEventAfterSelectAnimation = false;

        public UnityEvent unityEventOnUnselect;
        #endregion

        #region Audio
        [SerializeField] private string clickAudio;

        [SerializeField] private string selectAudio;

        [SerializeField] private string hoverAudio;

        [SerializeField] private string toggleOnAudio;

        [SerializeField] private string toggleOffAudio;
        #endregion

        #region Haptics
        [SerializeField] private HapticSettings clickHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings selectHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings hoverHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings toggleOnHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings toggleOffHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        #endregion

        #region Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!ButtonCanBeClicked(true)) { return; }
            
            _isHovered = true;
            
            // If treat hover as selection, call select handlers instead
            if (treatHoverAsSelection)
            {
                OnSelect(null);
                return;
            }
            
            HoverAnimation();
            SIGS.PlayAudio(hoverAudio);
            if (hoverHaptics.Enabled)
                SIGS.TriggerHaptic(hoverHaptics);
            if (selectionAffectedByHover && selectionObject != null)
            {
                selectionObject.SetActive(true);
            }
            DoUnityEventOnHover();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            
            // If treat hover as selection, call deselect handlers instead
            if (treatHoverAsSelection)
            {
                OnDeselect(null);
                return;
            }
            
            UnhoverAnimation();
            if (selectionAffectedByHover && selectionObject != null)
            {
                selectionObject.SetActive(false);
            }
            DoUnityEventOnUnhover();
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (!ButtonCanBeClicked(true)) { return; }
            
            // Only block if hover animation has an infinite loop (can't be interrupted)
            if (hoverAnimation != null && hoverAnimation.Performing && hoverAnimation.HasAnInfiniteLoop()) { return; }
            
            _isSelected = true;
            
            SelectAnimation();
            SIGS.PlayAudio(selectAudio);
            if (selectHaptics.Enabled)
                SIGS.TriggerHaptic(selectHaptics);
            if (selectionObject != null)
            {
                selectionObject.SetActive(true);
            }
            DoUnityEventOnSelect();
        }

        private void DoUnityEventOnUnselect()
        {
            if (unityEventAfterSelectAnimation && deselectAnimation != null)
            {
                SSUtility.DoIn(deselectAnimation.FullEndTime(), () =>
                {
                    unityEventOnUnselect?.Invoke();
                });
                return;
            }

            unityEventOnUnselect?.Invoke();
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isSelected = false;
            
            DeselectAnimation();
            if (selectionObject != null)
            {
                selectionObject.SetActive(false);
            }
            DoUnityEventOnUnselect();
        }
        #endregion
    }
}