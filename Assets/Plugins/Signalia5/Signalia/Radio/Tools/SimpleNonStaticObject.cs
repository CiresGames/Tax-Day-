using System;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Radio
{
    /// <summary>
    /// Makes an object retrievable by a key using the non-static pattern.
    /// </summary>
    [AddComponentMenu("Signalia/Radio/Signalia | Dead Key")]
    public class SimpleNonStaticObject : MonoBehaviour
    {
        [SerializeField] private List<NonStaticObjectElement> responderObjects = new();

        [Serializable]
        public class NonStaticObjectElement
        {
            [SerializeField] private GameObject otherGameObject;
            [SerializeField] private UnityEngine.Object referenceableObject;
            [SerializeField] private string nonStaticName;

            private DeadKey nonStaticObj;

            public void Initialize()
            {
                nonStaticObj = new DeadKey(nonStaticName, referenceableObject, otherGameObject);
            }

            public void DeInitialize()
            {
                nonStaticObj.Dispose();
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
