using Docker.DotNet;
using Docker.DotNet.Models;
using System.Collections.Generic;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Tiny utility class to test connection to the Docker Engine
    /// </summary>
    public static class TestDocker
    {
        public static async void Call()
        {
            try
            {
                Debug.Log("Connecting to Docker...");
                DockerClient dockerClient = new DockerClientConfiguration().CreateClient();

                Debug.Log("Querying active containers from Docker...");
                IList<ContainerListResponse> containers = await dockerClient.Containers.ListContainersAsync(
                    new ContainersListParameters()
                    {
                        Limit = 20,
                    });
                Debug.Log($"Active containers count: {containers.Count}");

                Debug.Log("Disconnecting from Docker...");
                dockerClient.Dispose();
                Debug.Log("Done");
            }
            catch (System.TimeoutException)
            {
                Debug.LogWarning("Could not connect to Docker");
            }
        }
    }
}
