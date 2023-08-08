using Docker.DotNet;
using Docker.DotNet.Models;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    /// <summary>
    /// Listens for events from Docker, allows to subscribe to events via OnEvent
    /// </summary>
    public class DockerEventsListener
    {
        public event Action<Message> OnEvent;

        private readonly DockerClient dockerClient;
        private CancellationTokenSource cancellationTokenSource;
        private Task stopTask;
        private TaskCompletionSource<object> stopTaskTcs;

        public bool running { get => cancellationTokenSource != null; }

        public DockerEventsListener(DockerClient dockerClient)
        {
            this.dockerClient = dockerClient;
        }

        public void Start()
        {
            if (running) return;

            Debug.unityLogger.Log(LogTags.DockerSync, "Requesting events listener to start.");

            cancellationTokenSource = new CancellationTokenSource();
            Task.Factory.StartNew(EventsMonitorThread, TaskCreationOptions.LongRunning);
        }

        public Task Stop()
        {
            if (!running) return null;
            if (stopTaskTcs != null) return stopTaskTcs.Task;

            Debug.unityLogger.Log(LogTags.DockerSync, "Requesting events listener to stop.");

            stopTaskTcs = new TaskCompletionSource<object>();

            cancellationTokenSource.Cancel();

            return stopTaskTcs.Task;
        }

        private async Task EventsMonitorThread()
        {
            Debug.unityLogger.Log(LogTags.DockerSync, "EventsMonitorThread() starts.");

            try
            {
                await dockerClient.System.MonitorEventsAsync(
                    new ContainerEventsParameters(),
                    new Progress<Message>(HandleDockerEvent),
                    cancellationTokenSource.Token
                );
            }
            catch (TimeoutException ex)
            {
                Debug.unityLogger.LogWarning(LogTags.DockerSync, $"Received exception: \"{ex}\". Probably could not connect to Docker.");
            }
            catch (TaskCanceledException ex)
            {
                Debug.unityLogger.Log(LogTags.DockerSync, $"Recieved expected exception \"{ex}\".");
            }

            // FIXME: I always recieve "IOException: Unexpected end of stream" right after cancelling a
            // cancellationToken, but cant catch it anywhere (except in
            // System.Threading.Tasks.TaskScheduler.UnobservedTaskException, when it is too late), I receive it as
            // "UnobservedTaskException" in global tasks exception hander, so it is an expected exception?
            // This looks related: https://github.com/dotnet/Docker.DotNet/issues/516

            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;

            if (stopTaskTcs != null)
            {
                stopTaskTcs.SetResult(null);
            }

            Debug.unityLogger.Log(LogTags.DockerSync, "EventsMonitorThread() exits.");
        }

        private void HandleDockerEvent(Message message)
        {
            OnEvent?.Invoke(message);
        }
    }
}
