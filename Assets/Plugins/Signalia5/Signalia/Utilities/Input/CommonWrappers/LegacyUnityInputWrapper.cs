using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace  AHAKuo.Signalia.Utilities.SIGInput.FactoryWrappers
{
    /// <summary>
    /// This is an example implementation of a SignaliaInputWrapper for Unity's legacy input system. The same applies for any other input system.
    /// </summary>
    public class LegacyUnityInputWrapper : SignaliaInputWrapper
    {
        private Vector2 previousMoveInput = Vector2.zero;
        private Vector2 previousLookInput = Vector2.zero;

        protected override void PollInput()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            // Jump (Space)
            if (Input.GetKeyDown(KeyCode.Space))
                SignaliaInputBridge.PassDown("Jump");
            
            if (Input.GetKey(KeyCode.Space))
                SignaliaInputBridge.PassHeld("Jump");
                
            if (Input.GetKeyUp(KeyCode.Space))
                SignaliaInputBridge.PassUp("Jump");

            // Attack (Left Mouse Button or Ctrl)
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.LeftControl))
                SignaliaInputBridge.PassDown("Attack");
            
            if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftControl))
                SignaliaInputBridge.PassHeld("Attack");
                
            if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.LeftControl))
                SignaliaInputBridge.PassUp("Attack");
            
            // Sprint (Left Shift)
            if (Input.GetKeyDown(KeyCode.LeftShift))
                SignaliaInputBridge.PassDown("Sprint");
            
            if (Input.GetKey(KeyCode.LeftShift))
                SignaliaInputBridge.PassHeld("Sprint");
                
            if (Input.GetKeyUp(KeyCode.LeftShift))
                SignaliaInputBridge.PassUp("Sprint");

            // Dash (C)
            if (Input.GetKeyDown(KeyCode.C))
                SignaliaInputBridge.PassDown("Dash");
            
            if (Input.GetKey(KeyCode.C))
                SignaliaInputBridge.PassHeld("Dash");
                
            if (Input.GetKeyUp(KeyCode.C))
                SignaliaInputBridge.PassUp("Dash");

            // Pause (Escape)
            if (Input.GetKeyDown(KeyCode.Escape))
                SignaliaInputBridge.PassDown("Pause");
            
            if (Input.GetKey(KeyCode.Escape))
                SignaliaInputBridge.PassHeld("Pause");
                
            if (Input.GetKeyUp(KeyCode.Escape))
                SignaliaInputBridge.PassUp("Pause");

            // Menu (Tab)
            if (Input.GetKeyDown(KeyCode.Tab))
                SignaliaInputBridge.PassDown("Menu");
            
            if (Input.GetKey(KeyCode.Tab))
                SignaliaInputBridge.PassHeld("Menu");
                
            if (Input.GetKeyUp(KeyCode.Tab))
                SignaliaInputBridge.PassUp("Menu");

            // Cancel (Escape or Backspace)
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace))
                SignaliaInputBridge.PassDown("Cancel");
            
            if (Input.GetKey(KeyCode.Escape) || Input.GetKey(KeyCode.Backspace))
                SignaliaInputBridge.PassHeld("Cancel");
                
            if (Input.GetKeyUp(KeyCode.Escape) || Input.GetKeyUp(KeyCode.Backspace))
                SignaliaInputBridge.PassUp("Cancel");

            // Confirm (Enter or Space)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space))
                SignaliaInputBridge.PassDown("Confirm");
            
            if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.Space))
                SignaliaInputBridge.PassHeld("Confirm");
                
            if (Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Space))
                SignaliaInputBridge.PassUp("Confirm");

            // Interact (E)
            if (Input.GetKeyDown(KeyCode.E))
                SignaliaInputBridge.PassDown("Interact");
            
            if (Input.GetKey(KeyCode.E))
                SignaliaInputBridge.PassHeld("Interact");
                
            if (Input.GetKeyUp(KeyCode.E))
                SignaliaInputBridge.PassUp("Interact");

            // Move (WASD or Arrow Keys) - Vector2
            Vector2 moveInput = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                moveInput.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                moveInput.y -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                moveInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                moveInput.x += 1f;

            SignaliaInputBridge.PassVector("Move", moveInput);
            previousMoveInput = moveInput;

            // Look (Mouse Movement) - Vector2
            Vector2 lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            SignaliaInputBridge.PassVector("Look", lookInput);
            previousLookInput = lookInput;
            
            // Scrolling (Mouse ScrollWheel) - Float
            SignaliaInputBridge.PassFloat("Scroll", -Input.mouseScrollDelta.y); // negative because usually you'd want to use this for zooming
            
            #else
            Debug.Log("You are not using the legacy input manager. Make a custom wrapper and implement your input logic instead of using this one.");
#endif
        }
    }
}