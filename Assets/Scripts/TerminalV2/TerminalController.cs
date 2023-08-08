using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace DockerIrl.Terminal
{
    public class TerminalController : MonoBehaviour
    {
        [Header("Events")]
        [Tooltip("Called when PTY process starts")]
        public UnityEvent OnCommandStart;

        [Tooltip("Called when PTY process exits")]
        public UnityEvent OnCommandExit;

        [Tooltip("Called from Update() if terminal buffer was updated this frame")]
        public UnityEvent OnTerminalBufferUpdated;

        [Tooltip("Called from Update() if input was received this frame")]
        public UnityEvent OnInput;

        [Tooltip("Called when terminal program sends Bell virtual code")]
        public UnityEvent OnBell;

        [Header("Console")]
        // TODO: This field is supposed to be editable from the Editor, but Editor cannot create Dictionary input field, which is needed for env
        // NOTE: Changes to these variables take effect only after terminal session is restarted
        public IPty.PtyOptions ptyOptions;

        [Tooltip("Max console lines")]
        public int maxBufferLines = 100;

        [Header("Internal")]
        public TerminalInput terminalInput;
        public TerminalTextRenderer terminalTextRenderer;
        public TerminalCursor terminalCursor;

        public bool running { get; private set; }

        public int lastExitCode { get; private set; }
        public int currentPid { get => pty == null ? -1 : pty.pid; }

        public string foregroundTextTmpro { get; private set; }
        public string backgroundTextTmpro { get; private set; }

        public bool terminalEmulatorBufferUpdatedThisFrame { get; private set; } = false;
        public bool receivedInputThisFrame { get; private set; } = false;
        private bool invokeOnBellEventNextFrame = false;

        public ITerminalEmulator terminalEmulator { get; private set; }
        private IPty pty;
        private TaskCompletionSource<object> exitPromise;
        private readonly object terminalLock = new();

        private void Update()
        {
            if (terminalEmulator != null)
            {
                lock (terminalLock)
                {
                    if (terminalEmulator != null && terminalEmulatorBufferUpdatedThisFrame)
                    {
                        terminalEmulatorBufferUpdatedThisFrame = false;

                        var (tmpForegroundText, tmpBackgroundText) = terminalEmulator.RenderBuffer();
                        terminalTextRenderer.SetText(tmpForegroundText, tmpBackgroundText);

                        OnTerminalBufferUpdated?.Invoke();
                    }
                }
            }

            if (receivedInputThisFrame)
            {
                receivedInputThisFrame = false;
                OnInput?.Invoke();
            }

            if (invokeOnBellEventNextFrame)
            {
                invokeOnBellEventNextFrame = false;
                OnBell?.Invoke();
            }
        }

        public void StartTerminalSession()
        {
            // Initialize terminal emulator
            terminalEmulator = new VtNetCoreTerminalEmulator();

            terminalEmulator.OnSendData += HandleTerminalEmulatorOnSendData;
            terminalEmulator.OnLog += HandleTerminalEmulatorOnLog;
            terminalEmulator.OnBell += HandleTerminalEmulatorOnBell;

            terminalEmulator.Initialize(
                ptyOptions: ptyOptions,
                maxBufferLines: maxBufferLines
            );

            // Initialize internal components
            terminalInput.OnInputSystemInput += HandleInputSystemInput;
            terminalInput.OnAdditionalInput += HandleAdditionalInput;
            terminalCursor.gameObject.SetActive(true);

            _ = StartPty()
                .ContinueWith((continuationTask) =>
                {
                    UnityMainThreadDispatcher.Instance().Enqueue(() =>
                    {
                        running = true;
                        OnCommandStart?.Invoke();
                    });
                });
        }

        private async Task StartPty()
        {
            pty = new NewPty();
            await pty.StartAsync(ptyOptions);

            // Start a task that constantly reads data from PTY and sends it to our terminalEmulator
            // No need to wait for it ending.
            _ = Task.Factory
                .StartNew(() => ReadAndParseOutputStream(), TaskCreationOptions.LongRunning)
                .ContinueWith((continuationTask) =>
                {
                    HandlePtyExit();
                });
        }

        public void EndTerminalSession()
        {
            pty.Kill();

            // Now waiting for HandlePtyExit callback, further cleanup is done here
        }

        public Task EndTerminalSessionAsync()
        {
            if (exitPromise != null)
            {
                throw new InvalidOperationException("Already requested exit");
            }

            exitPromise = new();
            OnCommandExit.AddListener(HandleOnCommandExitSelf);
            // This will eventually trigger HandleOnCommandExitSelf
            pty.Kill();

            return exitPromise.Task;
        }

        private void HandleOnCommandExitSelf()
        {
            OnCommandExit.RemoveListener(HandleOnCommandExitSelf);
            exitPromise.SetResult(null);
            exitPromise = null;
        }

        public void FocusInput()
        {
            terminalInput.RegisterInputHandlers();
            terminalInput.OnAnyInput += HandleAnyInput;
        }

        public void UnfocusInput()
        {
            if (running)
            {
                terminalInput.UnregisterInputHandlers();
                terminalInput.OnAnyInput -= HandleAnyInput;
            }
        }

        public void Resize(Vector2Int size) => Resize(size.x, size.y);
        public void Resize(int widthOrColumns, int heightOrRows)
        {
            if (!running) return;

            terminalEmulator.SetSize(widthOrColumns, heightOrRows);
        }

        private void HandlePtyExit()
        {
            terminalInput.OnAnyInput -= HandleAnyInput;
            terminalInput.OnInputSystemInput -= HandleInputSystemInput;
            terminalInput.OnAdditionalInput -= HandleAdditionalInput;

            terminalEmulator.OnLog -= HandleTerminalEmulatorOnLog;
            terminalEmulator.OnBell -= HandleTerminalEmulatorOnBell;
            terminalEmulator = null;

            lastExitCode = pty.processExitCode;
            pty = null;

            running = false;

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                terminalCursor.gameObject.SetActive(false);
                terminalInput.UnregisterInputHandlers();

                OnCommandExit?.Invoke();
            });
        }

        private void HandleTerminalEmulatorOnSendData(string data)
        {
            pty.input.Write(data);
        }

        private void HandleTerminalEmulatorOnLog(string text)
        {
            Debug.Log(text);
        }

        private void HandleTerminalEmulatorOnBell()
        {
            invokeOnBellEventNextFrame = true;
        }

        private void HandleInputSystemInput(char c)
        {
            lock (terminalLock)
            {
                terminalEmulator.SendTextInput(c);
            }
        }

        private void HandleAdditionalInput(KeyCode keyCode)
        {
            lock (terminalLock)
            {
                terminalEmulator.SendKeyInput(keyCode);
            }
        }

        private void HandleAnyInput()
        {
            receivedInputThisFrame = true;
        }

        /// <summary>
        /// Constantly read data from PTY output and send it to a Virtual Terminal Emulator.
        /// </summary>
        private void ReadAndParseOutputStream()
        {
            int c;
            while (true)
            {
                try
                {
                    if (pty.output.EndOfStream)
                    {
                        Debug.LogWarning("End of stream");
                        break;
                    }

                    c = pty.output.Read();
                    if (c == 0)
                    {
                        Debug.LogWarning("0 byte");
                        break;
                    }
                }
                catch (ObjectDisposedException)
                {
                    Debug.unityLogger.LogWarning(LogTags.Pty, "Reader thread exited due to somewhat expected exception but might have not read full output. Please find this text in source code and fix it.");

                    // FIXME: This happens because NewPty.HandleProcessExited disposes output stream instantly, or too
                    // early, without giving a chance for this thread to read remaining output from stream.
                    // I believe better solution is to allow this thread to read til `output.EndOfStream` or
                    // `output.Read() == 0`, and then signal IPty instance to dispose items. However on Windows,
                    // output buffer is not always populated with 0, making it impossible to know if output stream is
                    // empty (closed?), as `output.Read()` will just hang.

                    // Currently, we can fix this by waiting some delay (1sec) after pty exited to allow this thread to
                    // read all remaining data from the stream. After a second, pty thread should dispose output
                    // stream, this way generating an expected exception on hanging `output.Read()`. This still is a
                    // hacky solution, but at least allows to extract some output...

                    // I believe these are related
                    // https://github.com/microsoft/terminal/discussions/15006
                    // https://github.com/microsoft/terminal/issues/4564

                    // maybe im a potato and there is an obvious better way to do this, but there are literally 0
                    // examples on the internet on how to work with conpty :(

                    // thx for listening to my ted talk, dont forget to drink water

                    break;
                }

                string output = ((char)c).ToString();

                // Note: If program sends 999 bytes, lock will be locked and released 999 times... Can this be optimized?
                // Cannot use .Read(buffer, 0, length) as it will block until there is enough available data to
                // fill the buffer, similar to .ReadBlock(buffer, 0, length), despite whats written in docs. test
                // urself first, before changing this code.
                lock (terminalLock)
                {
                    terminalEmulator.Write(output);
                    terminalEmulatorBufferUpdatedThisFrame = true;
                }
            }

            Debug.Log("ReadStream thread done");
        }
    }
}
