using DockerIrl.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    [Serializable]
    public record SerializedSettings
    {
        [Serializable]
        public record General
        {
            [Serializable]
            public record NewContainerConfiguration
            {
                public bool persist;
                public string matchDockerContainerBy;
                public string highlightTextTemplate;
                public List<SerializedTerminal> terminals = new();
            }

            public bool automaticallyCreateDockerIrlContainerForNewDockerContainer = true;
            public bool removeRunningTerminals = false;
            public string loadStateFilePath = "state.json";
            public string saveStateFilePath = "state.json";
            public float characterMoveSpeed = 4;
            public float characterSprintSpeed = 6;
            public float dockerStateQueryIntervalSeconds = 10;
            public float autoSaveIntervalSeconds = 60;
            public float interactRange = 10;
            public bool breathing = true;
            public bool saveOnClose = true;
            public string loadKeybindsFilePath = "keybinds.json";
            public bool showDebugActions = false;

            public NewContainerConfiguration newContainerConfiguration;
        }
        public General general = new();

        [Serializable]
        public record Sound
        {
            public float steps = 1;
            public float terminal = 1;
        }
        public Sound sound = new();
    }


    public class Settings : MonoBehaviour
    {
        public string loadConfigPath = "settings.json";
        public string saveConfigPath = "Assets/ThisIsGitIgnored/settings.json";

        /// <summary>
        /// Main thread;
        /// </summary>
        public event Action onSettingsChanged;

        public bool automaticallyCreateDockerIrlContainerForNewDockerContainer { get => x.general.automaticallyCreateDockerIrlContainerForNewDockerContainer; }
        public bool removeRunningTerminals { get => x.general.removeRunningTerminals; }
        public string loadStateFilePath { get => x.general.loadStateFilePath; }
        public string saveStateFilePath { get => x.general.saveStateFilePath; }

        public SerializedSettings x;

        public async Task LoadFromFile()
        {
            Debug.Log($"Loading config from file: \"{loadConfigPath}\"");

            try
            {
                string jsonText = await File.ReadAllTextAsync(loadConfigPath);
                x = JsonConvert.DeserializeObject<SerializedSettings>(jsonText);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Debug.LogWarning($"Failed to load config from file: \"{loadConfigPath}\"");
            }

            Debug.Log("Notifying listeners about settings update...");
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                onSettingsChanged?.Invoke();
            });

            Debug.Log("Done loading config.");
        }

        public async void SaveToFile()
        {
            string jsonText = JsonConvert.SerializeObject(x, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            });

            await File.WriteAllTextAsync(saveConfigPath, jsonText);
        }

        public void ResetSettingsToDefaults()
        {
            x = default;
        }
    }
}
