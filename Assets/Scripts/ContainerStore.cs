using DockerIrl.ContainerManagement;
using DockerIrl.Serialization;
using DockerIrl.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace DockerIrl
{
    public class ContainerStore : MonoBehaviour
    {
        public WeightedPrefabsPicker.WeightedPrefabsPickerUnityEditorInput containerPrefabsEditorInput;
        public WeightedPrefabsPicker containerPrefabs;

        public ReadOnlyCollection<ContainerBehaviour> store { get; private set; }
        private readonly List<ContainerBehaviour> _store = new();

        public UnityEvent<ContainerBehaviour> onContainerAdded;

        [Header("Global components")]
        public TerminalStore terminalStore;
        public Settings settings;
        public FreeContainerSpotFinder freeContainerSpotFinder;

        [Header("Internal components")]
        public Transform containersParent;

        void Awake()
        {
            containerPrefabs = containerPrefabsEditorInput.ToWeightedPrefabsPicker();
            store = _store.AsReadOnly();
        }

        public void RemoveContainer(ContainerBehaviour container)
        {
            _store.Remove(container);
            Destroy(container.gameObject);
        }

        public ContainerBehaviour InstantiateContainer(SerializedContainer serializedContainer)
        {
            var containerRotationQuaternion = Quaternion.Euler(serializedContainer.rotation);

            // Find model
            WeightedPrefabsPicker.WeightedPrefab weightedPrefab = containerPrefabs.FindById(serializedContainer.modelId);
            if (weightedPrefab == null)
            {
                Debug.unityLogger.LogWarning(LogTags.ContainerStore, $"Invalid modelId: \"{serializedContainer.modelId}\", will pick random model.");
                weightedPrefab = containerPrefabs.Pick();
            }

            // Instantiate container
            ContainerBehaviour containerBehaviour = InstantiateContainer(
                id: serializedContainer.id,
                weightedPrefab: weightedPrefab,
                position: serializedContainer.position,
                rotation: containerRotationQuaternion,
                terminals: serializedContainer.terminals
            );

            // Initialize
            containerBehaviour.persist = serializedContainer.persist;
            containerBehaviour.matchId = serializedContainer.matchId;
            containerBehaviour.highlightTextTemplate = serializedContainer.highlightTextTemplate;

            return containerBehaviour;
        }

        public ContainerBehaviour InstantiateNewContainer(string dockerId, string dockerName)
        {
            // Find free spot
            var positionFinderResult = freeContainerSpotFinder.FindFreeSpot();
            if (positionFinderResult == null)
            {
                Debug.unityLogger.LogWarning(LogTags.ContainerStore, "Could not find free spot for a contianer");
                return null;
            }

            var newContainerConfiguration = settings.x.general.newContainerConfiguration;

            bool matchDockerByName = newContainerConfiguration.matchDockerContainerBy == "name";
            string matchDocker = matchDockerByName ? dockerName : dockerId;

            string renderTemplate(string template) => X.FormatStringTemplate(template, new()
            {
                { "matchId", matchDocker },
            });

            // Instantiate container
            ContainerBehaviour containerBehaviour = InstantiateContainer(
                id: X.GenerateUUID(),
                weightedPrefab: containerPrefabs.Pick(),
                position: positionFinderResult.position,
                rotation: positionFinderResult.rotation,
                terminals: GetNewContainerTerminalConfigurations(
                    renderTemplate: renderTemplate
                )
            );

            // Initialize
            containerBehaviour.matchId = matchDockerByName ? dockerName : dockerId;
            containerBehaviour.highlightTextTemplate = newContainerConfiguration.highlightTextTemplate;

            return containerBehaviour;
        }

        public ContainerBehaviour InstantiateContainer(
            string id,
            WeightedPrefabsPicker.WeightedPrefab weightedPrefab,
            Vector3 position,
            Quaternion rotation,
            List<SerializedTerminal> terminals // TODO: Must be List<TerminalCreationParams>
        )
        {
            // Instantiate container
            GameObject containerGameObject = Instantiate(weightedPrefab.prefab, position, rotation, containersParent);
            containerGameObject.name = $"Container {id}";

            // Initialize
            ContainerBehaviour containerBehaviour = containerGameObject.GetComponent<ContainerBehaviour>();
            containerBehaviour.id = id;
            containerBehaviour.prefabId = weightedPrefab.id;

            // Instantiate terminals
            foreach (var serializedTerminal in terminals)
            {
                Vector3 terminalPosition = position + rotation * serializedTerminal.position;
                Quaternion terminalRotation = rotation * Quaternion.Euler(serializedTerminal.rotation);

                terminalStore.InstantiateContainerTerminal(
                    serializedTerminal with
                    {
                        position = terminalPosition,
                        rotation = terminalRotation.eulerAngles,
                    },
                    containerBehaviour
                );
            }

            // Finalize
            _store.Add(containerBehaviour);

            return containerBehaviour;
        }

        private List<SerializedTerminal> GetNewContainerTerminalConfigurations(Func<string, string> renderTemplate)
        {
            var newContainerTerminalsConfigurations = settings.x?.general?.newContainerConfiguration?.terminals;

            if (newContainerTerminalsConfigurations == null)
            {
                return new();
            }

            return newContainerTerminalsConfigurations
                .Select((newContainerTerminalConfiguration) => newContainerTerminalConfiguration with
                {
                    id = X.GenerateUUID(),
                    console = newContainerTerminalConfiguration.console with
                    {
                        command = renderTemplate(newContainerTerminalConfiguration.console.command),
                        args = newContainerTerminalConfiguration.console.args.Select((arg) => renderTemplate(arg)).ToArray(),
                        cwd = renderTemplate(newContainerTerminalConfiguration.console.cwd),
                    },
                })
                .ToList();
        }
    }
}