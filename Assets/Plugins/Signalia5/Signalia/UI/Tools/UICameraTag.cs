using UnityEngine;

namespace AHAKuo.Signalia.UI
{
    [AddComponentMenu("Signalia/Tools/Signalia | UI Camera Tag")]
    /// <summary>
    /// A tag for UI camera. Used by animations to use viewport conversions and other camera-related functions.
    /// </summary>
    public class UICameraTag : MonoBehaviour
    {
        private static Camera _uiCamera;
        public static Camera UICamera
        {
            get
            {
                if (_uiCamera == null)
                {
                    // if one camera exists in the scene, 
                    // use it
#if UNITY_6000_0_OR_NEWER
                    Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
                    Camera[] cameras = FindObjectsOfType<Camera>();
#endif
                    if (cameras.Length == 1)
                    {
                        // add the tag to the camera if it doesn't have it already
                        if (cameras[0].GetComponent<UICameraTag>() == null)
                            cameras[0].gameObject.AddComponent<UICameraTag>();
                            _uiCamera = cameras[0];
                    }

                    // if not, then see if there is one that has culling mask of UI only
                    else if (cameras.Length > 1)
                    {
                        foreach (Camera cam in cameras)
                        {
                            if (cam.cullingMask == LayerMask.GetMask("UI"))
                            {
                                cam.gameObject.AddComponent<UICameraTag>();
                                _uiCamera = cam;
                                break;
                            }
                        }
                    }

                    if (_uiCamera == null)
                        Debug.LogWarning("UICameraTag: UICamera is not set. Please choose a camera to be the main UICamera, and place a UICameraTag on it. If you already did that, make sure to reference the camera outside of Awake or make sure it has been initialized. This tag is need when using positional animations, and other camera related functions.");
                }
                return _uiCamera;
            }
            private set
            {
                _uiCamera = value;
            }
        }

        private void Awake()
        {
            UICamera = GetComponent<Camera>();

            // find and see if there is more than one UICameraTag in the scene/s, if so, tell the user.
#if UNITY_6000_0_OR_NEWER
            UICameraTag[] tags = FindObjectsByType<UICameraTag>(FindObjectsSortMode.None);
#else
            UICameraTag[] tags = FindObjectsOfType<UICameraTag>();
#endif

            if (tags.Length > 1)
            {
                Debug.LogWarning("There are more than one UICameraTag in the scene. This may cause unexpected behavior. If you are loading between scenes, then it might be safe to ignore this warning. If you are not, then please make sure to remove the extra UICameraTag components.");
            }
        }
    }
}
