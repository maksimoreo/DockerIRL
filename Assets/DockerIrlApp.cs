using DockerIrl.ContainerManagement;
using DockerIrl.Player;
using DockerIrl.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace DockerIrl
{
    public static class LogTags
    {
        public static string General = "[General]";
        public static string Initialization = "[Initialization]";
        public static string Close = "[Close]";
        public static string DockerSync = "[DockerSync]";
        public static string ContainerStore = "[ContainerStore]";
        public static string Pty = "[Pty]";
        public static string Terminal = "[Terminal]";
        public static string UiUtils = "[UiUtils]";
        public static string DataLoading = "[DataLoading]";
    }

    public class DockerIrlApp : MonoBehaviour
    {
        public static DockerIrlApp instance { get; private set; }

        public Action OnAppInitialized;

        [Header("References")]
        public GeneralCharacterBehaviour player;
        public ContainerStore containerStore;
        public TerminalStore terminalStore;
        public FreeContainerSpotFinder freeContainerSpotFinder;
        public Docker.DotNet.DockerClient dockerClient;
        public Utils.ExtraInput extraInput;
        public Settings settings;
        public StateSerializer stateSerializer;
        public DockerSyncer dockerSyncer;
        public InputLoader inputLoader;

        // Services
        public TaskPerformer loadAndApplyDockerState;
        public TaskPerformer loadInitialState;
        public TaskPerformer loadAndApplyFileState;
        public TaskPerformer reload;
        public TaskPerformer quit;

        // Scheduled tasks
        private ScheduledTask scheduledSynchronizeDocker;
        private ScheduledTask scheduledSaveState;

        void Awake()
        {
            Debug.unityLogger.Log(LogTags.Initialization, "Initializing singleton");
            if (instance)
                throw new Exception("Duplicate singleton");
            else
                instance = this;

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing async exception handler");
            System.Threading.Tasks.TaskScheduler.UnobservedTaskException +=
                (object sender, System.Threading.Tasks.UnobservedTaskExceptionEventArgs eventArgs) =>
                {
                    Debug.LogError($"Received UnobservedTaskException event with {eventArgs.GetType()}");
                    Debug.LogException(eventArgs.Exception);
                };

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing System.Diagnostics.Trace handler");
            System.IO.StreamWriter writer = new("System.Diagnostics.Trace.log")
            {
                AutoFlush = true
            };
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(writer));
            System.Diagnostics.Trace.WriteLine("This file contains the output of System.Diagnostics.Trace.Write methods");

            Debug.unityLogger.Log(LogTags.Initialization, "Setting App framerate");
            Application.targetFrameRate = 60;
        }

        void Start()
        {
            Debug.unityLogger.Log(LogTags.Initialization, "Continuing initialization with async tasks");
            InitializeSecondStage();
        }

        private async void InitializeSecondStage()
        {
            Debug.unityLogger.Log(LogTags.Initialization, "Loading and applying settings");
            await settings.LoadFromFile();
            ApplyInitialSettings();
            settings.onSettingsChanged += HandleSettingsChanged;

            Debug.unityLogger.Log(LogTags.Initialization, "Loading keybinds");
            await inputLoader.LoadKeybinds();

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing Docker client");
            dockerClient = new Docker.DotNet.DockerClientConfiguration().CreateClient();

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing other services");
            loadInitialState = new TaskPerformer(() => new LoadInitialState(
                logger: Debug.unityLogger,
                terminalStore: terminalStore,
                containerStore: containerStore,
                dockerClient: dockerClient,
                player: player,
                settings: settings
            ).Call());
            loadAndApplyFileState = new TaskPerformer(() => new LoadAndApplyFileState()
            {
                logger = Debug.unityLogger,
                containerStore = containerStore,
                terminalStore = terminalStore,
                settings = settings,
            }.Call());
            loadAndApplyDockerState = new TaskPerformer(() => new LoadAndApplyDockerState(
                logger: Debug.unityLogger,
                containerStore: containerStore,
                dockerClient: dockerClient
            ).Call());
            reload = new TaskPerformer(() => Reload());
            quit = new TaskPerformer(() => GracefullyClose());

            Debug.unityLogger.Log(LogTags.Initialization, "Loading containers/terminals state");
            await loadInitialState.Call();

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing Docker event listener");
            dockerSyncer = new DockerSyncer(
                dockerClient: dockerClient,
                settings: settings,
                freeContainerSpotFinder: freeContainerSpotFinder,
                containerStore: containerStore
            );
            dockerSyncer.dockerEventsListener.Start();

            Debug.unityLogger.Log(LogTags.Initialization, "Initializing scheduled tasks");
            scheduledSynchronizeDocker = new ScheduledTask(this, settings.x.general.dockerStateQueryIntervalSeconds);
            scheduledSynchronizeDocker.OnTask += HandleScheduledTaskSynchronizeDocker;

            scheduledSaveState = new ScheduledTask(this, settings.x.general.autoSaveIntervalSeconds);
            scheduledSaveState.OnTask += HandleScheduledTaskSaveState;

            Debug.unityLogger.Log(LogTags.Initialization, "Switching to main thread, hold on...");
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                Debug.unityLogger.Log(LogTags.Initialization, "Continuing on main thread");

                Debug.unityLogger.Log(LogTags.Initialization, "Starting scheduled tasks");
                scheduledSaveState.Start();
                scheduledSynchronizeDocker.Start();

                Debug.unityLogger.Log(LogTags.Initialization, "Invoking other callbacks");
                OnAppInitialized?.Invoke();
            });

            Debug.unityLogger.Log(LogTags.Initialization, "Initialization complete. Happy hacking! :)");
        }

        // Even though this method returns a Task, it is not guaranteed that it will ever return
        public async Task GracefullyClose()
        {
            Debug.unityLogger.Log(LogTags.Close, "Begin");

            // TODO: Show message box with running terminals count, ask user if they really want to exit

            Debug.unityLogger.Log(LogTags.Close, "Switching to main thread, hold on...");
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                Debug.unityLogger.Log(LogTags.Close, "Continuing on main thread");

                if (settings.x.general.saveOnClose)
                {
                    Debug.unityLogger.Log(LogTags.Close, "Saving current state");
                    stateSerializer.SaveStateToFile();
                }

                Debug.unityLogger.Log(LogTags.Close, "Stopping scheduled tasks");
                scheduledSaveState.Stop();
                scheduledSynchronizeDocker.Stop();
            });

            Debug.unityLogger.Log(LogTags.Close, "Back to background thread");

            Debug.unityLogger.Log(LogTags.Close, "Stopping Docker event listener");
            var dockerSyncerStopTask = dockerSyncer.dockerEventsListener.Stop();
            if (dockerSyncerStopTask != null)
            {
                await dockerSyncerStopTask;
            }

            var runningTerminals = terminalStore.allTerminals.Where((terminal) => terminal.terminalController.running);

            Debug.unityLogger.Log(LogTags.Close, $"Stopping {runningTerminals.Count()} running terminal processes");
            if (runningTerminals.Any())
            {
                await CloseTerminals(runningTerminals);
            }

            // Stop other timers, hooks, etc

            Debug.unityLogger.Log(LogTags.Close, "Waiting some time before closing just in case");
            await Task.Delay(500).ContinueWith((task) =>
            {
                UnityMainThreadDispatcher.Instance().Enqueue(() =>
                {
                    ForceClose();
                });
            });
        }

        public void ForceClose()
        {
            Debug.unityLogger.Log(LogTags.Close, "bye :)");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public async Task Reload()
        {
            Debug.unityLogger.Log(LogTags.General, "Reloading...");

            await settings.LoadFromFile();
            await loadAndApplyFileState.Call();
            await inputLoader.LoadKeybinds();

            Debug.unityLogger.Log(LogTags.General, "Reloading done.");
        }

        public void OnGUI()
        {
            if (!instance || !settings.x.general.showDebugActions)
            {
                return;
            }

            if (GUILayout.Button("Quit"))
            {
                _ = GracefullyClose();
            }

            if (GUILayout.Button($"Loading config from \"{settings.loadConfigPath}\""))
            {
                _ = settings.LoadFromFile();
            }

            if (GUILayout.Button($"Save config to \"{settings.saveConfigPath}\""))
            {
                settings.SaveToFile();
            }

            if (GUILayout.Button($"Apply File state (\"{settings.loadStateFilePath}\")"))
            {
                _ = loadAndApplyFileState.Call();
            }

            if (GUILayout.Button($"Save current state to \"{settings.saveStateFilePath}\""))
            {
                stateSerializer.SaveStateToFile();
            }

            if (GUILayout.Button("Apply Docker state"))
            {
                _ = loadAndApplyDockerState.Call();
            }

            if (GUILayout.Button("Close running terminals"))
            {
                Debug.unityLogger.Log(LogTags.UiUtils, "Close running terminals");

                _ = CloseTerminals(terminalStore.allTerminals.Where((terminal) => terminal.terminalController.running));
            }

            if (GUILayout.Button("Save current keybinds to json"))
            {
                _ = inputLoader.SaveKeybinds();
            }
        }

        public void ApplyInitialSettings()
        {
            player.firstPersonController.MoveSpeed = settings.x.general.characterMoveSpeed;
            player.firstPersonController.SprintSpeed = settings.x.general.characterSprintSpeed;
            player.highlightBehavior.interactionRange = settings.x.general.interactRange;
            player.cinemachineBreathing = settings.x.general.breathing;
            player.footstepsController.audioSource.volume = settings.x.sound.steps;
        }

        /// <summary>
        /// Call this after changing Settings
        /// </summary>
        private void HandleSettingsChanged()
        {
            scheduledSaveState.intervalSeconds = settings.x.general.autoSaveIntervalSeconds;
            scheduledSaveState.Restart();

            scheduledSynchronizeDocker.intervalSeconds = settings.x.general.dockerStateQueryIntervalSeconds;
            scheduledSynchronizeDocker.Restart();

            player.firstPersonController.MoveSpeed = settings.x.general.characterMoveSpeed;
            player.firstPersonController.SprintSpeed = settings.x.general.characterSprintSpeed;
            player.highlightBehavior.interactionRange = settings.x.general.interactRange;
            player.cinemachineBreathing = settings.x.general.breathing;
            player.footstepsController.audioSource.volume = settings.x.sound.steps;

            terminalStore.allTerminals.ForEach((terminal) =>
            {
                terminal.audioSource.volume = settings.x.sound.terminal;
            });
        }

        private void HandleScheduledTaskSynchronizeDocker()
        {
            _ = loadAndApplyDockerState.Call();
        }

        private void HandleScheduledTaskSaveState()
        {
            stateSerializer.SaveStateToFile();
        }

        private Task CloseTerminals(IEnumerable<Terminal.TerminalMonitor> terminals)
        {
            Debug.unityLogger.Log(LogTags.UiUtils, $"Closing {terminals.Count()} terminals");

            return Task.WhenAll(terminals.Select(terminal => terminal.terminalController.EndTerminalSessionAsync()));
        }
    }
}
