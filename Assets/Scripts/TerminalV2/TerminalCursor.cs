using UnityEngine;

namespace DockerIrl.Terminal
{
    public class TerminalCursor : MonoBehaviour
    {
        [Header("General")]
        [Tooltip("Size of cursor character. Probably should be equals to text character size. Used to position cursor.")]
        public Vector2 characterSize;
        public bool overrideShape;
        public ITerminalEmulator.CursorShape overrideShapeShape = ITerminalEmulator.CursorShape.Bar;

        [Header("Shapes")]
        public Rect barShapeRect;
        public Rect blockShapeRect;
        public Rect underlineShapeRect;

        [Header("Internal components")]
        public RectTransform cursorRectTransform;

        public TerminalController terminalController;

        private void Update()
        {
            if (terminalController.terminalEmulator == null)
                return;

            var shape = overrideShape ? overrideShapeShape : terminalController.terminalEmulator.cursorShape;
            var shapeRect =
                shape == ITerminalEmulator.CursorShape.Bar ? barShapeRect
                : shape == ITerminalEmulator.CursorShape.Block ? blockShapeRect
                : underlineShapeRect;

            Vector2Int textPosition = terminalController.terminalEmulator.cursorPosition;
            Vector2 absolutePosition = textPosition * characterSize;
            Vector2 shapePosition = absolutePosition + shapeRect.position;

            cursorRectTransform.anchoredPosition = shapePosition;
            cursorRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, shapeRect.width);
            cursorRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, shapeRect.height);
        }
    }
}
