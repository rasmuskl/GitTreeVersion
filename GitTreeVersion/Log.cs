using Spectre.Console;

namespace GitTreeVersion
{
    public static class Log
    {
        public static bool IsDebug { get; set; }

        public static void Debug(string markup)
        {
            if (!IsDebug)
            {
                return;
            }
            
            AnsiConsole.MarkupLine(markup);
        }

        public static void Warning(string text)
        {
            AnsiConsole.MarkupLine($"[yellow]{text}[/]");
        }
    }
}