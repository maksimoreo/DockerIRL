using Docker.DotNet;
using Docker.DotNet.Models;
using DockerIrl.Utils;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    public class LoadAndApplyDockerState
    {
        private readonly ILogger logger;
        private readonly ContainerStore containerStore;
        private readonly DockerClient dockerClient;

        public LoadAndApplyDockerState(ILogger logger, ContainerStore containerStore, DockerClient dockerClient)
        {
            this.logger = logger;
            this.containerStore = containerStore;
            this.dockerClient = dockerClient;
        }

        public async Task Call()
        {
            logger.Log("Loading Docker state...");

            var dockerState = await DataLoader.FetchDockerState(logger, dockerClient);

            if (dockerState == null) return;

            logger.Log("Loaded Docker state, applying...");

            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                ApplyDockerState(dockerState);
            });

            logger.Log("Loaded and applied Docker state.");
        }

        public void ApplyDockerState(IList<ContainerListResponse> dockerState)
        {
            var (matchedPairs, unmatchedFirstListItems, unmatchedSecondListItems) = X.PartitionLists(
                containerStore.store,
                dockerState,
                (dockerIrlContainer, dockerContainer) => dockerIrlContainer.MatchDocker(dockerContainer)
            );

            logger.Log($"Removing {unmatchedFirstListItems.Count} DockerIRL containers.");
            int removedContainersCount = 0;
            foreach (var dockerIrlContainer in unmatchedFirstListItems)
            {
                if (dockerIrlContainer.persist)
                {
                    logger.Log($"Will not remove unmatched DockerIRL container \"{dockerIrlContainer}\" as it is marked with \"persist\".");
                    continue;
                }

                logger.Log($"Removing DockerIRL container \"{dockerIrlContainer.id}\".");
                containerStore.RemoveContainer(dockerIrlContainer);

                removedContainersCount++;
            }

            logger.Log($"Updating {matchedPairs.Count} DockerIRL containers.");
            int updatedContainersCount = 0;
            foreach (var (dockerIrlContainer, dockerContainer) in matchedPairs)
            {
                logger.Log($"Updating DockerIRL container \"{dockerIrlContainer.id}\" with Docker container \"{dockerContainer.ID}\".");
                dockerIrlContainer.docker.Update(dockerContainer);

                updatedContainersCount++;
            }

            logger.Log($"Adding {unmatchedSecondListItems.Count} DockerIRL containers");
            int addedContainersCount = 0;
            foreach (var dockerContainer in unmatchedSecondListItems)
            {
                logger.Log($"Creating new DockerIRL container for Docker container \"{dockerContainer.ID}\".");

                var dockerIrlContainer = CreateDockerIrlContainer(dockerContainer);

                if (dockerIrlContainer == null)
                {
                    logger.Log($"Did not create container.");
                    continue;
                }

                addedContainersCount++;
            }

            logger.Log($"Removed {removedContainersCount} containers, updated {updatedContainersCount} containers and added {addedContainersCount} containers");
        }

        private ContainerBehaviour CreateDockerIrlContainer(ContainerListResponse dockerContainer)
        {
            var dockerIrlContainer = containerStore.InstantiateNewContainer(
                dockerId: dockerContainer.ID,
                dockerName: dockerContainer.Names[0]
            );

            dockerIrlContainer.docker.Update(dockerContainer);

            return dockerIrlContainer;
        }
    }
}
