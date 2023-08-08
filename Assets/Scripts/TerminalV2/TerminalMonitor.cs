using DockerIrl.Player;
using DockerIrl.Serialization;
using DockerIrl.Utils;
using UnityEngine;

namespace DockerIrl.Terminal
{
    public class TerminalMonitor : MonoBehaviour, Utils.IHasId
    {
        public string id { get; set; }

        [Header("General")]
        [Tooltip("If true, will not create any process, only focus. Usefull when testing.")]
        public bool skipProcessStart = false;

        [Tooltip("If true, unfocus will stop the process, and next focus will create a new process. If false, unfocus will only unfocus input, and next focus will just re-attach input.")]
        public bool closeOnUnfocus = false;

        [Tooltip("TODO: Unfocus when process closes.")]
        public bool unfocusOnClose = false;

        [Tooltip("If true, will move camera closer to the terminal, for convenience.")]
        public bool useCamera = true;

        [Tooltip("This template will be rendered when user looks at this monitor")]
        public string highlightTextTemplate;

        [Tooltip("This template will be rendered when just after terminal is instantiated")]
        public string defaultIdleTextTemplate;

        [Header("Global components")]
        [Tooltip("If true, will automatically query dependencies from DockerIrlApp.instance at Awake()")]
        public bool autoInitializeRefsOnStart = false;

        public GeneralCharacterBehaviour generalCharacterBehaviour;
        public InputLoader inputLoader;
        public PlayerHighlightBehavior highlightBehavior;
        public PauseBehaviour pauseBehavior;
        public ObjectMovingBehaviour objectMovingBehavior;
        public StarterAssets.FirstPersonController firstPersonController;
        public UnityEngine.InputSystem.InputAction closeTerminalAction;

        [Header("Links")]
        [Tooltip("Container behaviour this terminal is attached to, nullable")]
        public ContainerBehaviour container;

        [Header("Internal components")]
        public Transform monitorModel;
        public Transform ledsRootTransform;
        public Canvas canvas;
        public HighlightableBehaviour highlightableBehaviour;
        public AudioSource audioSource;

        // TODO: This code binds to specific implementation (TerminalTextSingleScreen), but should use an interface
        // (ITerminalTextRenderer). However it is impossible to assign anything to an interface field from Unity
        // Inspector. It is possible to use by using a base class (BaseTerminalTextRenderer) or a bridge base class.
        // Currently there is only one implementation (non-scrollable) so this all mess is just unnecessary, but in
        // the future, when adding a scrollable text renderer, consider using one of the suggested porymorphic
        // approaches here.
        public TerminalTextRenderer terminalTextRenderer;
        public new Cinemachine.CinemachineVirtualCamera camera;
        public TerminalController terminalController;
        public Collider myCollider;
        public TerminalCursor terminalCursor;

        private RectTransform canvasRectTransform;
        private bool focused;

        public void Awake()
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
            terminalController.OnCommandExit.AddListener(HandleTerminalCanvasCommandExit);
        }

        public void Start()
        {
            if (autoInitializeRefsOnStart)
            {
                FindAllReferences();
            }

            terminalTextRenderer.SetText(
                foregroundTmproText: RenderTextTemplate(defaultIdleTextTemplate),
                backgroundTmproText: ""
            );
        }

        public void FindAllReferences()
        {
            generalCharacterBehaviour = DockerIrlApp.instance.player;
            inputLoader = DockerIrlApp.instance.inputLoader;
            terminalController.terminalInput.extraInput = DockerIrlApp.instance.extraInput;
            highlightBehavior = DockerIrlApp.instance.player.highlightBehavior;
            pauseBehavior = DockerIrlApp.instance.player.pauseBehaviour;
            objectMovingBehavior = DockerIrlApp.instance.player.objectMovingBehaviour;
            firstPersonController = DockerIrlApp.instance.player.firstPersonController;
            closeTerminalAction = DockerIrlApp.instance.player.input.actions.FindAction("closeTerminal", true);
        }

        public void OnInteract()
        {
            if (focused)
                OnUnfocus();
            else
                OnFocus();
        }

        public void OnHighlight(HighlightMenuHandle highlightMenuHandle)
        {
            RenderHighlightText();
        }

        private void RenderHighlightText()
        {
            if (!highlightableBehaviour.isSelected) return;
            if (string.IsNullOrEmpty(highlightTextTemplate)) return;

            highlightableBehaviour.highlightMenuHandle.ShowText(RenderTextTemplate(highlightTextTemplate));
        }

        private string RenderTextTemplate(string template)
        {
            return X.FormatStringTemplate(template, new()
            {
                { "id", id },
                { "command", terminalController.ptyOptions.command },
                { "commandWithArgs", terminalController.ptyOptions.command + " " + string.Join(" ", terminalController.ptyOptions.args) },
                { "isRunningText", terminalController.running ? "Running" : "Idle" },
                { "lastExitCode", terminalController.running ? "" : terminalController.lastExitCode.ToString() },
                { "currentPid", terminalController.running ? terminalController.currentPid.ToString() : "" },
                { "actionBinding_interact", inputLoader.interactActionBindingDisplayString },
            });
        }

        public void FromSerializeableObject(SerializedTerminal serializedTerminal)
        {
            id = serializedTerminal.id;

            transform.SetPositionAndRotation(serializedTerminal.position, Quaternion.Euler(serializedTerminal.rotation));

            terminalController.ptyOptions = serializedTerminal.console.ToPtyOptions();
            if (terminalController.running)
            {
                terminalController.Resize(serializedTerminal.console.columns, serializedTerminal.console.rows);
            }

            skipProcessStart = serializedTerminal.skipProcessStart;

            SetMonitorSize(serializedTerminal.monitorSize);

            if (serializedTerminal.camera == null)
            {
                useCamera = false;
            }
            else
            {
                useCamera = true;
                camera.m_Lens.FieldOfView = serializedTerminal.camera.fov;
                camera.transform.localPosition = new Vector3(0, 0, -serializedTerminal.camera.distance);
            }

            canvas.transform.localScale = new Vector3(serializedTerminal.canvasScale.x, serializedTerminal.canvasScale.y, 1);

            canvasRectTransform.sizeDelta = serializedTerminal.canvasSize;

            terminalTextRenderer.fontSize = serializedTerminal.fontSize;

            terminalCursor.barShapeRect = serializedTerminal.cursorBarShapeRect;
            terminalCursor.blockShapeRect = serializedTerminal.cursorBlockShapeRect;
            terminalCursor.underlineShapeRect = serializedTerminal.cursorUnderlineShapeRect;
            terminalCursor.characterSize = serializedTerminal.cursorCharacterSize;

            highlightTextTemplate = serializedTerminal.highlightTextTemplate;
            defaultIdleTextTemplate = serializedTerminal.defaultIdleTextTemplate;
        }

        public SerializedTerminal ToSerializeableObject()
        {
            return new SerializedTerminal()
            {
                id = id,
                position = transform.localPosition,
                rotation = transform.localEulerAngles,
                console = terminalController.ptyOptions.ToSerializedTerminalConsole(),
                skipProcessStart = skipProcessStart,
                camera = useCamera ? new SerializedTerminal.SerializedTerminalCamera()
                {
                    fov = camera.m_Lens.FieldOfView,
                    distance = -camera.transform.localPosition.z,
                } : null,
                monitorSize = new Vector2(monitorModel.localScale.x, monitorModel.localScale.y),
                canvasSize = new Vector2(canvasRectTransform.rect.width, canvasRectTransform.rect.height),
                canvasScale = canvas.transform.localScale,
                fontSize = terminalTextRenderer.fontSize,
                cursorBarShapeRect = terminalCursor.barShapeRect,
                cursorBlockShapeRect = terminalCursor.blockShapeRect,
                cursorUnderlineShapeRect = terminalCursor.underlineShapeRect,
                cursorCharacterSize = terminalCursor.characterSize,
                highlightTextTemplate = highlightTextTemplate,
                defaultIdleTextTemplate = defaultIdleTextTemplate,
            };
        }

        public void SetMonitorSize(Vector2 size) => SetMonitorSize(size.x, size.y);
        public void SetMonitorSize(float width, float height)
        {
            // TODO: Create appropriately sized model and replace 0.1f with 1
            monitorModel.localScale = new Vector3(width, height, 0.1f);
            ledsRootTransform.localPosition = new Vector3(width / 2, -height / 2, 0);
        }

        private void OnFocus()
        {
            if (generalCharacterBehaviour.focusedTerminalMonitor != null)
            {
                throw new System.Exception("Cannot focus new terminal as character is already focused into other terminal");
            }

            if (!terminalController.running && !skipProcessStart)
            {
                terminalController.StartTerminalSession();
            }

            terminalController.FocusInput();
            closeTerminalAction.performed += HandleCloseTerminal;

            if (useCamera)
            {
                camera.Priority = 100;
            }

            generalCharacterBehaviour.focusedTerminalMonitor = this;
            highlightBehavior.enabled = false;
            firstPersonController.enabled = false;
            pauseBehavior.enabled = false;
            objectMovingBehavior.enabled = false;

            focused = true;
        }

        private void OnUnfocus()
        {
            terminalController.UnfocusInput();

            if (terminalController.running && closeOnUnfocus)
            {
                Debug.Log("terminalCanvas.EndTerminalSession()");
                terminalController.EndTerminalSession();
            }

            closeTerminalAction.performed -= HandleCloseTerminal;

            if (useCamera)
            {
                camera.Priority = 1;
            }

            generalCharacterBehaviour.focusedTerminalMonitor = null;
            highlightBehavior.enabled = true;
            firstPersonController.enabled = true;
            pauseBehavior.enabled = true;
            objectMovingBehavior.enabled = true;

            focused = false;
        }

        private void HandleCloseTerminal(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            Debug.Log("HandleCloseTerminal()");
            if (focused) OnUnfocus();
        }

        private void HandleTerminalCanvasCommandExit()
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                if (focused && unfocusOnClose) OnUnfocus();
            });
        }
    }
}
