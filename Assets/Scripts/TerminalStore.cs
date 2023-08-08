using DockerIrl.Serialization;
using System.Collections.Generic;
using UnityEngine;

namespace DockerIrl
{
    public class TerminalStore : MonoBehaviour
    {
        /// <summary>
        /// Only terminals that are not assigned to containers
        /// </summary>
        public List<Terminal.TerminalMonitor> freeTerminals = new();

        /// <summary>
        /// freeTerminals + terminals from all containers
        /// </summary>
        public List<Terminal.TerminalMonitor> allTerminals = new();

        public Transform freeTerminalsParent;
        public GameObject terminalPrefab;

        public Settings settings;

        /// <summary>
        /// Instantiates a terminal for free
        /// </summary>
        /// <param name="serializedTerminal"></param>
        /// <returns></returns>
        public Terminal.TerminalMonitor InstantiateFreeTerminal(SerializedTerminal serializedTerminal)
        {
            var terminal = InstantiateTerminal(serializedTerminal, freeTerminalsParent);
            freeTerminals.Add(terminal);

            return terminal;
        }

        public Terminal.TerminalMonitor InstantiateContainerTerminal(SerializedTerminal serializedTerminal, ContainerBehaviour container)
        {
            var terminal = InstantiateTerminal(serializedTerminal, container.terminalsRoot.transform);

            // Link
            terminal.container = container;
            container.terminals.Add(terminal);

            return terminal;
        }

        public void DestroyTerminal(Terminal.TerminalMonitor terminal)
        {
            if (terminal.terminalController.running)
            {
                if (!settings.removeRunningTerminals)
                {
                    throw new System.InvalidOperationException("Cannot remove a terminal with running process");
                }

                terminal.terminalController.EndTerminalSession();
            }

            if (terminal.container != null)
            {
                // Unlink
                terminal.container.terminals.Remove(terminal);
                terminal.container = null;
            }
            else
            {
                freeTerminals.Remove(terminal);
            }

            allTerminals.Remove(terminal);
        }

        private Terminal.TerminalMonitor InstantiateTerminal(SerializedTerminal serializedTerminal, Transform parent)
        {
            GameObject terminalGameObject = Instantiate(
                terminalPrefab,
                serializedTerminal.position,
                Quaternion.Euler(serializedTerminal.rotation),
                parent
            );
            terminalGameObject.name = $"Terminal {serializedTerminal.id}";

            Terminal.TerminalMonitor monitor = terminalGameObject.GetComponent<Terminal.TerminalMonitor>();

            // Initialize
            monitor.FromSerializeableObject(serializedTerminal);
            monitor.audioSource.volume = settings.x.sound.terminal;

            // Inject references
            monitor.FindAllReferences();

            allTerminals.Add(monitor);

            return monitor;
        }
    }
}
