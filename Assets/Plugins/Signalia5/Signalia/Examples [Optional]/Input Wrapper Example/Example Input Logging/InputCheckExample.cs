using System;
using System.Collections;
using System.Collections.Generic;
using AHAKuo.Signalia.Framework;
using UnityEngine;

namespace AHAKuo.Signalia.Examples
{
    public class InputCheckExample : MonoBehaviour
    {
        [Header("Input Check Options")]
        [SerializeField] private bool checkDown = true;
        [SerializeField] private bool checkHeld = false;
        [SerializeField] private bool checkUp = false;
        [SerializeField] private bool checkMove = true;
        [SerializeField] private bool checkLook = true;

        private void Update()
        {
            // Jump (Space)
            if (checkDown && SIGS.GetInputDown("Jump"))
                Debug.Log("Jump was pressed!");
            
            if (checkHeld && SIGS.GetInput("Jump"))
                Debug.Log("Jump is being held!");
            
            if (checkUp && SIGS.GetInputUp("Jump"))
                Debug.Log("Jump was released!");

            // Attack (Left Mouse Button or Ctrl)
            if (checkDown && SIGS.GetInputDown("Attack"))
                Debug.Log("Attack was pressed!");
            
            if (checkHeld && SIGS.GetInput("Attack"))
                Debug.Log("Attack is being held!");
            
            if (checkUp && SIGS.GetInputUp("Attack"))
                Debug.Log("Attack was released!");

            // Dash (Left Shift)
            if (checkDown && SIGS.GetInputDown("Dash"))
                Debug.Log("Dash was pressed!");
            
            if (checkHeld && SIGS.GetInput("Dash"))
                Debug.Log("Dash is being held!");
            
            if (checkUp && SIGS.GetInputUp("Dash"))
                Debug.Log("Dash was released!");

            // Pause (Escape)
            if (checkDown && SIGS.GetInputDown("Pause"))
                Debug.Log("Pause was pressed!");
            
            if (checkHeld && SIGS.GetInput("Pause"))
                Debug.Log("Pause is being held!");
            
            if (checkUp && SIGS.GetInputUp("Pause"))
                Debug.Log("Pause was released!");

            // Menu (Tab)
            if (checkDown && SIGS.GetInputDown("Menu"))
                Debug.Log("Menu was pressed!");
            
            if (checkHeld && SIGS.GetInput("Menu"))
                Debug.Log("Menu is being held!");
            
            if (checkUp && SIGS.GetInputUp("Menu"))
                Debug.Log("Menu was released!");

            // Cancel (Escape or Backspace)
            if (checkDown && SIGS.GetInputDown("Cancel"))
                Debug.Log("Cancel was pressed!");
            
            if (checkHeld && SIGS.GetInput("Cancel"))
                Debug.Log("Cancel is being held!");
            
            if (checkUp && SIGS.GetInputUp("Cancel"))
                Debug.Log("Cancel was released!");

            // Confirm (Enter or Space)
            if (checkDown && SIGS.GetInputDown("Confirm"))
                Debug.Log("Confirm was pressed!");
            
            if (checkHeld && SIGS.GetInput("Confirm"))
                Debug.Log("Confirm is being held!");
            
            if (checkUp && SIGS.GetInputUp("Confirm"))
                Debug.Log("Confirm was released!");

            // Interact (E)
            if (checkDown && SIGS.GetInputDown("Interact"))
                Debug.Log("Interact was pressed!");
            
            if (checkHeld && SIGS.GetInput("Interact"))
                Debug.Log("Interact is being held!");
            
            if (checkUp && SIGS.GetInputUp("Interact"))
                Debug.Log("Interact was released!");

            // Move (WASD or Arrow Keys) - Vector2
            if (checkMove)
            {
                var moveValue = SIGS.GetInputVector2("Move");
                
                if (moveValue != Vector2.zero)
                    Debug.Log($"Move Value: {moveValue}");
            }
            
            // Look (Mouse Movement) - Vector2
            if (checkLook)
            {
                var lookValue = SIGS.GetInputVector2("Look");
                
                if (lookValue != Vector2.zero)
                    Debug.Log($"Look Value: {lookValue}");
            }
        }
    }
}
