using UnityEngine;
using UnityEngine.InputSystem;
public class DynamicCursor : MonoBehaviour
{
    [System.Serializable]
    public class CursorState
    {
        public string tag;           // The tag of the object the cursor overlaps
        public Texture2D cursorTexture;
        public Vector2 hotspot = Vector2.zero;
    }

    [Header("Cursor States")]
    public CursorState[] cursorStates;
    public Texture2D defaultCursor;
    public Vector2 defaultHotspot = Vector2.zero;

    [Header("Settings")]
    public float raycastDistance = 100f;
    public LayerMask detectLayers = ~0; // All layers by default

    private Camera _cam;
    private string _currentTag = "";

    private void Start()
    {
        _cam = Camera.main;
        Cursor.lockState = CursorLockMode.Confined;
        SetCursor(defaultCursor, defaultHotspot);
    }

    private void Update()
    {
        DetectUnderCursor();
    }

    void DetectUnderCursor()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 worldPos = _cam.ScreenToWorldPoint(mousePos); // ← the fix

        Collider2D hit = Physics2D.OverlapPoint(mousePos, detectLayers);
        string hitTag = hit != null ? hit.gameObject.tag : "";

        Debug.Log(hit != null ? $"Hit: {hit.gameObject.name} | Tag: {hit.gameObject.tag}" : "No hit");

        if (hitTag == _currentTag) return;
        _currentTag = hitTag;

        foreach (var state in cursorStates)
        {
            if (state.tag == hitTag)
            {
                SetCursor(state.cursorTexture, state.hotspot);
                return;
            }
        }

        SetCursor(defaultCursor, defaultHotspot);
    }

    void SetCursor(Texture2D texture, Vector2 hotspot)
    {
        Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
    }
}