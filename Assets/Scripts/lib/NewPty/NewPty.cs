using Pty.Net;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DockerIrl
{
    public class NewPty : IPty
    {
        public StreamWriter input { get; private set; }
        public StreamReader output { get; private set; }

        public int processExitCode { get; private set; }
        public int pid { get; private set; }

        public event Action OnReady;

        private IPtyConnection ptyConnection;

        public void Start(IPty.PtyOptions ptyOptions)
        {
            _ = StartAsync(ptyOptions);
        }

        public async Task StartAsync(IPty.PtyOptions ptyOptions)
        {
            try
            {
                UnityEngine.Debug.unityLogger.Log(LogTags.Pty, $"Creating a PTY ({ptyOptions.command})");

                ptyConnection = await PtyProvider.SpawnAsync(
                    new PtyOptions()
                    {
                        App = ptyOptions.command,
                        Cwd = ptyOptions.cwd,
                        Cols = ptyOptions.columns,
                        Rows = ptyOptions.rows,
                        Environment = ptyOptions.env,
                        CommandLine = ptyOptions.args,
                    },
                    CancellationToken.None
                );
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
                throw;
            }

            pid = ptyConnection.Pid;
            UnityEngine.Debug.unityLogger.Log(LogTags.Pty, $"Created PTY with PID: {pid}");

            ptyConnection.ProcessExited += HandleProcessExited;

            input = new StreamWriter(ptyConnection.WriterStream)
            {
                AutoFlush = true
            };

            output = new StreamReader(ptyConnection.ReaderStream);

            OnReady?.Invoke();
        }

        public void Kill()
        {
            UnityEngine.Debug.unityLogger.Log(LogTags.Pty, $"Trying to kill PTY (PID: {pid})");
            ptyConnection.Kill();
        }

        private void HandleProcessExited(object sender, PtyExitedEventArgs eventArgs)
        {
            processExitCode = eventArgs.ExitCode;
            UnityEngine.Debug.unityLogger.Log(LogTags.Pty, $"PTY (PID: {pid}) exited with code: {processExitCode}");

            input.Dispose();
            output.Dispose();
            ptyConnection.Dispose();
        }
    }
}
