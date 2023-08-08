using System.Collections.Generic;
using System.Text;

namespace DockerIrl.Terminal.VtNetCoreBridge
{
    /// <summary>
    /// Transforms terminal buffer into printable text using TextMeshPro Rich Text format.
    /// Reference: http://digitalnativestudios.com/textmeshpro/docs/rich-text/#color
    /// </summary>
    public class TerminalTextStringBuilder
    {
        private readonly StringBuilder foregroundSb = new();
        private readonly StringBuilder backgroundSb = new();

        public TerminalTextStringBuilder() { }

        /// <summary>
        /// This method can be called multiple times.
        /// </summary>
        public (string foregroundText, string backgroundText) Process(List<VtNetCore.VirtualTerminal.Layout.LayoutRow> rows)
        {
            foregroundSb.Clear();
            backgroundSb.Clear();

            foreach (var row in rows)
            {
                foreach (var span in row.Spans)
                {
                    if (span.Hidden) continue;

                    // span.ForgroundColor & span.BackgroundColor have #FFFFFF format

                    foregroundSb.Append($"<color={span.ForgroundColor}>");
                    foregroundSb.Append(span.Text);

                    backgroundSb.Append($"<mark={span.BackgroundColor}>");
                    backgroundSb.Append(span.Text);
                }

                foregroundSb.AppendLine();
                backgroundSb.AppendLine();
            }

            return (
                foregroundText: foregroundSb.ToString(),
                backgroundText: backgroundSb.ToString()
            );
        }
    }
}
