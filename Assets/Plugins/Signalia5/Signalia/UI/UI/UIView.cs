using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Linq;
using AHAKuo.Signalia.Utilities;
using AHAKuo.Signalia.Framework;
using DG.Tweening;
using AHAKuo.Signalia.Radio;
using AHAKuo.Signalia.Utilities.SIGInput;

using AHAKuo.Signalia.GameSystems.LoadingScreens;

namespace AHAKuo.Signalia.UI
{
    [RequireComponent(typeof(RectTransform))]
    [AddComponentMenu("Signalia/UI/Signalia | UI View")]
    /// <summary>
    /// Placed on a RectTransform to control its visibility, tweening, and effects with Signalia.
    /// </summary>
    public class UIView : MonoBehaviour
    {
        #region Main
        [SerializeField] private UIViewStatus startingStatus = UIViewStatus.Hidden;

        [SerializeField] private bool backButtonHides = false;
        public bool BackButtonHides => backButtonHides;

        [SerializeField] private bool majorMenu = false;

        [SerializeField] private string menuToBackTo;

        [SerializeField] private bool hideAllOtherMenusOnShow = false;

        [SerializeField] private bool disableGameObject = false;

        [SerializeField] private bool disableGraphicRaycaster = false;

        [SerializeField] private Selectable firstSelectedOnShow;

        [SerializeField] private bool deselectAllOnHide;

        [SerializeField] private bool reselectPreviousMenuOnHide = false;

        [SerializeField] private bool useFaintBackground;

        [SerializeField] private Color faintBackgroundColor = new(0, 0, 0, 0.5f);

        [SerializeField] private bool faintBackgroundHideOnTap;

        #region Input Handling
        [SerializeField] private bool enableInputHandling = false;
        [SerializeField] private string inputActionName = "";
        [SerializeField] private UIViewInputBehavior inputBehavior = UIViewInputBehavior.Toggle;
        [SerializeField] private bool useCooldown = true;
        [SerializeField] private float cooldownDuration = 0.2f;
        #endregion

        #region Visible NoEdit-Variables (From Editor)  
        public UIViewStatus currentStatus;
        #endregion

        #region Visible Transition Classes
        [SerializeField] private UIAnimationAsset showAnimation;
        [SerializeField] private UIAnimationAsset hideAnimation;
        [SerializeField] private bool playOnlyWhenChangingStatus = true;
        [SerializeField] private bool cancelOpposites = true;
        #endregion

        #endregion

        public event Action OnHideByBackward;

        public void SubscribeToHideByBackward(Action action)
        {
            OnHideByBackward -= () => action();
            OnHideByBackward += () => action();
        }

        #region Menu Names
        public string GenerateMenuName()
        {
            return gameObject.name;
        }
        [SerializeField] private string menuName;

        public string MenuName => menuName;
        #endregion

        #region Read-Only Fields
        public RectTransform uiRect { get; private set; }
        public bool IsShowing => showAnimation == null ? false : showAnimation.Performing;
        public bool IsHiding => hideAnimation == null ? false : hideAnimation.Performing;        
        public bool IsShown => currentStatus == UIViewStatus.Shown;
        public bool IsHidden => currentStatus == UIViewStatus.Hidden;
        public UIAnimationAsset ShowAnimation => showAnimation;
        public UIAnimationAsset HideAnimation => hideAnimation;
        #endregion

        #region Private References
        private GraphicRaycaster graphicRaycaster;
        private UIViewGroup viewGroup;
        private Tween popupWait;
        public void AssignGroup(UIViewGroup grp) => viewGroup = grp;
        #endregion

        #region Delegates
        public delegate void Action();
        public event Action OnShowStart;
        public event Action OnHideStart;
        public event Action OnShowEnd;
        public event Action OnHideEnd;
        #endregion

        private void Awake()
        {
            if (LoadingScreen.OnALoadingScreen &&
                Watchman.IsQuitting) { return; }

            RuntimeValues.TrackedValues.LogViewRegistry(this);

            currentStatus = UIViewStatus.DoNothing;

            uiRect = GetComponent<RectTransform>();
            graphicRaycaster = GetComponent<GraphicRaycaster>();

            if(showAnimation != null)
            {
                showAnimation = showAnimation.CreateInstance();
            }
            else
            {
                Debug.LogWarning("No show animation found on " + gameObject.name + " Views require animations to work.");
            }

            if(hideAnimation != null)
            {
                hideAnimation = hideAnimation.CreateInstance();
            }
            else
            {
                Debug.LogWarning("No hide animation found on " + gameObject.name + " Views require animations to work.");
            }

            // faint background. Make a full stretch rect with faint background and add a UIButton to it with the hide parent view function.
            if (useFaintBackground)
            {
                GameObject bg = new("FaintBackground");
                RectTransform bgRect = bg.AddComponent<RectTransform>();
                bgRect.SetParent(transform, false);
                bgRect.SetAsFirstSibling();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                Image img = bg.AddComponent<Image>();
                img.color = faintBackgroundColor;

                if (faintBackgroundHideOnTap)
                {
                    UIButton btn = bg.AddComponent<UIButton>();
                    btn.SetHideParentView(true);
                }
            }
        }

        private void Start()
        {
            if (InEditMode()) { return; }

            if (startingStatus == UIViewStatus.DoNothing) { return; }

            if (startingStatus == UIViewStatus.Shown)
            {
                Show(true);
            }
            else
            {
                Hide(true);
            }

            ApplyOverrides();
        }

        private void Update()
        {
            if (InEditMode()) { return; }
            if (!enableInputHandling) { return; }
            if (string.IsNullOrWhiteSpace(inputActionName)) { return; }

            // Avoid spamming warnings when no wrapper exists
            if (!SignaliaInputWrapper.Exists) { return; }

            // Check if input action was pressed this frame
            if (SIGS.GetInputDown(inputActionName))
            {
                // Check cooldown if enabled
                if (useCooldown && SIGS.IsInputOnCooldown(inputActionName))
                {
                    return; // Still on cooldown, ignore input
                }

                HandleInputAction();

                // Set cooldown after handling input
                if (useCooldown && cooldownDuration > 0f)
                {
                    SIGS.SetInputCooldown(inputActionName, cooldownDuration);
                }
            }
        }

        private void HandleInputAction()
        {
            switch (inputBehavior)
            {
                case UIViewInputBehavior.Show:
                    Show();
                    break;
                case UIViewInputBehavior.Hide:
                    Hide();
                    break;
                case UIViewInputBehavior.Toggle:
                    if (IsShown)
                    {
                        Hide();
                    }
                    else
                    {
                        Show();
                    }
                    break;
            }
        }

        private void ApplyOverrides()
        {
            if (RuntimeValues.Config.OverrideUiViewAnimateOnce)
            {
                playOnlyWhenChangingStatus = RuntimeValues.Config.UIViewsAnimateOnce;
            }
        }

        private void SetStatus(UIViewStatus uIViewStatus)
        {
            currentStatus = uIViewStatus;
        }

        /// <summary>
        /// Show the view as a popup which hides automatically after some time.
        /// </summary>
        /// <param name="hideAfterTime"></param>
        /// <param name="unscaledWait"></param>
        public void ShowAsPopUp(float hideAfterTime, bool unscaledWait = false)
        {
            if (hideAfterTime <= showAnimation.FullEndTime())
            {
                Debug.LogWarning("Cannot use popup feature because the hide after time is not longer than the show animation.");
                return;
            }

            popupWait?.Kill();

            Show();

            popupWait = SIGS.DoIn(hideAfterTime, () => Hide(), unscaledWait);
        }

        /// <summary>
        /// Call this before invoking show or hide to forcefully cancel any ongoing motion before the next call.
        /// </summary>
        public void CancelCurrentMotion()
        {
            if (IsShowing)
                showAnimation.StopAnimations();
            if (IsHiding)
                hideAnimation.StopAnimations();
        }

        public void Show(bool instant = false)
        {
            // Cancel opposite animation if enabled and currently hiding
            if (cancelOpposites && IsHiding && hideAnimation != null)
            {
                hideAnimation.StopAnimations();
            }

            // Check if we should prevent re-iterating (but allow if we just cancelled a hide)
            if (playOnlyWhenChangingStatus
                && ((currentStatus == UIViewStatus.Shown && !IsHiding) ||
                IsShowing)) { return; }

            if (showAnimation == null) { return; }

            if (disableGameObject) { gameObject.SetActive(true); }

            if (disableGraphicRaycaster
                && graphicRaycaster != null) { graphicRaycaster.enabled = true; }

            if (viewGroup != null)
            {
                // hide all other menus in the unique list
                foreach (var item in viewGroup.Views)
                {
                    item.Hide();
                }
            }
            OnShowStart?.Invoke();
            DoShowEvents();
            DoShowUnityEvent();
            if (!instant)
            {
                SIGS.PlayAudio(showStartAudio);
                if (showStartHaptics.Enabled)
                    SIGS.TriggerHaptic(showStartHaptics);
            }

            // hide all cascades first
            foreach (var c in cascades)
            {
                c.Hide();
            }

            showAnimation.PerformAnimation(() => 
            {
                if (disableGraphicRaycaster
                    && graphicRaycaster != null) { graphicRaycaster.enabled = true; } // do it twice to confirm -_-. Not sure why I have to but there's a 0.1 chance it might not work the first time.
                SetStatus(UIViewStatus.Shown);
                OnShowEnd?.Invoke();
                DoShowEndEvents();
                DoShowEndUnityEvent();
                if (!instant)
                {
                    SIGS.PlayAudio(showEndAudio);
                    if (showEndHaptics.Enabled)
                        SIGS.TriggerHaptic(showEndHaptics);
                }
                foreach (var c in cascades)
                {
                    if (!c.ShowOnEnd) { continue; }
                    c.Show();
                }
                
                // Select the first selected item after animation completes
                if (EventSystem.current != null
                    && firstSelectedOnShow != null)
                {
                    EventSystem.current.SetSelectedGameObject(firstSelectedOnShow.gameObject);
                    
                    // Manually trigger OnSelect on UIButton to play selection animation
                    UIButton uiButton = firstSelectedOnShow.GetComponent<UIButton>();
                    if (uiButton != null)
                    {
                        uiButton.PerformSelect();
                    }
                }

                // Apply time modifier when view is fully shown
                ApplyTimeModifier();
                
                // Apply input blocker when view is fully shown
                ApplyInputBlocker();
            }, this.gameObject, instant);

            RuntimeValues.TrackedValues.LogMovingViewLength(this, showAnimation);
            RuntimeValues.Config.UIViewAnimatingIn.SendEvent(gameObject);
            foreach (var c in cascades)
            {
                if (c.ShowOnEnd) { continue; }
                c.Show();
            }

            if (hideAllOtherMenusOnShow)
            {
                RuntimeValues.TrackedValues.ViewRegistry.Where(x => x.majorMenu && x.IsShown).ToList().ForEach(x => x.Hide());
            }

            if (backButtonHides)
            {
                RuntimeValues.TrackedValues.LogTravelHistory(this);
            }
        }

        public void Hide(bool instant = false)
        {
            // Cancel opposite animation if enabled and currently showing
            if (cancelOpposites && IsShowing && showAnimation != null)
            {
                showAnimation.StopAnimations();
            }

            // Check if we should prevent re-iterating (but allow if we just cancelled a show)
            if (playOnlyWhenChangingStatus
                && ((currentStatus == UIViewStatus.Hidden && !IsShowing) ||
                IsHiding)) { return; }

            if (hideAnimation == null) { return; }

            if (deselectAllOnHide)
            {
                if (EventSystem.current != null)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }

            // hide all children menus
            foreach (var item in childrenMenus)
            {
                SIGS.UIViewControl(item, false);
            }
            OnHideStart?.Invoke();
            DoHideEvents();
            DoHideUnityEvent();
            if (!instant)
            {
                SIGS.PlayAudio(hideStartAudio);
                if (hideStartHaptics.Enabled)
                    SIGS.TriggerHaptic(hideStartHaptics);
            }
            hideAnimation.PerformAnimation(() =>
            {
                SetStatus(UIViewStatus.Hidden);
                if (disableGameObject) { gameObject.SetActive(false); }
                if (disableGraphicRaycaster
                    && graphicRaycaster != null) { graphicRaycaster.enabled = false; }
                if (!instant)
                {
                    SIGS.PlayAudio(hideEndAudio);
                    if (hideEndHaptics.Enabled)
                        SIGS.TriggerHaptic(hideEndHaptics);
                }
                OnHideEnd?.Invoke();
                DoHideEndEvents();
                DoHideEndUnityEvent();
                foreach (var c in cascades)
                {
                    c.Hide();
                }

                // Remove time modifier when view is fully hidden
                RemoveTimeModifier();
                
                // Remove input blocker when view is fully hidden
                RemoveInputBlocker();
            }
            , this.gameObject, instant);

            if (backButtonHides)
            {
                RuntimeValues.TrackedValues.LogRemoveTravelHistory(this);
            }

            RuntimeValues.TrackedValues.LogMovingViewLength(this, hideAnimation);
            RuntimeValues.Config.UIViewAnimatingOut.SendEvent(gameObject);

            // Only reselect previous menu if explicitly enabled
            if (reselectPreviousMenuOnHide)
            {
                var lastMenu = RuntimeValues.TrackedValues.TravelHistory.LastOrDefault();

                if (lastMenu != null &&
                    lastMenu.IsShown &&
                    EventSystem.current != null
                && lastMenu.firstSelectedOnShow != null)
                {
                    EventSystem.current.SetSelectedGameObject(lastMenu.firstSelectedOnShow.gameObject);
                    
                    // Manually trigger OnSelect on UIButton to play selection animation
                    UIButton uiButton = lastMenu.firstSelectedOnShow.GetComponent<UIButton>();
                    if (uiButton != null)
                    {
                        uiButton.PerformSelect();
                    }
                }
            }
        }

        public void HideByBackward()
        {
            Hide();

            if (menuToBackTo.HasValue())
            {
                SIGS.UIViewControl(menuToBackTo, true);
            }

            foreach (var item in hideByBackwardEvents)
            {
                SimpleRadio.SendEventByContext(item, gameObject);
            }

            OnHideByBackward?.Invoke();
        }

        public enum UIViewStatus
        {
            DoNothing,
            Shown,
            Hidden
        }

        public enum UIViewInputBehavior
        {
            Show,
            Hide,
            Toggle
        }

        #region Events

        private void InvokeFromEvent(string menu, bool show)
        {
            if (menuName.IsNullOrEmpty()) { return; }

            if (menuName != menu) { return; }

            if (show)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void DoShowEvents()
        {
            foreach (var ev in showStartEvents)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }
        }

        private void DoHideEvents()
        {
            foreach (var ev in hideStartEvents)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }
        }

        private void DoShowEndEvents()
        {
            foreach (var ev in showEndEvents)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }
        }

        private void DoHideEndEvents()
        {
            foreach (var ev in hideEndEvents)
            {
                SimpleRadio.SendEventByContext(ev, gameObject);
            }
        }

        [SerializeField] private string[] showStartEvents = new string[0];

        [SerializeField] private string[] hideStartEvents = new string[0];

        [SerializeField] private string[] showEndEvents = new string[0];

        [SerializeField] private string[] hideEndEvents = new string[0];

        [SerializeField] private string[] hideByBackwardEvents = new string[0];

        #endregion

        #region Unity Events

        private void DoShowUnityEvent()
        {
            showUnityEvent?.Invoke();
        }

        private void DoShowEndUnityEvent()
        {
            showEndUnityEvent?.Invoke();

        }

        private void DoHideUnityEvent()
        {
            hideUnityEvent?.Invoke();
        }

        private void DoHideEndUnityEvent()
        {
            hideEndUnityEvent?.Invoke();

        }

        [SerializeField] private UnityEvent showUnityEvent;

        [SerializeField] private UnityEvent showEndUnityEvent;

        [SerializeField] private UnityEvent hideUnityEvent;

        [SerializeField] private UnityEvent hideEndUnityEvent;

        #endregion

        #region Cascades
        [SerializeField] private List<Cascadable> cascades = new();
        #endregion

        #region Inspector Tools
        private bool InPlayMode() => Application.isPlaying;
        private bool InEditMode() => !Application.isPlaying;
        private bool ShowAnimationContainsALoop()
        {
            bool loops = false;
            if(showAnimation != null)
            {
                loops = showAnimation.HasALoop() || showAnimation.HasAnInfiniteLoop();   
                if(loops) { return loops; }
            }
            return loops;
        }
        private bool HideAnimationContainsALoop()
        {
            bool loops = false;
            if (hideAnimation != null)
            {
                loops = hideAnimation.HasALoop() || hideAnimation.HasAnInfiniteLoop();
                if (loops) { return loops; }
            }
            return loops;
        }
        #endregion

        #region Event Subscription
        private void OnDestroy()
        {
            if (Watchman.IsQuitting
                || !Application.isPlaying
                || (LoadingScreen.OnALoadingScreen &&
    Watchman.IsQuitting))
                return;

            RuntimeValues.TrackedValues.LogRemoveViewRegistry(this);

            // remove me from Travel History if I am in it
            RuntimeValues.TrackedValues.LogRemoveTravelHistory(this);
        }

        public void CreateCascadesFromChildren()
        {
            cascades.Clear();

            // get immediate children and create cascades from them, don't double add existing cascades.
            List<UIAnimatable> anims = new();

            anims.AddRange(transform.GetComponentsInChildren<UIAnimatable>());

            if (anims.Count <= 0) { return; }

            anims.Where(x => !x.AnimationArray.Any(r => r.HasALoop)).Where(x => !cascades.Any(y => y.Target == x)).ToList().ForEach(x => cascades.Add(new(x)));
        }

        /// <summary>
        /// Add children cascades but apply an additive show on end delay
        /// </summary>
        public void CreateCascadesWithShowDominoEffect(bool show, bool hide)
        {
            cascades.Clear();

            // get immediate children and create cascades from them, don't double add existing cascades.
            List<UIAnimatable> anims = new();

            anims.AddRange(transform.GetComponentsInChildren<UIAnimatable>());

            if (anims.Count <= 0) { return; }

            anims = anims.Where(x => !x.AnimationArray.Any(r => r.HasALoop)).Where(x => !cascades.Any(y => y.Target == x)).ToList();

            for (int i = 0; i < anims.Count; i++)
            {
                var anim = anims[i];
                var cascade = new Cascadable(anim, i, show, hide);
                cascades.Add(cascade);
            }
        }
        #endregion

        #region Audio
        [SerializeField] private string showStartAudio;
        [SerializeField] private string showEndAudio;
        [SerializeField] private string hideStartAudio;
        [SerializeField] private string hideEndAudio;
        #endregion

        #region Haptics
        [SerializeField] private HapticSettings showStartHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings showEndHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings hideStartHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        [SerializeField] private HapticSettings hideEndHaptics = new HapticSettings(HapticType.None, 1f, 0.1f, false);
        #endregion

        #region Hierarchy
        [SerializeField] private List<string> childrenMenus = new();
        #endregion

        #region Game System Specific
        
        /// <summary>
        /// Applies the best settings for Dialogue.
        /// </summary>
        public void ApplyDialogueSystemSettings()
        {
            playOnlyWhenChangingStatus = false;
        }

        #endregion

        #region Signalia Time Integration

        /// <summary>
        /// Checks if this view has a time modifier configured in the Signalia config.
        /// </summary>
        private UIViewTimeModifier GetTimeModifierConfig()
        {
            if (string.IsNullOrEmpty(menuName)) return null;
            
            var config = RuntimeValues.Config;
            if (config?.SignaliaTime?.UIViewTimeModifiers == null) return null;

            foreach (var modifier in config.SignaliaTime.UIViewTimeModifiers)
            {
                if (modifier.ViewName == menuName)
                {
                    return modifier;
                }
            }
            return null;
        }

        /// <summary>
        /// Applies the time modifier for this view when it becomes visible.
        /// </summary>
        private void ApplyTimeModifier()
        {
            var modifierConfig = GetTimeModifierConfig();
            if (modifierConfig == null) return;

            string modifierId = $"UIView_{menuName}";
            var modifier = new TimeModifier(
                modifierId,
                string.IsNullOrEmpty(modifierConfig.Description) ? $"UIView: {menuName}" : modifierConfig.Description,
                modifierConfig.EffectiveValue,
                "UIView"
            );
            
            SignaliaTime.SetModifier(modifier);
        }

        /// <summary>
        /// Removes the time modifier for this view when it becomes hidden.
        /// </summary>
        private void RemoveTimeModifier()
        {
            var modifierConfig = GetTimeModifierConfig();
            if (modifierConfig == null) return;

            string modifierId = $"UIView_{menuName}";
            SignaliaTime.RemoveModifier(modifierId);
        }

        #endregion

        #region Input Blocker Integration

        /// <summary>
        /// Checks if this view has an input blocker configured in the Signalia config.
        /// </summary>
        private InputBlocker GetInputBlockerConfig()
        {
            if (string.IsNullOrEmpty(menuName)) return null;
            
            var config = RuntimeValues.Config;
            if (config?.InputSystem?.InputBlockers == null) return null;

            foreach (var blocker in config.InputSystem.InputBlockers)
            {
                if (blocker.UIViewName == menuName)
                {
                    return blocker;
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the cursor visibility rule for this view from config.
        /// </summary>
        private UIViewCursorVisibilityRule GetCursorVisibilityRule()
        {
            if (string.IsNullOrEmpty(menuName)) return null;
            
            var config = RuntimeValues.Config;
            if (config?.InputSystem?.CursorVisibility?.ViewVisibilityRules == null) return null;

            foreach (var rule in config.InputSystem.CursorVisibility.ViewVisibilityRules)
            {
                if (rule.ViewName == menuName)
                {
                    return rule;
                }
            }
            return null;
        }

        /// <summary>
        /// Applies the input state modifier for this view when it becomes visible.
        /// Uses the modifier system instead of direct Enable/Disable calls.
        /// </summary>
        private void ApplyInputBlocker()
        {
            var blockerConfig = GetInputBlockerConfig();
            var cursorRule = GetCursorVisibilityRule();
            
            // Only create modifier if we have either blocker config or cursor rule
            if (blockerConfig == null && cursorRule == null) return;

            string modifierId = $"UIView_{menuName}";
            
            // Determine blocked maps and actions
            string[] blockedMaps = blockerConfig?.ActionMapNames ?? Array.Empty<string>();
            string[] blockedActions = blockerConfig?.ActionNames ?? Array.Empty<string>();
            
            // Determine cursor visibility
            bool showCursor = false;
            CursorLockMode hiddenLockState = CursorLockMode.Locked;
            
            // Priority: InputBlocker.ControlCursor > CursorVisibilityRule
            if (blockerConfig != null && blockerConfig.ControlCursor)
            {
                showCursor = blockerConfig.ShowCursor;
                hiddenLockState = blockerConfig.HiddenCursorLockState;
            }
            else if (cursorRule != null)
            {
                showCursor = cursorRule.OnViewVisible == CursorVisibilityAction.Show;
                hiddenLockState = cursorRule.OnViewVisible == CursorVisibilityAction.HideAndLock 
                    ? CursorLockMode.Locked 
                    : CursorLockMode.None;
            }
            
            var modifier = new InputStateModifier(
                modifierId,
                "UIView",
                blockedMaps,
                blockedActions,
                showCursor,
                hiddenLockState,
                0
            );
            
            SignaliaInputBridge.SetModifier(modifier);
        }

        /// <summary>
        /// Removes the input state modifier for this view when it becomes hidden.
        /// </summary>
        private void RemoveInputBlocker()
        {
            var blockerConfig = GetInputBlockerConfig();
            var cursorRule = GetCursorVisibilityRule();
            
            if (blockerConfig == null && cursorRule == null) return;

            string modifierId = $"UIView_{menuName}";
            SignaliaInputBridge.RemoveModifier(modifierId);
        }

        #endregion
    }
}