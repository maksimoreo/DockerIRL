using System;
using System.Collections.Generic;
using UnityEngine;

namespace DockerIrl.Terminal
{
    public class TerminalInput : MonoBehaviour
    {
        private static HashSet<KeyCode> extraInputKeyCodesFilter = new HashSet<KeyCode>
        {
            KeyCode.UpArrow, KeyCode.RightArrow, KeyCode.DownArrow, KeyCode.LeftArrow
        };

        public Utils.ExtraInput extraInput;
        public bool debugInput;

        // Note: rn i just need to notify if there is any input, if u want to get actual keyCodes then refactor this code plz
        public event Action OnAnyInput;

        public event Action<char> OnInputSystemInput;
        public event Action<KeyCode> OnAdditionalInput;

        public void RegisterInputHandlers()
        {
            UnityEngine.InputSystem.Keyboard.current.onTextInput += HandleInputSystemTextInput;
            extraInput.OnInput += HandleAdditionalInput;
            extraInput.StartCapturing();
        }

        public void UnregisterInputHandlers()
        {
            UnityEngine.InputSystem.Keyboard.current.onTextInput -= HandleInputSystemTextInput;
            extraInput.OnInput -= HandleAdditionalInput;
            extraInput.StopCapturing();
        }

        private void HandleInputSystemTextInput(char c)
        {
            int code = (int)c;

            if (debugInput)
                Debug.Log($"HandleInputSystemTextInput: c: {c}, code: {code}");

            OnInputSystemInput?.Invoke(c);
            OnAnyInput?.Invoke();
        }

        private void HandleAdditionalInput(KeyCode keyCode)
        {
            if (debugInput)
                Debug.Log($"HandleAdditionalInput: keyCode: {keyCode}");

            if (extraInputKeyCodesFilter.Contains(keyCode))
            {
                OnAdditionalInput?.Invoke(keyCode);
                OnAnyInput?.Invoke();
            }
        }
    }
}
