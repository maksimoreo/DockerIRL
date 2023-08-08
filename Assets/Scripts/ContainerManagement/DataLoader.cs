using Docker.DotNet;
using Docker.DotNet.Models;
using DockerIrl.Serialization;
using DockerIrl.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    public static class DataLoader
    {
        public static async Task<IList<ContainerListResponse>> FetchDockerState(ILogger logger, DockerClient dockerClient)
        {
            logger.Log("Fetching state from Docker.");

            try
            {
                var containers = await dockerClient.Containers.ListContainersAsync(new ContainersListParameters()
                {
                    All = true,
                });

                logger.Log($"Successfully fetched {containers.Count} containers from Docker.");

                return containers;
            }
            catch (TimeoutException)
            {
                logger.LogWarning("DockerSyncer", "Failed to fetch state from Docker.");
                return null;
            }
        }

        public static async Task<SerializedFileState> FetchAndValidateFileState(ILogger logger, Settings settings)
        {
            logger.Log("Fetching file state.");

            var rawFileState = await FetchFileState(logger, settings);

            if (rawFileState == null) return null;

            logger.Log("Validating basic file structure.");

            // Validate duplicate container IDs
            var duplicateContainerIds = FindDuplicateIds(rawFileState.containers);
            if (duplicateContainerIds.Count() > 0)
            {
                logger.LogWarning("FetchAndValidateFileState", $"Duplicate container IDs: {X.ToStringList(duplicateContainerIds)}.");
                return null;
            }

            // Validate duplicate terminal IDs
            var allTerminals = rawFileState
                .containers
                .SelectMany((container) => container.terminals)
                .Concat(rawFileState.terminals);

            var duplicateTerminalsIds = FindDuplicateIds(allTerminals);
            if (duplicateTerminalsIds.Count() > 0)
            {
                logger.LogWarning("FetchAndValidateFileState", $"Duplicate terminal IDs: {X.ToStringList(duplicateTerminalsIds)}.");
                return null;
            }

            logger.Log("Passed basic validation.");

            return rawFileState;
        }

        private static IEnumerable<string> FindDuplicateIds(IEnumerable<IHasId> items)
        {
            return items.GroupBy((item) => item.id)
                .Where((group) => group.Count() > 1)
                .Select((group) => group.Key);
        }

        public static async Task<SerializedFileState> FetchFileState(ILogger logger, Settings settings)
        {
            var filePath = settings.loadStateFilePath;

            try
            {
                string jsonText = await File.ReadAllTextAsync(filePath);
                var fileState = JsonConvert.DeserializeObject<SerializedFileState>(jsonText);

                return fileState;
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                logger.LogWarning("DockerSyncer", $"Failed to fetch file: \"{filePath}\"");
                return null;
            }
        }
    }
}
