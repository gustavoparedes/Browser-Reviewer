using System.Drawing;
using System.Net;

namespace Browser_Reviewer
{
    public enum BrowserReviewerLogLevel
    {
        Info,
        Success,
        Warning,
        Error,
        Debug
    }

    public static class BrowserReviewerLogger
    {
        private static readonly object SyncRoot = new();
        private static StreamWriter? eventWriter;
        private static StreamWriter? pathWriter;
        private static StreamWriter? parserHtmlWriter;
        private static string? configuredDatabasePath;

        public static void ConfigureForDatabase(string? databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                return;
            }

            lock (SyncRoot)
            {
                if (string.Equals(configuredDatabasePath, databasePath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                CloseWriters();
                configuredDatabasePath = databasePath;

                string directory = Path.GetDirectoryName(databasePath) ?? Environment.CurrentDirectory;
                string baseName = Path.GetFileNameWithoutExtension(databasePath);

                Directory.CreateDirectory(directory);
                eventWriter = new StreamWriter(Path.Combine(directory, baseName + ".events.log"), append: true)
                {
                    AutoFlush = true
                };
                pathWriter = new StreamWriter(Path.Combine(directory, baseName + ".paths.log"), append: true)
                {
                    AutoFlush = false
                };

                string parserHtmlPath = Path.Combine(directory, baseName + ".parser.html");
                bool writeHeader = !File.Exists(parserHtmlPath) || new FileInfo(parserHtmlPath).Length == 0;
                parserHtmlWriter = new StreamWriter(parserHtmlPath, append: true)
                {
                    AutoFlush = true
                };

                if (writeHeader)
                {
                    parserHtmlWriter.WriteLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"utf-8\"><title>Browser Reviewer Parser Log</title>");
                    parserHtmlWriter.WriteLine("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:24px;color:#202124}table{border-collapse:collapse;width:100%;font-size:13px}th,td{border:1px solid #d0d7de;padding:7px;vertical-align:top}th{background:#f6f8fa;text-align:left}.high{color:#116329;font-weight:600}.medium{color:#9a6700;font-weight:600}.low{color:#cf222e;font-weight:600}</style></head><body>");
                    parserHtmlWriter.WriteLine("<h1>Browser Reviewer Parser Log</h1><table><thead><tr><th>Time</th><th>Artifact</th><th>Browser</th><th>Parser version</th><th>Confidence</th><th>Evidence source</th><th>Records</th><th>Source file</th><th>Notes</th></tr></thead><tbody>");
                }
            }
        }

        public static void Log(RichTextBox? target, string message, BrowserReviewerLogLevel level = BrowserReviewerLogLevel.Info, string? context = null)
        {
            string line = FormatLine(message, level, context);
            Color color = GetColor(level, target?.ForeColor ?? SystemColors.ControlText);

            WriteEventLine(line);

            if (target == null)
            {
                WriteConsoleLine(line, level);
                return;
            }

            if (target.InvokeRequired)
            {
                target.Invoke((Action)(() => AppendToRichTextBox(target, line, color)));
                return;
            }

            AppendToRichTextBox(target, line, color);
        }

        public static void Log(RichTextBox? target, string message, Color? legacyColor)
        {
            Log(target, message, InferLevel(legacyColor), null);
        }

        public static void LogParserRun(
            string artifact,
            string browser,
            string parserVersion,
            string parseConfidence,
            string evidenceSource,
            int records,
            string sourceFile,
            string notes)
        {
            string message = $"{artifact} parser {parserVersion} processed {records} records for {browser}. Confidence: {parseConfidence}. Source: {evidenceSource}.";
            Log(null, message, BrowserReviewerLogLevel.Debug, "Parser");

            lock (SyncRoot)
            {
                if (parserHtmlWriter == null)
                {
                    return;
                }

                string confidenceClass = (parseConfidence ?? string.Empty).ToLowerInvariant();
                parserHtmlWriter.WriteLine(
                    "<tr>" +
                    $"<td>{Html(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))}</td>" +
                    $"<td>{Html(artifact)}</td>" +
                    $"<td>{Html(browser)}</td>" +
                    $"<td>{Html(parserVersion)}</td>" +
                    $"<td class=\"{Html(confidenceClass)}\">{Html(parseConfidence)}</td>" +
                    $"<td>{Html(evidenceSource)}</td>" +
                    $"<td>{records}</td>" +
                    $"<td>{Html(sourceFile)}</td>" +
                    $"<td>{Html(notes)}</td>" +
                    "</tr>");
            }
        }

        public static async Task TracePathAsync(string fullPath)
        {
            StreamWriter? writer;
            lock (SyncRoot)
            {
                writer = pathWriter;
            }

            if (writer == null)
            {
                return;
            }

            try
            {
                await writer.WriteLineAsync(fullPath);
            }
            catch (Exception ex)
            {
                Log(null, $"Unable to write path trace: {ex.Message}", BrowserReviewerLogLevel.Warning);
            }
        }

        public static void Close()
        {
            lock (SyncRoot)
            {
                CloseWriters();
                configuredDatabasePath = null;
            }
        }

        private static string FormatLine(string message, BrowserReviewerLogLevel level, string? context)
        {
            string levelText = level.ToString().ToUpperInvariant();
            string contextText = string.IsNullOrWhiteSpace(context) ? string.Empty : $" [{context}]";
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{levelText}]{contextText} {message}";
        }

        private static void AppendToRichTextBox(RichTextBox target, string line, Color color)
        {
            int start = target.TextLength;
            target.SelectionStart = start;
            target.SelectionLength = 0;
            target.SelectionColor = color;
            target.AppendText(line + Environment.NewLine);
            target.SelectionColor = target.ForeColor;
            target.SelectionStart = target.TextLength;
            target.ScrollToCaret();
        }

        private static void WriteEventLine(string line)
        {
            lock (SyncRoot)
            {
                eventWriter?.WriteLine(line);
            }
        }

        private static string Html(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        private static void WriteConsoleLine(string line, BrowserReviewerLogLevel level)
        {
            if (level == BrowserReviewerLogLevel.Error || level == BrowserReviewerLogLevel.Warning)
            {
                Console.Error.WriteLine(line);
                return;
            }

            Console.WriteLine(line);
        }

        private static Color GetColor(BrowserReviewerLogLevel level, Color fallback)
        {
            return level switch
            {
                BrowserReviewerLogLevel.Success => Color.Green,
                BrowserReviewerLogLevel.Warning => Color.DarkOrange,
                BrowserReviewerLogLevel.Error => Color.Red,
                BrowserReviewerLogLevel.Debug => Color.Gray,
                _ => fallback
            };
        }

        private static BrowserReviewerLogLevel InferLevel(Color? color)
        {
            if (color == null)
            {
                return BrowserReviewerLogLevel.Info;
            }

            Color value = color.Value;
            if (value.ToArgb() == Color.Red.ToArgb())
            {
                return BrowserReviewerLogLevel.Error;
            }

            if (value.ToArgb() == Color.Green.ToArgb())
            {
                return BrowserReviewerLogLevel.Success;
            }

            if (value.ToArgb() == Color.DarkOrange.ToArgb() || value.ToArgb() == Color.Orange.ToArgb())
            {
                return BrowserReviewerLogLevel.Warning;
            }

            return BrowserReviewerLogLevel.Info;
        }

        private static void CloseWriters()
        {
            pathWriter?.Flush();
            pathWriter?.Dispose();
            pathWriter = null;

            eventWriter?.Flush();
            eventWriter?.Dispose();
            eventWriter = null;

            parserHtmlWriter?.Flush();
            parserHtmlWriter?.Dispose();
            parserHtmlWriter = null;
        }
    }
}
