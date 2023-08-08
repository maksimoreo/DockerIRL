using System;
using System.Collections.Generic;
using VtNetCore.VirtualTerminal;

namespace DockerIrl
{
    /// <summary>
    /// Virtual Terminal Emulator that uses VtNetCore library. It is also a bridge between VtNetCore, Unity's Input
    /// System, Unity's TextMeshPro, and other components.
    /// </summary>
    public sealed class VtNetCoreTerminalEmulator : ITerminalEmulator
    {
        private static Dictionary<UnityEngine.KeyCode, string> keyInputToVtNetCoreInputMap = new()
        {
            { UnityEngine.KeyCode.UpArrow, "Up" },
            { UnityEngine.KeyCode.LeftArrow, "Left" },
            { UnityEngine.KeyCode.DownArrow, "Down" },
            { UnityEngine.KeyCode.RightArrow, "Right" }
        };

        public UnityEngine.Vector2Int cursorPosition
        {
            get
            {
                var reportedPosition = vtController.CursorState.Position;

                return new UnityEngine.Vector2Int(reportedPosition.Column, reportedPosition.Row);
            }
        }

        public ITerminalEmulator.CursorShape cursorShape
        {
            get
            {
                var reportedCursorShape = vtController.CursorState.CursorShape;

                return
                    reportedCursorShape == VtNetCore.VirtualTerminal.Enums.ECursorShape.Bar ? ITerminalEmulator.CursorShape.Bar
                    : reportedCursorShape == VtNetCore.VirtualTerminal.Enums.ECursorShape.Block ? ITerminalEmulator.CursorShape.Block
                    : ITerminalEmulator.CursorShape.Underline;
            }
        }

        public event Action<string> OnSendData;

        public event Action<string> OnLog;
        public event Action OnBell;

        private VirtualTerminalController vtController;
        private VtNetCore.XTermParser.DataConsumer vtOutputStream;
        private readonly Terminal.VtNetCoreBridge.TerminalTextStringBuilder terminalTextBuilder = new();

        public VtNetCoreTerminalEmulator() { }

        public void Initialize(IPty.PtyOptions ptyOptions, int maxBufferLines = 200)
        {
            vtController = new VirtualTerminalController();
            //vtController.Debugging = true;
            vtController.OnLog += HandleVtOnLog;
            vtController.OnBell += HandleVtOnBell;
            vtController.MaximumHistoryLines = maxBufferLines;
            vtController.ResizeView(columns: ptyOptions.columns, rows: ptyOptions.rows);
            vtOutputStream = new VtNetCore.XTermParser.DataConsumer(vtController);
            vtController.SendData += HandleVtSendData;
            // vtOutputStream.SequenceDebugging = true;
        }

        public void Write(string text)
        {
            vtOutputStream.Write(text);
        }

        public void SendTextInput(char inputChar)
        {
            // In Unity's InputSystem:
            // If user presses "C" key,                     it will send 0x63
            // If user presses "C" key while holding Shift, it will send 0x43
            // If user presses "C" key while holding Ctrl,  it will send 0x03

            // Also, a Reference for ASCII codes: https://www.ascii-code.com/

            if (inputChar == 0x03)
            {
                SendKeyPressedToVtController("\u0003");
            }
            else if (inputChar == 0x08)
            {
                SendKeyPressedToVtController("Back");
            }
            else // if (inputChar >= 32)
            {
                // Add other "if"s for input < 32 if needed

                // From their source code, it looks like VT will process whatever is passed as key as plain text, if it
                // wont find any entry in VtNetCore\VirtualTerminal\KeyboardTranslations.cs
                // It is important when user sends after-255-characters, like "ą".
                SendKeyPressedToVtController(inputChar.ToString());
            }
        }

        public void SendKeyInput(UnityEngine.KeyCode keyCode)
        {
            if (!keyInputToVtNetCoreInputMap.ContainsKey(keyCode)) return;

            SendKeyPressedToVtController(keyInputToVtNetCoreInputMap[keyCode]);
        }

        private void SendKeyPressedToVtController(string input)
        {
            lock (vtController)
            {
                vtController.KeyPressed(input, controlPressed: false, shiftPressed: false);
            }
        }

        public (string tmpForegroundText, string tmpBackgroundText) RenderBuffer()
        {
            List<VtNetCore.VirtualTerminal.Layout.LayoutRow> rows;

            lock (vtController)
            {
                // This operation deep-copies text from VtController. I think. Right?
                rows = vtController.ViewPort.GetPageSpans(
                    startingLine: 0,
                    lineCount: -1
                );
            }

            return terminalTextBuilder.Process(rows);
        }

        public void SetSize(int widthOrColumns, int heightOrRows)
        {
            vtController.ResizeView(columns: widthOrColumns, rows: heightOrRows);
        }

        public void SetMaxBufferLines(int maxBufferLines)
        {
            vtController.MaximumHistoryLines = maxBufferLines;
        }

        private void HandleVtOnLog(object sender, TextEventArgs e)
        {
            OnLog?.Invoke(e.Text);
        }

        private void HandleVtOnBell(object sender, EventArgs e)
        {
            OnBell?.Invoke();
        }

        /// <summary>
        /// Virtual Terminal Emulator sends data to PTY input.
        /// </summary>
        private void HandleVtSendData(object sender, SendDataEventArgs e)
        {
            string text = System.Text.Encoding.UTF8.GetString(e.Data);

            OnSendData?.Invoke(text);
        }
    }
}
