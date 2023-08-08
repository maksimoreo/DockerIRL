using System;
using System.Collections;
using UnityEngine;

namespace DockerIrl.Utilities
{
    /// <summary>
    /// Encapsulates a task that repeats indefinitely every X seconds. Can be disabled or enabled.
    /// Why can't I use Unity's Coroutines instead? This class gives following benefits:
    /// + combines `coroutine` & `intervalSeconds` into a single object;
    /// + easier to work with by using Start(), Stop(), Restart();
    /// + `enabled` field is bool, use it as `if (task.enabled) ...` instead of `if (coroutine != null) ...`
    /// U have to remember tho, that instance of ScheduledTask is forever binded to specified scheduler MonoBehaviour.
    /// Do not pass ScheduledTask instance to other object, it is better to create it at some global Scheduler
    /// gameObject.
    /// </summary>
    public class ScheduledTask
    {
        public event Action OnTask;
        public float intervalSeconds;

        public bool enabled { get => coroutine != null; }

        private readonly MonoBehaviour scheduler;
        private Coroutine coroutine;

        public ScheduledTask(MonoBehaviour scheduler, float intervalSeconds)
        {
            this.scheduler = scheduler;
            this.intervalSeconds = intervalSeconds;
        }

        /// <summary>
        /// Starts task if it is not enabled. Does nothing if task is enabled.
        /// </summary>
        public void Start()
        {
            if (coroutine != null) return;
            if (intervalSeconds < 0.01) return;

            coroutine = scheduler.StartCoroutine(TaskCoroutine());
        }

        /// <summary>
        /// Stops task if it is enabled. Does nothing if task is not enabled.
        /// </summary>
        public void Stop()
        {
            if (coroutine == null) return;

            scheduler.StopCoroutine(coroutine);
            coroutine = null;
        }

        /// <summary>
        /// Stops task if it is enabled and starts it again.
        /// </summary>
        public void Restart()
        {
            if (coroutine != null) scheduler.StopCoroutine(coroutine);
            if (intervalSeconds < 0.01) return;

            coroutine = scheduler.StartCoroutine(TaskCoroutine());
        }

        private IEnumerator TaskCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(intervalSeconds);

                Debug.Log($"ScheduledTask: {intervalSeconds}");
                OnTask?.Invoke();
            }
        }
    }

    /// <summary>
    /// Same as ScheduledTask, but encapsulated into MonoBehaviour, and usable from the Unity Editor.
    /// </summary>
    public class ScheduledTaskBehaviour : MonoBehaviour
    {
        // TODO: Add a custom fancy Editor GUI with Start/Stop buttons to control task in play mode.
        // TODO: This MonoBehaviour currently holds a single ScheduledTask. Make it hold multiple tasks. Probably requires to implement custom Editor GUI.

        public UnityEngine.Events.UnityEvent OnTask;

        public float initialIntervalSeconds = 5;
        public ScheduledTask scheduledTask;

        private void Awake()
        {
            scheduledTask = new ScheduledTask(scheduler: this, intervalSeconds: initialIntervalSeconds);
            scheduledTask.OnTask += HandleScheduledTask;
        }

        private void OnEnable()
        {
            scheduledTask.Start();
        }

        private void OnDisable()
        {
            scheduledTask.Stop();
        }

        private void HandleScheduledTask()
        {
            OnTask?.Invoke();
        }
    }
}
