using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Overrides the texture tiling for a Renderer's material using MaterialPropertyBlock,
    /// allowing per-instance tiling adjustments without modifying the shared material.
    /// Supports both manual tiling values and automatic tiling based on the object's local scale.
    /// Can enforce uniform X and Y tiling values when enabled.
    /// </summary>
    [AddComponentMenu("Signalia/Tools/Scene Helpers/Signalia | Local Tiling Override")]
    [ExecuteAlways]
    public class LocalTilingOverride : MonoBehaviour
    {
        [SerializeField] private bool useAutoTiling = false;
        [SerializeField] private bool uniformity = false;
        [SerializeField] private Vector2 tiling = Vector2.one;

        private Renderer rend;
        private MaterialPropertyBlock mpb;
        private Vector3 lastScale;

        void OnEnable()
        {
            rend = GetComponent<Renderer>();
            mpb = new MaterialPropertyBlock();
            lastScale = transform.localScale;
            Apply();
        }

        void OnValidate()
        {
            if (!rend) rend = GetComponent<Renderer>();
            if (mpb == null) mpb = new MaterialPropertyBlock();
            lastScale = transform.localScale;
            Apply();
        }

        void Update()
        {
            if (useAutoTiling && transform.localScale != lastScale)
            {
                lastScale = transform.localScale;
                Apply();
            }
        }

        void Apply()
        {
            if (rend == null) return;

            Vector2 finalTiling = useAutoTiling 
                ? new Vector2(transform.localScale.x, transform.localScale.y)
                : tiling;

            if (uniformity)
            {
                float uniformValue = finalTiling.x;
                finalTiling = new Vector2(uniformValue, uniformValue);
            }

            rend.GetPropertyBlock(mpb);
            mpb.SetVector("_MainTex_ST", new Vector4(finalTiling.x, finalTiling.y, 0f, 0f));
            rend.SetPropertyBlock(mpb);
        }
    }
}

