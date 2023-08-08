using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DockerIrl.Player
{
    public class InputLoader : MonoBehaviour
    {
        public PlayerInput playerInput;
        public Settings settings;

        // Cached keybind description strings
        // TODO: maybe it is cleaner to move these to an object (like displayStrings.interact), but rn its just more complicated to ensure encapsulation
        public string moveObjectActionBindingDisplayString { get; private set; }
        public string placeObjectActionBindingDisplayString { get; private set; }
        public string cancelMoveObjectActionBindingDisplayString { get; private set; }
        public string interactActionBindingDisplayString { get; private set; }

        private void Start()
        {
            moveObjectActionBindingDisplayString = GetInputActionDisplayString("MoveObject");
            placeObjectActionBindingDisplayString = GetInputActionDisplayString("PlaceObject");
            cancelMoveObjectActionBindingDisplayString = GetInputActionDisplayString("CancelMoveObject");
            interactActionBindingDisplayString = GetInputActionDisplayString("Interact");
        }

        public Dictionary<string, string> GenerateTemplateDataForInput() => new()
        {
            { "actionBinding_moveObject", moveObjectActionBindingDisplayString },
            { "actionBinding_placeObject", placeObjectActionBindingDisplayString },
            { "actionBinding_cancelMove", cancelMoveObjectActionBindingDisplayString },
            { "actionBinding_interact", interactActionBindingDisplayString },
        };

        public async Task LoadKeybinds()
        {
            string filePath = settings.x.general.loadKeybindsFilePath;

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.unityLogger.LogWarning(LogTags.General, "Setting \"general/loadKeybindsFilePath\" is empty, will not load keybinds from file.");
                return;
            }

            Debug.unityLogger.Log(LogTags.General, $"Reading keybinds from \"{filePath}\".");
            string json;
            try
            {
                json = await File.ReadAllTextAsync(settings.x.general.loadKeybindsFilePath);
            }
            catch (Exception ex) when (ex is FileNotFoundException || ex is DirectoryNotFoundException)
            {
                Debug.unityLogger.LogWarning(LogTags.General, $"Failed to read keybinds from \"{filePath}\".");
                return;
            }

            await UnityMainThreadDispatcher.Instance().EnqueueAsync(() =>
            {
                Debug.unityLogger.Log(LogTags.General, $"Applying keybinds.");
                playerInput.actions.LoadBindingOverridesFromJson(json);
            });
        }

        /// <summary>
        /// Saves InputSystem keybinds to a file. This method is mostly useful for debugging.
        /// </summary>
        /// <returns></returns>
        public async Task SaveKeybinds()
        {
            string json = playerInput.actions.ToJson();

            await File.WriteAllTextAsync(settings.x.general.loadKeybindsFilePath, json);
        }

        public string GetInputActionDisplayString(string actionName)
        {
            return GetInputActionDisplayString(playerInput.actions.FindAction(actionName, true));
        }

        /// <summary>
        /// HEAVY METHOD, do not call in Update(), instead call once & cache the result
        /// Example output: "[Esc], [Return] or [Control+Return]"
        /// </summary>
        /// <param name="action"></param>
        /// <returns>Binded controls text ready to be displayed directly to the User</returns>
        public string GetInputActionDisplayString(InputAction action)
        {
            List<int> bindingsIndexes = new();

            // First add simple bindings
            // Simple bindings like A, B, Space, Esc, Return, ...
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (!binding.isComposite && !binding.isPartOfComposite)
                    bindingsIndexes.Add(i);
            }

            // Next add composite bindings
            // Composite bindings like Ctrl+A, Shift+A, Ctrl+Shift+A, ...
            for (int i = 0; i < action.bindings.Count; i++)
            {
                var binding = action.bindings[i];

                if (binding.isComposite)
                    bindingsIndexes.Add(i);
            }

            var bindingsStrings = bindingsIndexes.Select((bindingIndex) => $"[{action.GetBindingDisplayString(bindingIndex)}]");
            var bindingString = string.Join(", ", bindingsStrings);

            return bindingString;
        }

        public string GetInputSystemDebugInfo()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("DockerIrl.Player.Input.WriteDebugToLog():");
            sb.AppendLine("  playerInput.actions.ToJson():");
            sb.AppendLine(playerInput.actions.ToJson());

            foreach (var action in playerInput.currentActionMap.actions)
            {
                sb.AppendLine($"  Action: {action.name}");

                sb.AppendLine($"    GetBindingDisplayString: {action.GetBindingDisplayString()}");
                sb.AppendLine($"    SmartBindingDisplayString: {GetInputActionDisplayString(action)}");

                sb.AppendLine($"    action.GetBindingDisplayString(i):");
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    sb.AppendLine($"      action.GetBindingDisplayString({i}): {action.GetBindingDisplayString(i)}");
                }

                sb.AppendLine($"    binds:");
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];
                    sb.AppendLine($"      {i}: ToDisplayString: {binding.ToDisplayString()}, name: {binding.name}");
                    sb.AppendLine($"      action.GetBindingDisplayString({i}): {action.GetBindingDisplayString(i)}");

                    // Note that `action.bindings[i].ToDisplayString() != action.GetBindingDisplayString(i)`
                }

                sb.AppendLine($"    controls:");
                for (int i = 0; i < action.controls.Count; i++)
                {
                    var control = action.controls[i];
                    sb.AppendLine($"      {i}: ToDisplayString: {control.ToString()}, name: {control.name}");
                }
            }

            return sb.ToString();
        }
    }
}
