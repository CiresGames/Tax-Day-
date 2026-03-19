using UnityEngine;
using UnityEngine.Events;

namespace AHAKuo.Signalia.GameSystems.Movement
{
    /// <summary>
    /// Abstract base class for movement physics modifiers.
    /// Provides a common interface for components that modify character movement physics.
    /// All modifiers automatically find and cache the IMovementPhysicsAuthority component on this GameObject or in parent objects.
    /// </summary>
    public abstract class MovementPhysicsModifier : MonoBehaviour
    {
        #region Serialized Fields

        /// <summary>
        /// Whether this modifier is enabled and should execute its logic.
        /// When disabled, the modifier will not perform any actions.
        /// </summary>
        [SerializeField] private bool modifierEnabled = true;

        /// <summary>
        /// Event invoked when the modifier becomes enabled/activated.
        /// Triggered when IsModifierEnabled changes from false to true.
        /// </summary>
        [SerializeField] private UnityEvent onModifierBegin = new UnityEvent();

        /// <summary>
        /// Event invoked when the modifier becomes disabled/deactivated.
        /// Triggered when IsModifierEnabled changes from true to false.
        /// </summary>
        [SerializeField] private UnityEvent onModifierEnd = new UnityEvent();

        #endregion

        #region Protected Fields

        /// <summary>
        /// Cached reference to the movement physics authority component.
        /// Automatically found and assigned in Awake.
        /// </summary>
        protected IMovementPhysicsAuthority PhysicsAuthority { get; private set; }

        #endregion

        #region Private Fields

        /// <summary>
        /// Previous enabled state to detect changes.
        /// </summary>
        private bool previousEnabledState;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets whether this modifier is enabled.
        /// When disabled, the modifier will not perform any actions.
        /// </summary>
        public bool IsModifierEnabled
        {
            get => modifierEnabled;
            set
            {
                bool oldValue = modifierEnabled;
                modifierEnabled = value;
                
                // Trigger events if state changed
                if (oldValue != modifierEnabled)
                {
                    if (modifierEnabled)
                    {
                        RaiseModifierBegin();
                    }
                    else
                    {
                        RaiseModifierEnd();
                    }
                }
            }
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Event invoked when the modifier becomes enabled/activated.
        /// </summary>
        public UnityEvent OnModifierBegin => onModifierBegin;

        /// <summary>
        /// Event invoked when the modifier becomes disabled/deactivated.
        /// </summary>
        public UnityEvent OnModifierEnd => onModifierEnd;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            // Find the physics authority component on this GameObject or in parent objects
            PhysicsAuthority = GetComponentInParent<IMovementPhysicsAuthority>();
            
            if (PhysicsAuthority == null)
            {
                Debug.LogError($"{GetType().Name} requires an IMovementPhysicsAuthority component (e.g., MovementPhysics3D) on this GameObject or a parent GameObject.", this);
            }

            // Initialize previous state
            previousEnabledState = modifierEnabled;
        }

        protected virtual void OnEnable()
        {
            // If modifier was enabled and component becomes enabled, trigger begin event
            if (modifierEnabled && !previousEnabledState)
            {
                RaiseModifierBegin();
                previousEnabledState = true;
            }
            else if (modifierEnabled)
            {
                // Component enabled and modifier was already enabled - trigger begin
                RaiseModifierBegin();
            }
        }

        protected virtual void OnDisable()
        {
            // If modifier was enabled and component becomes disabled, trigger end event
            if (modifierEnabled)
            {
                RaiseModifierEnd();
            }
        }

        protected virtual void Update()
        {
            // Check for enabled state changes (in case modifierEnabled is changed directly via serialized field)
            if (previousEnabledState != modifierEnabled)
            {
                if (modifierEnabled)
                {
                    RaiseModifierBegin();
                }
                else
                {
                    RaiseModifierEnd();
                }
                previousEnabledState = modifierEnabled;
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Invokes the OnModifierBegin event. Can be overridden by derived classes.
        /// </summary>
        protected virtual void RaiseModifierBegin()
        {
            onModifierBegin?.Invoke();
        }

        /// <summary>
        /// Invokes the OnModifierEnd event. Can be overridden by derived classes.
        /// </summary>
        protected virtual void RaiseModifierEnd()
        {
            onModifierEnd?.Invoke();
        }

        #endregion
    }
}

