using DockerIrl.Serialization;
using DockerIrl.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    public class LoadAndApplyFileState
    {
        public ILogger logger;
        public TerminalStore terminalStore;
        public ContainerStore containerStore;
        public Settings settings;

        public async Task Call()
        {
            logger.Log("Loading file state...");

            var fileState = await DataLoader.FetchAndValidateFileState(logger, settings);

            if (fileState == null) return;

            // Validate state, terminal placement, etc.

            if (!ValidateUpdatingFileState(fileState)) return;

            logger.Log("Loaded state from file, now applying...");

            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                ApplyFileStateContainers(fileState.containers);
                ApplyFileStateTerminals(fileState.terminals);
            });

            logger.Log("Loaded and applied state from File.");
        }

        /// <summary>
        /// Validates given file state in respect to updating current state
        /// </summary>
        /// <param name="fileState"></param>
        /// <returns></returns>
        private bool ValidateUpdatingFileState(SerializedFileState fileState)
        {
            logger.Log("Validating updating file state.");

            if (!settings.removeRunningTerminals)
            {
                var allFileTerminals = fileState
                    .containers
                    .SelectMany((container) => container.terminals)
                    .Concat(fileState.terminals);

                var runningTerminalsToBeRemoved = terminalStore
                    .allTerminals
                    .Where((dockerIrlTerminal) =>
                        dockerIrlTerminal.terminalController.running &&
                            !allFileTerminals.Any((fileTerminal) => dockerIrlTerminal.id == fileTerminal.id)
                    );

                if (runningTerminalsToBeRemoved.Count() > 0)
                {
                    var ids = X.ToIdStringList(runningTerminalsToBeRemoved);
                    logger.Log($"Applying this configuration would remove {runningTerminalsToBeRemoved.Count()} running terminals: {ids} Either remove them or enable \"removeRunningTerminals\" setting.");
                    return false;
                }
            }

            logger.Log("Passed updating validation.");

            return true;
        }

        public void ApplyFileStateContainers(IList<SerializedContainer> fileContainers)
        {
            var (matchedPairs, unmatchedFirstListItems, unmatchedSecondListItems) = X.PartitionLists(
                containerStore.store,
                fileContainers,
                X.IdMatch
            );

            logger.Log($"Removing {unmatchedFirstListItems.Count} containers.");
            foreach (var dockerIrlContainer in unmatchedFirstListItems)
            {
                logger.Log($"Removing container \"{dockerIrlContainer.id}\".");
                containerStore.RemoveContainer(dockerIrlContainer);
            }

            logger.Log($"Updating {matchedPairs.Count} containers.");
            foreach (var (dockerIrlContainer, fileContainer) in matchedPairs)
            {
                logger.Log($"Updating container \"{dockerIrlContainer.id}\".");
                UpdateDockerIrlContainer(dockerIrlContainer, fileContainer);
            }

            logger.Log($"Adding {unmatchedSecondListItems.Count} containers");
            foreach (var item in unmatchedSecondListItems)
            {
                logger.Log($"Adding container \"{item.id}\".");
                CreateDockerIrlContainer(item);
            }

            logger.Log($"Removed {unmatchedFirstListItems.Count} containers, updated {matchedPairs.Count} countainers and added {unmatchedSecondListItems.Count} containers.");
        }

        public void ApplyFileStateTerminals(IList<SerializedTerminal> fileTerminals)
        {
            logger.Log($"Free terminals count before updating: {terminalStore.freeTerminals.Count}.");

            var (matchedPairs, unmatchedExistingTerminals, unmatchedFileTerminals) = X.PartitionLists(
                terminalStore.freeTerminals,
                fileTerminals,
                X.IdMatch
            );

            logger.Log($"Removing {unmatchedExistingTerminals.Count} terminals.");
            foreach (var dockerIrlTerminal in unmatchedExistingTerminals)
            {
                logger.Log($"Removing terminal \"{dockerIrlTerminal}\".");
                terminalStore.DestroyTerminal(dockerIrlTerminal);
            }

            logger.Log($"Updating {matchedPairs.Count} terminals.");
            foreach (var (dockerIrlTerminal, fileTerminal) in matchedPairs)
            {
                logger.Log($"Updating terminal \"{dockerIrlTerminal.id}\".");
                UpdateTerminal(dockerIrlTerminal, fileTerminal);
            }

            logger.Log($"Adding {unmatchedFileTerminals.Count}.");
            foreach (var fileTerminal in unmatchedFileTerminals)
            {
                logger.Log($"Adding terminal \"{fileTerminal.id}\".");
                terminalStore.InstantiateFreeTerminal(fileTerminal);
            }

            logger.Log($"Free terminals count after update: {terminalStore.freeTerminals.Count}.");
        }

        private ContainerBehaviour CreateDockerIrlContainer(SerializedContainer fileContainer)
        {
            var dockerIrlContainer = containerStore.InstantiateContainer(fileContainer);

            logger.Log($"Created container as \"{dockerIrlContainer.id}\"");

            return dockerIrlContainer;
        }

        private void UpdateDockerIrlContainer(ContainerBehaviour dockerIrlContainer, SerializedContainer fileContainer)
        {
            dockerIrlContainer.transform.position = fileContainer.position;
            dockerIrlContainer.transform.eulerAngles = fileContainer.rotation;
            dockerIrlContainer.matchId = fileContainer.matchId;
            dockerIrlContainer.persist = fileContainer.persist;

            UpdateDockerIrlContainerTerminals(dockerIrlContainer, fileContainer);
        }

        private void UpdateDockerIrlContainerTerminals(ContainerBehaviour dockerIrlContainer, SerializedContainer fileContainer)
        {
            var (matchedPairs, unmatchedFirstListItems, unmatchedSecondListItems) = X.PartitionLists(
                dockerIrlContainer.terminals,
                fileContainer.terminals,
                X.IdMatch
            );

            logger.Log($"Removing {unmatchedFirstListItems.Count} terminals.");
            foreach (var dockerIrlTerminal in unmatchedFirstListItems)
            {
                logger.Log($"Removing terminal \"{dockerIrlTerminal.id}\".");
                terminalStore.DestroyTerminal(dockerIrlTerminal);
            }

            logger.Log($"Updating {matchedPairs.Count} terminals.");
            foreach (var (dockerIrlTerminal, fileTerminal) in matchedPairs)
            {
                logger.Log($"Updating terminal \"{dockerIrlTerminal.id}\".");
                UpdateTerminal(dockerIrlTerminal, fileTerminal);
            }

            logger.Log($"Adding {unmatchedSecondListItems.Count} terminals.");
            foreach (var fileTerminal in unmatchedSecondListItems)
            {
                logger.Log($"Adding terminal \"{fileTerminal.id}\".");
                terminalStore.InstantiateContainerTerminal(fileTerminal, dockerIrlContainer);
            }

            logger.Log($"Removed {unmatchedFirstListItems.Count} terminals, updated {matchedPairs.Count} terminals and added {unmatchedSecondListItems.Count} terminals.");
        }

        private void UpdateTerminal(Terminal.TerminalMonitor dockerIrlTerminal, SerializedTerminal fileTerminal)
        {
            dockerIrlTerminal.FromSerializeableObject(fileTerminal);
        }
    }
}
