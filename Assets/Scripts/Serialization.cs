using DockerIrl.Utils;
using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace DockerIrl.Serialization
{
    public record SerializedFileState
    {
        [JsonProperty(Order = 0)]
        public SerializedPlayer player;

        [JsonProperty(Order = 1)]
        public IList<SerializedContainer> containers = new List<SerializedContainer>();

        [JsonProperty(Order = 2)]
        public IList<SerializedTerminal> terminals = new List<SerializedTerminal>();
    }

    public record SerializedPlayer
    {
        public Vector3 position;
        public Vector2 rotation;
    }

    public record SerializedContainer : IHasId
    {
        [JsonProperty(Order = 0)]
        public string id { get; set; }

        [JsonProperty(Order = 1)]
        public Vector3 position;

        [JsonProperty(Order = 2)]
        public Vector3 rotation;

        [JsonProperty(Order = 3)]
        public string modelId;

        [JsonProperty(Order = 4)]
        public string matchId;

        [JsonProperty(Order = 5)]
        public bool persist;

        [JsonProperty(Order = 6)]
        public string highlightTextTemplate = @"Container
{{containerId}}
Docker: {{dockerId}} - {{dockerStatus}}
";

        [JsonProperty(Order = 7)]
        public List<SerializedTerminal> terminals;
    }

    public record SerializedTerminal : IHasId
    {
        [JsonProperty(Order = 0)]
        public string id { get; set; }

        [JsonProperty(Order = 1)]
        public Vector3 position;

        [JsonProperty(Order = 2)]
        public Vector3 rotation;

        [JsonProperty(Order = 3)]
        public bool skipProcessStart = false;

        public record SerializedTerminalConsole
        {
            [JsonProperty(Order = 1)]
            public string command;

            [JsonProperty(Order = 2)]
            public string[] args;

            [JsonProperty(Order = 3)]
            public string cwd;

            [JsonProperty(Order = 4)]
            public Dictionary<string, string> env;

            [JsonProperty(Order = 5)]
            public int columns;

            [JsonProperty(Order = 6)]
            public int rows;

            public IPty.PtyOptions ToPtyOptions() => new()
            {
                command = command,
                args = args,
                cwd = cwd,
                columns = columns,
                rows = rows,
                env = env,
            };
        }

        [JsonProperty(Order = 4)]
        public SerializedTerminalConsole console = new()
        {
            command = "pwsh",
            args = new string[0],
            cwd = "",
            env = new Dictionary<string, string>(),
            columns = 80,
            rows = 32,
        };

        public record SerializedTerminalCamera
        {
            [JsonProperty(Order = 1)]
            public float fov;

            [JsonProperty(Order = 2)]
            public float distance;
        }

        [JsonProperty(Order = 5)]
        public SerializedTerminalCamera camera = new()
        {
            fov = 30,
            distance = 1.5f,
        };

        [JsonProperty(Order = 6)]
        public Vector2 monitorSize = new(1, 0.75f);

        [JsonProperty(Order = 7)]
        public Vector2 canvasSize = new(640, 480);

        [JsonProperty(Order = 8)]
        public Vector2 canvasScale = new(0.0015f, 0.0015f);

        [JsonProperty(Order = 9)]
        public float fontSize = 12;

        [JsonProperty(Order = 10)]
        public Rect cursorBarShapeRect = new(0, 0, 1, 14.7f);

        [JsonProperty(Order = 11)]
        public Rect cursorBlockShapeRect = new(0, 0, 7.4f, 14.7f);

        [JsonProperty(Order = 12)]
        public Rect cursorUnderlineShapeRect = new(0, -13.7f, 7.4f, 1);

        [JsonProperty(Order = 13)]
        public Vector2 cursorCharacterSize = new(7.4f, -14.75f);

        [JsonProperty(Order = 14)]
        public string highlightTextTemplate = @"Terminal ""{{id}}""
{{command}}
{{isRunningText}}
[E] - Use
";

        [JsonProperty(Order = 15)]
        public string defaultIdleTextTemplate = "\n\n\n\n\n\n\n\n\n\t\tTerminal \"{{id}}\"\n\n\n\t\tCommand:\n\t\t{{command}}\n\n\n\t\t[E] - Use";
    }
}
