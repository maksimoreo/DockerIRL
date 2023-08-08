using DockerIrl.Serialization;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Serializes state and saves it to a file.
    /// </summary>
    public class StateSerializer : MonoBehaviour
    {
        public Settings settings;
        public GeneralCharacterBehaviour characterBehaviour;
        public ContainerStore containerStore;
        public TerminalStore terminalStore;

        private string saveStateFilePath { get => settings.saveStateFilePath; }

        public void SaveStateToFile()
        {
            Debug.Log("Serializing state...");
            var serializedState = new SerializedFileState()
            {
                player = characterBehaviour.ToSerializableObject(),
                containers = containerStore.store.Select((container) => container.ToSerializableObject()).ToList(),
                terminals = terminalStore.freeTerminals.Select((terminal) => terminal.ToSerializeableObject()).ToList(),
            };

            Debug.Log("Converting to JSON...");
            var json = JsonConvert.SerializeObject(serializedState, new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            });

            Debug.Log($"Writing to \"{saveStateFilePath}\"...");
            File.WriteAllText(saveStateFilePath, json);

            Debug.Log("Done.");
        }
    }
}
