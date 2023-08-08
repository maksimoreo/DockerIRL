using Docker.DotNet;
using Docker.DotNet.Models;
using DockerIrl.ContainerManagement;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine;

namespace DockerIrl
{
    public class DockerSyncer
    {
        public readonly FreeContainerSpotFinder freeContainerSpotFinder;
        public readonly Settings settings;
        public readonly DockerEventsListener dockerEventsListener;
        public readonly ContainerStore containerStore;

        public DockerSyncer(DockerClient dockerClient, ContainerStore containerStore, FreeContainerSpotFinder freeContainerSpotFinder, Settings settings)
        {
            this.containerStore = containerStore;
            this.freeContainerSpotFinder = freeContainerSpotFinder;
            this.settings = settings;

            dockerEventsListener = new DockerEventsListener(dockerClient);
            dockerEventsListener.OnEvent += HandleDockerEvent;
        }

        private void HandleDockerEvent(Message message)
        {
            Debug.unityLogger.Log(LogTags.DockerSync, $"Received message from Docker: \"{JsonConvert.SerializeObject(message)}\", sending to main thread queue.");

            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                Debug.unityLogger.Log(LogTags.DockerSync, $"Processing event from Docker: \"{JsonConvert.SerializeObject(message)}\".");

                // List of event types/actions: https://docs.docker.com/engine/reference/commandline/events/

                switch (message.Type)
                {
                    case "container":
                        switch (message.Action)
                        {
                            case "create":
                                Debug.unityLogger.Log(LogTags.DockerSync, "Processing as \"container/create\" event.");
                                ProcessEventContainerCreate(message);
                                return;

                            case "destroy":
                                Debug.unityLogger.Log(LogTags.DockerSync, "Processing as \"container/destroy\" event.");
                                ProcessEventContainerDestroy(message);
                                return;

                            case "start":
                                Debug.unityLogger.Log(LogTags.DockerSync, "Processing as \"container/start\" event.");
                                ProcessEventContainerStart(message);
                                return;

                            case "stop":
                                Debug.unityLogger.Log(LogTags.DockerSync, "Processing as \"container/stop\" event.");
                                ProcessEventContainerStop(message);
                                return;
                        }
                        break;
                }

                Debug.unityLogger.Log(LogTags.DockerSync, "Skipping this event.");
            });
        }

        private void ProcessEventContainerCreate(Message message)
        {
            string dockerContainerName = message.Actor.Attributes["name"];

            var dockerIrlContainer = containerStore.InstantiateNewContainer(
                dockerId: message.Actor.ID,
                dockerName: dockerContainerName
            );

            dockerIrlContainer.docker.name = dockerContainerName;
        }

        private void ProcessEventContainerDestroy(Message message)
        {
            ContainerBehaviour containerBehaviour = containerStore.store.FirstOrDefault(
                (container) => container.MatchDocker(message.Actor.ID, message.Actor.Attributes["name"])
            );

            if (containerBehaviour == null)
            {
                Debug.unityLogger.LogWarning(LogTags.DockerSync, "Could not find matching container for this event.");
                return;
            }

            if (containerBehaviour.persist)
            {
                Debug.unityLogger.Log(LogTags.DockerSync, "Will not destroy container as it is marked with \"persist\".");
                return;
            }

            containerStore.RemoveContainer(containerBehaviour);
        }

        private void ProcessEventContainerStart(Message message)
        {
            Debug.unityLogger.LogWarning(LogTags.DockerSync, "This event handler is not implemented, will not do anything.");
        }

        private void ProcessEventContainerStop(Message message)
        {
            Debug.unityLogger.LogWarning(LogTags.DockerSync, "This event handler is not implemented, will not do anything.");
        }
    }
}
