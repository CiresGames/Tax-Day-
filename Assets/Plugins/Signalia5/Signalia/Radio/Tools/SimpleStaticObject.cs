using System;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// Makes an object retrievable by a key.
    /// </summary>
    [AddComponentMenu("Signalia/Radio/Signalia | Live Key")]
    public class SimpleStaticObject : MonoBehaviour
    {
        [SerializeField] private List<StaticObjectElement> responderObjects = new();

        [Serializable]
        public class StaticObjectElement
        {
            [SerializeField] private GameObject otherGameObject;
            [SerializeField] private UnityEngine.Object referenceableObject;
            [SerializeField] private string staticName;

            private LiveKey staticObj;

            public void Initialize()
            {
                staticObj = new LiveKey(staticName, () => referenceableObject);
            }

            public void DeInitialize()
            {
                staticObj.Dispose();
            }
        }

        private void Awake()
        {
            responderObjects.ForEach(r => r.Initialize());
        }

        private void OnDestroy()
        {
            responderObjects.ForEach(r => r.DeInitialize());
        }
    }
}