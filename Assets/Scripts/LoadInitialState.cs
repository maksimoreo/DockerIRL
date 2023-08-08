using Docker.DotNet;
using Docker.DotNet.Models;
using DockerIrl.ContainerManagement;
using DockerIrl.Serialization;
using DockerIrl.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Loads initial state
    /// </summary>
    public class LoadInitialState
    {
        public record IntermediateInitialContainerConfiguration
        {
            public SerializedContainer fileContainer;
            public ContainerListResponse dockerContainer;
        }

        private readonly ILogger logger;
        private readonly TerminalStore terminalStore;
        private readonly ContainerStore containerStore;
        private readonly DockerClient dockerClient;
        private readonly GeneralCharacterBehaviour player;
        private readonly Settings settings;

        public LoadInitialState(ILogger logger, TerminalStore terminalStore, ContainerStore containerStore, DockerClient dockerClient, GeneralCharacterBehaviour player, Settings settings)
        {
            this.logger = logger;
            this.terminalStore = terminalStore;
            this.containerStore = containerStore;
            this.dockerClient = dockerClient;
            this.player = player;
            this.settings = settings;
        }

        public async Task Call()
        {
            logger.Log("Loading initial state...");

            // Fetching states from file & Docker in parallel
            var fetchDockerStateTask = DataLoader.FetchDockerState(logger, dockerClient);
            var fetchFileStateTask = DataLoader.FetchAndValidateFileState(logger, settings);

            var dockerState = await fetchDockerStateTask.ConfigureAwait(false);
            var fileState = await fetchFileStateTask.ConfigureAwait(false);

            logger.Log("Performing merge & valiation.");

            IList<IntermediateInitialContainerConfiguration> containers;

            if (dockerState != null && fileState != null)
            {
                containers = MergeDockerAndFileContainers(dockerState, fileState.containers);
            }
            else if (dockerState == null && fileState != null)
            {
                containers = fileState
                    .containers
                    .Select((fileContainer) => new IntermediateInitialContainerConfiguration()
                    {
                        fileContainer = fileContainer,
                    })
                    .ToList();
            }
            else if (dockerState != null && fileState == null)
            {
                containers = dockerState
                    .Select((dockerContainer) => new IntermediateInitialContainerConfiguration()
                    {
                        dockerContainer = dockerContainer,
                    })
                    .ToList();
            }
            else
            {
                containers = null;
            }

            logger.Log("Sending work to main thread.");

            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                if (containers != null)
                {
                    ApplyContainersInitialState(containers);
                }

                if (fileState != null)
                {
                    ApplyTerminalsInitialState(fileState.terminals);
                    ApplyPlayerState(fileState.player);
                }
            });

            logger.Log("Completed loading initial state.");
        }

        public void ApplyContainersInitialState(IList<IntermediateInitialContainerConfiguration> containers)
        {
            logger.Log($"Adding {containers.Count} initial containers.");

            foreach (var container in containers)
            {
                CreateDockerIrlContainer(container);
            }

            logger.Log($"Added {containers.Count} initial containers.");
        }

        private void ApplyTerminalsInitialState(IList<SerializedTerminal> terminals)
        {
            logger.Log($"Adding {terminals.Count} initial terminals.");

            foreach (var terminal in terminals)
            {
                logger.Log($"Adding \"{terminal.id}\" terminal.");
                terminalStore.InstantiateFreeTerminal(terminal);
            }

            logger.Log($"Added {terminals.Count} terminals.");
        }

        private void ApplyPlayerState(SerializedPlayer player)
        {
            logger.Log("Applying player state");

            this.player.characterController.enabled = false;
            this.player.transform.position = player.position;
            this.player.characterController.enabled = true;

            this.player.SetCameraRotation(player.rotation);
        }

        /// <summary>
        /// Merges container configurations fetched from state file and Docker.
        /// </summary>
        /// <param name="dockerContainers">Containers fetched from Docker</param>
        /// <param name="fileContainers">Containers fetched from state file</param>
        private IList<IntermediateInitialContainerConfiguration> MergeDockerAndFileContainers(
            IList<ContainerListResponse> dockerContainers,
            IList<SerializedContainer> fileContainers
        )
        {
            var mergedState = new List<IntermediateInitialContainerConfiguration>();

            var (matchedPairs, unmatchedFileContainers, unmatchedDockerContainers) = X.PartitionLists(
                fileContainers,
                dockerContainers,
                (fileContainer, dockerContainer) =>
                    fileContainer.matchId == dockerContainer.ID || fileContainer.matchId == dockerContainer.Names[0]
            );

            foreach (var (fileContainer, dockerContainer) in matchedPairs)
            {
                mergedState.Add(
                    new IntermediateInitialContainerConfiguration()
                    {
                        fileContainer = fileContainer,
                        dockerContainer = dockerContainer,
                    }
                );
            }

            foreach (var fileContainer in unmatchedFileContainers)
            {
                if (fileContainer.persist)
                {
                    mergedState.Add(
                        new IntermediateInitialContainerConfiguration()
                        {
                            fileContainer = fileContainer,
                        }
                    );
                }
            }

            foreach (var dockerContainer in unmatchedDockerContainers)
            {
                if (settings.automaticallyCreateDockerIrlContainerForNewDockerContainer)
                {
                    mergedState.Add(
                        new IntermediateInitialContainerConfiguration()
                        {
                            dockerContainer = dockerContainer,
                        }
                    );
                }
            }

            return mergedState;
        }

        private ContainerBehaviour CreateDockerIrlContainer(IntermediateInitialContainerConfiguration intermediateContainerConfiguration)
        {
            var fileContainer = intermediateContainerConfiguration.fileContainer;
            var dockerContainer = intermediateContainerConfiguration.dockerContainer;

            ContainerBehaviour dockerIrlContainer;

            if (fileContainer != null)
            {
                logger.Log($"Adding container from file ({fileContainer.id}).");
                dockerIrlContainer = containerStore.InstantiateContainer(fileContainer);

                if (dockerContainer != null)
                {
                    dockerIrlContainer.docker.Update(dockerContainer);
                }
            }
            else // if (dockerContainer != null)
            {
                logger.Log($"Adding container from Docker ({dockerContainer.ID}).");
                dockerIrlContainer = containerStore.InstantiateNewContainer(
                    dockerId: dockerContainer.ID,
                    dockerName: dockerContainer.Names[0]
                );

                dockerIrlContainer.docker.Update(dockerContainer);
            }

            return dockerIrlContainer;
        }
    }
}
