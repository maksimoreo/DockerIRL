using System;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Something that acts like a Terminal Emulator, from DockerIrl perspective. DockerIrl app will use this
    /// TerminalEmulator object to send user's input to, receive program's output from, and handle other user actions.
    /// This object should implement interactions between Unity's InputSystem, DockerIrl app, OS PTY and underlying
    /// terminal emulator library.
    /// </summary>
    public interface ITerminalEmulator
    {
        /// <summary>
        /// Emulator has processed User input and now wants to send it to the PTY.
        /// DATA: Emulator -> PTY
        /// </summary>
        public event Action<string> OnSendData;

        public event Action OnBell;
        public event Action<string> OnLog;

        /// <summary>
        /// Initialize terminal. U can get rows & columns counts from ptyOptions
        /// </summary>
        /// <param name="ptyOptions"></param>
        /// <param name="maxBufferLines"></param>
        public void Initialize(IPty.PtyOptions ptyOptions, int maxBufferLines = 200);

        #region Input
        /// <summary>
        /// Receives text input. It is usually a single character. This input cannot be represented by a KeyCode.
        /// Example: 'a', '1', '&', ' ', 'ą', emoji, etc.
        /// Right now it is the output of Unity's InputSystem:
        /// https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Keyboard.html#UnityEngine_InputSystem_Keyboard_onTextInput
        /// DATA: User -> Emulator
        /// </summary>
        public void SendTextInput(char input);

        /// <summary>
        /// Send everything that can't be expressed as a single character and is more like an action from user
        /// Example: arrow keys
        /// DATA: User -> Emulator
        /// </summary>
        public void SendKeyInput(KeyCode keyCode);

        // TODO: Mouse input
        // public void SendMouseInput(...);
        #endregion

        /// <summary>
        /// Renders current buffer state to TextMeshPro markup syntax text.
        /// DATA: Emulator -> Terminal or TextRenderer
        /// </summary>
        /// <returns>Foreground TMP text and background TMP text</returns>

        // BTW, yes, it would be a bit cleaner if this method would send something like TerminalChar[][], where
        // TerminalChar also holds foreground and background colors...
        public (string tmpForegroundText, string tmpBackgroundText) RenderBuffer();

        /// <summary>
        /// Program writes to emulator
        /// DATA: Program -> Emulator
        /// </summary>
        /// <param name="text"></param>
        public void Write(string text);

        public Vector2Int cursorPosition { get; }

        public enum CursorShape { Bar, Block, Underline }
        public CursorShape cursorShape { get; }

        public void SetSize(int xOrColumns, int yOrRows);
    }
}
