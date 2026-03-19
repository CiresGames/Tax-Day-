using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.InlineScript.External.Examples
{
    /// <summary>
    /// An example MonoBehaviour that demonstrates how to use InlineVoid to execute code during Unity's lifecycle methods.
    /// Useful for quickly injecting behavior into any GameObject without needing to write a new script.
    /// </summary>
    [AddComponentMenu("Signalia/Inline Script/Signalia | Inline Behaviour")]
    public class GameObjectScript : MonoBehaviour
    {
        [SerializeField] private InlineVoid onAwake;
        [SerializeField] private InlineVoid onEnable;
        [SerializeField] private InlineVoid onStart;

        [SerializeField] private InlineVoid onUpdate;
        [SerializeField] private InlineVoid onLateUpdate;
        [SerializeField] private InlineVoid onFixedUpdate;

        [SerializeField] private InlineVoid onBecameVisible;
        [SerializeField] private InlineVoid onBecameInvisible;
        [SerializeField] private InlineVoid onMouseEnter;
        [SerializeField] private InlineVoid onMouseExit;
        [SerializeField] private InlineVoid onMouseDown;
        [SerializeField] private InlineVoid onMouseUp;

        [SerializeField] private InlineVoid onDisable;
        [SerializeField] private InlineVoid onDestroy;

        void Awake() => onAwake.ExecuteMono(gameObject);
        void OnEnable() => onEnable.ExecuteMono(gameObject);
        void Start() => onStart.ExecuteMono(gameObject);
        void Update() => onUpdate.ExecuteMono(gameObject);
        void LateUpdate() => onLateUpdate.ExecuteMono(gameObject);
        void FixedUpdate() => onFixedUpdate.ExecuteMono(gameObject);

        void OnBecameVisible() => onBecameVisible.ExecuteMono(gameObject);
        void OnBecameInvisible() => onBecameInvisible.ExecuteMono(gameObject);

        void OnMouseEnter() => onMouseEnter.ExecuteMono(gameObject);
        void OnMouseExit() => onMouseExit.ExecuteMono(gameObject);
        void OnMouseDown() => onMouseDown.ExecuteMono(gameObject);
        void OnMouseUp() => onMouseUp.ExecuteMono(gameObject);

        void OnDisable() => onDisable.ExecuteMono(gameObject);
        void OnDestroy() => onDestroy.ExecuteMono(gameObject);
    }
}