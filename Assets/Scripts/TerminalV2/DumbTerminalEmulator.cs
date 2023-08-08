using System;
using System.Collections.Generic;

namespace DockerIrl
{
    // Gets PTY's output stream and does NOT parse it
    public class DumbTerminalEmulator : ITerminalEmulator
    {
        public static Dictionary<int, string> nonPrintableCharactersMap = new()
        {
            { 0, "<NUL>" },
            { 1, "<SOH>" },
            { 2, "<STX>" },
            { 3, "<ETX>" },
            { 4, "<EOT>" },
            { 5, "<ENQ>" },
            { 6, "<ACK>" },
            { 7, "<BEL>" },
            { 8, "<BS>" },
            { 9, "<HT>" },
            { 10, "<LF>" },
            { 11, "<VT>" },
            { 12, "<FF>" },
            { 13, "<CR>" },
            { 14, "<SO>" },
            { 15, "<SI>" },
            { 16, "<DLE>" },
            { 17, "<DC1>" },
            { 18, "<DC2>" },
            { 19, "<DC3>" },
            { 20, "<DC4>" },
            { 21, "<NAK>" },
            { 22, "<SYN>" },
            { 23, "<ETB>" },
            { 24, "<CAN>" },
            { 25, "<EM>" },
            { 26, "<SUB>" },
            { 27, "<ESC>" },
            { 28, "<FS>" },
            { 29, "<GS>" },
            { 30, "<RS>" },
            { 31, "<US>" },
        };

        public UnityEngine.Vector2Int cursorPosition { get; } = UnityEngine.Vector2Int.zero;
        public ITerminalEmulator.CursorShape cursorShape { get; } = ITerminalEmulator.CursorShape.Bar;

        public event Action<string> OnSendData;

#pragma warning disable 0169
        // Warning disable reason: Most of these callbacks are never called, but are required by an interface. The
        // point of this class is to provide minimal working implementation anyway...

        public event Action<string> OnLog;
        public event Action OnBell;
#pragma warning restore 0169

        // TODO: Use this
        private int maxBufferLines = 200;

        private string buffer;

        public DumbTerminalEmulator() { }

        public void Initialize(IPty.PtyOptions ptyOptions, int maxBufferLines = 200)
        {
            this.maxBufferLines = maxBufferLines;
        }

        public void Write(string text)
        {
            buffer += text;
        }

        public void SendTextInput(char input)
        {
            OnSendData?.Invoke(input.ToString());
        }

        public void SendKeyInput(UnityEngine.KeyCode keyCode) { }

        public (string tmpForegroundText, string tmpBackgroundText) RenderBuffer()
        {
            return (buffer, "");
        }

        public void SetMaxBufferLines(int maxBufferLines)
        {
            this.maxBufferLines = maxBufferLines;
        }

        public void SetSize(int widthOrColumns, int heightOrRows)
        {
            // do nothing
        }
    }
}
