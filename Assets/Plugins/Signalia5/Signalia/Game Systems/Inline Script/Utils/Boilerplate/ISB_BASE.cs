using UnityEngine;
using System.ComponentModel;
using Component = UnityEngine.Component;

namespace AHAKuo.Signalia.GameSystems.InlineScript.Internal.Utils
{
    /// <summary>
    /// DO NOT INHERIT OR USE OUTSIDE SCOPE.
    /// </summary>
    public abstract class ISB_BASE
    {
        protected Transform transform;
        protected GameObject gameObject;
        protected string name => gameObject != null ? gameObject.name : "<no GameObject>";
        
        /// <summary>
        /// Executes the inline script code. Has gameobject as context.
        /// </summary>
        /// <param name="gameObject"></param>
        public virtual void ExecuteCode(GameObject gameObject = null)
        {
            // Initialize MonoBehaviour-like fields
            this.gameObject = gameObject;
            this.transform = gameObject != null ? gameObject.transform : null;
        }

        #region MonoBehaviour-like Component Access Methods

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject.
        /// </summary>
        public T GetComponent<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject.
        /// </summary>
        public Component GetComponent(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponent(componentType) : null;
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject.
        /// </summary>
        public Component GetComponent(string type)
        {
            return gameObject != null ? gameObject.GetComponent(type) : null;
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject.
        /// </summary>
        public T[] GetComponents<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponents<T>() : new T[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject.
        /// </summary>
        public Component[] GetComponents(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponents(componentType) : new Component[0];
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject or its children.
        /// </summary>
        public T GetComponentInChildren<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponentInChildren<T>() : null;
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject or its children.
        /// </summary>
        public Component GetComponentInChildren(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponentInChildren(componentType) : null;
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject or its children.
        /// </summary>
        public Component GetComponentInChildren(System.Type componentType, bool includeInactive)
        {
            return gameObject != null ? gameObject.GetComponentInChildren(componentType, includeInactive) : null;
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its children.
        /// </summary>
        public T[] GetComponentsInChildren<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponentsInChildren<T>() : new T[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its children.
        /// </summary>
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : Component
        {
            return gameObject != null ? gameObject.GetComponentsInChildren<T>(includeInactive) : new T[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its children.
        /// </summary>
        public Component[] GetComponentsInChildren(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponentsInChildren(componentType) : new Component[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its children.
        /// </summary>
        public Component[] GetComponentsInChildren(System.Type componentType, bool includeInactive)
        {
            return gameObject != null ? gameObject.GetComponentsInChildren(componentType, includeInactive) : new Component[0];
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject or its parents.
        /// </summary>
        public T GetComponentInParent<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponentInParent<T>() : null;
        }

        /// <summary>
        /// Gets a component of the specified type from the assigned GameObject or its parents.
        /// </summary>
        public Component GetComponentInParent(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponentInParent(componentType) : null;
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its parents.
        /// </summary>
        public T[] GetComponentsInParent<T>() where T : Component
        {
            return gameObject != null ? gameObject.GetComponentsInParent<T>() : new T[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its parents.
        /// </summary>
        public T[] GetComponentsInParent<T>(bool includeInactive) where T : Component
        {
            return gameObject != null ? gameObject.GetComponentsInParent<T>(includeInactive) : new T[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its parents.
        /// </summary>
        public Component[] GetComponentsInParent(System.Type componentType)
        {
            return gameObject != null ? gameObject.GetComponentsInParent(componentType) : new Component[0];
        }

        /// <summary>
        /// Gets all components of the specified type from the assigned GameObject and its parents.
        /// </summary>
        public Component[] GetComponentsInParent(System.Type componentType, bool includeInactive)
        {
            return gameObject != null ? gameObject.GetComponentsInParent(componentType, includeInactive) : new Component[0];
        }

        #endregion

        #region Transform and GameObject Utilities

        /// <summary>
        /// Gets the position of the assigned GameObject's transform.
        /// </summary>
        public Vector3 position
        {
            get => transform != null ? transform.position : Vector3.zero;
            set { if (transform != null) transform.position = value; }
        }

        /// <summary>
        /// Gets the rotation of the assigned GameObject's transform.
        /// </summary>
        public Quaternion rotation
        {
            get => transform != null ? transform.rotation : Quaternion.identity;
            set { if (transform != null) transform.rotation = value; }
        }

        /// <summary>
        /// Gets the local scale of the assigned GameObject's transform.
        /// </summary>
        public Vector3 localScale
        {
            get => transform != null ? transform.localScale : Vector3.one;
            set { if (transform != null) transform.localScale = value; }
        }

        /// <summary>
        /// Gets the local position of the assigned GameObject's transform.
        /// </summary>
        public Vector3 localPosition
        {
            get => transform != null ? transform.localPosition : Vector3.zero;
            set { if (transform != null) transform.localPosition = value; }
        }

        /// <summary>
        /// Gets the local rotation of the assigned GameObject's transform.
        /// </summary>
        public Quaternion localRotation
        {
            get => transform != null ? transform.localRotation : Quaternion.identity;
            set { if (transform != null) transform.localRotation = value; }
        }

        /// <summary>
        /// Gets the forward direction of the assigned GameObject's transform.
        /// </summary>
        public Vector3 forward => transform != null ? transform.forward : Vector3.forward;

        /// <summary>
        /// Gets the right direction of the assigned GameObject's transform.
        /// </summary>
        public Vector3 right => transform != null ? transform.right : Vector3.right;

        /// <summary>
        /// Gets the up direction of the assigned GameObject's transform.
        /// </summary>
        public Vector3 up => transform != null ? transform.up : Vector3.up;

        /// <summary>
        /// Gets the parent transform of the assigned GameObject.
        /// </summary>
        public Transform parent
        {
            get => transform != null ? transform.parent : null;
            set { if (transform != null) transform.parent = value; }
        }

        /// <summary>
        /// Gets the child count of the assigned GameObject's transform.
        /// </summary>
        public int childCount => transform != null ? transform.childCount : 0;

        /// <summary>
        /// Gets a child transform by index from the assigned GameObject.
        /// </summary>
        public Transform GetChild(int index)
        {
            return transform != null ? transform.GetChild(index) : null;
        }

        /// <summary>
        /// Finds a child transform by name from the assigned GameObject.
        /// </summary>
        public Transform Find(string name)
        {
            return transform != null ? transform.Find(name) : null;
        }

        /// <summary>
        /// Gets the root transform of the assigned GameObject.
        /// </summary>
        public Transform root => transform != null ? transform.root : null;

        #endregion

        #region Common MonoBehaviour Utilities

        /// <summary>
        /// Destroys the assigned GameObject.
        /// </summary>
        public void Destroy()
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject);
            }
        }

        /// <summary>
        /// Destroys the assigned GameObject after a delay.
        /// </summary>
        public void Destroy(float delay)
        {
            if (gameObject != null)
            {
                Object.Destroy(gameObject, delay);
            }
        }

        /// <summary>
        /// Destroys a component on the assigned GameObject.
        /// </summary>
        public void Destroy(Component component)
        {
            if (component != null)
            {
                Object.Destroy(component);
            }
        }

        /// <summary>
        /// Destroys a component on the assigned GameObject after a delay.
        /// </summary>
        public void Destroy(Component component, float delay)
        {
            if (component != null)
            {
                Object.Destroy(component, delay);
            }
        }

        /// <summary>
        /// Instantiates a GameObject at the assigned GameObject's position.
        /// </summary>
        public GameObject Instantiate(GameObject original)
        {
            return original != null && transform != null ? Object.Instantiate(original, transform.position, transform.rotation) : null;
        }

        /// <summary>
        /// Instantiates a GameObject at the assigned GameObject's position with specified rotation.
        /// </summary>
        public GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
        {
            return original != null ? Object.Instantiate(original, position, rotation) : null;
        }

        /// <summary>
        /// Instantiates a GameObject as a child of the assigned GameObject.
        /// </summary>
        public GameObject Instantiate(GameObject original, Transform parent)
        {
            return original != null ? Object.Instantiate(original, parent) : null;
        }

        /// <summary>
        /// Instantiates a GameObject as a child of the assigned GameObject with world position stay.
        /// </summary>
        public GameObject Instantiate(GameObject original, Transform parent, bool worldPositionStays)
        {
            return original != null ? Object.Instantiate(original, parent, worldPositionStays) : null;
        }

        /// <summary>
        /// Sets the active state of the assigned GameObject.
        /// </summary>
        public void SetActive(bool value)
        {
            if (gameObject != null)
            {
                gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// Gets the active state of the assigned GameObject.
        /// </summary>
        public bool activeInHierarchy => gameObject != null ? gameObject.activeInHierarchy : false;

        /// <summary>
        /// Gets the active self state of the assigned GameObject.
        /// </summary>
        public bool activeSelf => gameObject != null ? gameObject.activeSelf : false;

        /// <summary>
        /// Gets the tag of the assigned GameObject.
        /// </summary>
        public string tag
        {
            get => gameObject != null ? gameObject.tag : "Untagged";
            set { if (gameObject != null) gameObject.tag = value; }
        }

        /// <summary>
        /// Gets the layer of the assigned GameObject.
        /// </summary>
        public int layer
        {
            get => gameObject != null ? gameObject.layer : 0;
            set { if (gameObject != null) gameObject.layer = value; }
        }

        /// <summary>
        /// Compares the tag of the assigned GameObject.
        /// </summary>
        public bool CompareTag(string tag)
        {
            return gameObject != null ? gameObject.CompareTag(tag) : false;
        }

        #endregion
    }
}
