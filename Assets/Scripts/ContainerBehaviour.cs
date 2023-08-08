using Docker.DotNet.Models;
using DockerIrl.Serialization;
using DockerIrl.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DockerIrl
{
    [Serializable]
    public class DockerContainerData
    {
        public enum Status
        {
            created,
            running,
            paused,
            restarting,
            removing,
            exited,
            dead,
        }

        public string id;
        public string name;

        // aka "State" from "List containers" from Docker Engine API,
        // aka "State.Status" from "Inspect a container" from Docker Engine API.
        // aka "State" from Docker.DotNet
        public Status state;

        // aka "Status" from "List containers" from Docker Engine API,
        // aka "STATUS" column from `docker containers ls --all`.
        // aka "Status" from Docker.DotNet
        public string status;

        public string imageId;
        public string imageName;

        public void Update(ContainerListResponse dockerContainer)
        {
            id = dockerContainer.ID;
            name = dockerContainer.Names[0];
            state = Enum.Parse<Status>(dockerContainer.State);
            status = dockerContainer.Status;
            imageId = dockerContainer.ImageID;
            imageName = dockerContainer.Image;
        }
    }

    public class ContainerBehaviour : MonoBehaviour, IHasId
    {
        // DockerIRL data
        public string id { get; set; }
        public bool persist;
        public string prefabId;
        public string highlightTextTemplate;

        /// <summary>
        /// Holds Docker container ID or Names[0]
        /// </summary>
        public string matchId;

        public DockerContainerData docker;
        public event Action OnDockerDataChanged;
        public string dockerDataChangedAt;

        [Header("Internal components")]
        public HighlightableBehaviour highlightableBehaviour;
        public GameObject terminalsRoot;
        public List<Terminal.TerminalMonitor> terminals;

        public string containerIdentity
        {
            get => $"Container \"{id}\": {name}{(persist ? " (persist)" : "")}";
        }

        public bool MatchDocker(ContainerListResponse dockerContainer) => MatchDocker(dockerContainer.ID, dockerContainer.Names[0]);
        public bool MatchDocker(string id, string name) => matchId == id || matchId == name;

        public SerializedContainer ToSerializableObject()
        {
            return new SerializedContainer()
            {
                id = id,
                position = transform.position,
                rotation = transform.eulerAngles,
                modelId = prefabId,
                persist = persist,
                matchId = matchId,
                highlightTextTemplate = highlightTextTemplate,
                terminals = terminals.Select((terminal) => terminal.ToSerializeableObject()).ToList(),
            };
        }

        public void RenderHighlightText()
        {
            if (!highlightableBehaviour.isSelected) return;
            if (string.IsNullOrEmpty(highlightTextTemplate)) return;

            var text = X.FormatStringTemplate(highlightTextTemplate, new()
            {
                { "containerId", id },
                { "dockerId", docker.id },
                { "dockerName", docker.name },
                { "dockerStatus", docker.status },
                { "dockerImageId", docker.imageId },
                { "dockerImageName", docker.imageName },
                { "actionBinding_moveObject", DockerIrlApp.instance.inputLoader.moveObjectActionBindingDisplayString },
            });

            highlightableBehaviour.highlightMenuHandle.ShowText(text);
        }
    }
}