using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DockerIrl
{
    public interface IPty
    {
        [Serializable]
        public record PtyOptions
        {
            public string command;
            public string[] args;
            public string cwd;
            public Dictionary<string, string> env;

            // Console WIDTH, number of character COLUMNS
            public int columns;

            // Console HEIGHT, number of character ROWS
            public int rows;

            public Serialization.SerializedTerminal.SerializedTerminalConsole ToSerializedTerminalConsole() => new()
            {
                command = command,
                args = args,
                cwd = cwd,
                columns = columns,
                rows = rows,
                env = env,
            };
        }

        public StreamWriter input { get; }

        public StreamReader output { get; }

        /// <summary>
        /// Process exit code reported by the OS. Available after output stream is closed;
        /// </summary>
        public int processExitCode { get; }
        public int pid { get; }

        /// <summary>
        /// Invoked once the process has been spawned and is ready to receive input.
        /// </summary>
        public event Action OnReady;

        public void Start(PtyOptions options);
        public Task StartAsync(PtyOptions options);

        /// <summary>
        /// Asks a process to stop immediatelly. U can get notified if the process really closes when output stream is closed.
        /// </summary>
        public void Kill();
    }
}
