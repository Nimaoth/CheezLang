﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using Cheez.Ast;
using Cheez.Compiler;
using Cheez.Compiler.Parsing;

namespace CheezCLI
{
    class ConsoleErrorHandler : IErrorHandler
    {
        public bool HasErrors { get; set; }

        public ITextProvider TextProvider { get; set; }

        public void ReportError(IText text, ILocation location, string message, List<Error> subErrors, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            ReportError(new Error
            {
                Location = location,
                Message = message,
                SubErrors = subErrors
            }, callingFunctionFile, callingFunctionName, callLineNumber);
        }

        public void ReportError(Error error, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            HasErrors = true;

#if DEBUG
            Log($"{Path.GetFileName(callingFunctionFile)}:{callingFunctionName}():{callLineNumber}", ConsoleColor.DarkYellow);
#endif

            if (error.Location != null)
            {
                var text = TextProvider.GetText(error.Location);

                TokenLocation beginning = error.Location.Beginning;
                TokenLocation end = error.Location.End;

                // location, message
                Log($"{beginning}: {error.Message}", ConsoleColor.Red);
                PrintLocation(text, error.Location, linesBefore: 0);
            }
            else
            {
                Log(error.Message, ConsoleColor.Red);
            }

            // details
            if (error.Details?.Count > 0)
            {
                foreach (var d in error.Details)
                {
                    Console.WriteLine("|");

                    foreach (var line in d.message.Split('\n'))
                    {
                        if (line != "") 
                            Log("| " + line, ConsoleColor.White);
                    }

                    if (d.location != null)
                    {
                        var detailText = TextProvider.GetText(d.location);
                        PrintLocation(detailText, d.location, linesBefore: 0, highlightColor: ConsoleColor.Green);
                    }
                }
            }

            Console.Error.WriteLine();

            if (error.SubErrors?.Count > 0)
            {
                Log("| Related:", ConsoleColor.White);

                foreach (var e in error.SubErrors)
                {
                    ReportError(e, callingFunctionFile, callingFunctionName, callLineNumber);
                }
            }
        }

        public void ReportError(string message, [CallerFilePath] string callingFunctionFile = "", [CallerMemberName] string callingFunctionName = "", [CallerLineNumber] int callLineNumber = 0)
        {
            HasErrors = true;
#if DEBUG
            Log($"{Path.GetFileName(callingFunctionFile)}:{callingFunctionName}():{callLineNumber}", ConsoleColor.DarkYellow);
#endif

            Log(message, ConsoleColor.Red);
        }

        private void PrintLocation(IText text, ILocation location, bool underline = true, int linesBefore = 2, int linesAfter = 0, ConsoleColor highlightColor = ConsoleColor.Red, ConsoleColor textColor = ConsoleColor.DarkGreen)
        {
            TokenLocation beginning = location.Beginning;
            TokenLocation end = location.End;

            int index = beginning.index;
            int lineNumber = beginning.line;
            int lineStart = GetLineStartIndex(text, index);
            int lineEnd = GetLineEndIndex(text, end.end);
            int linesSpread = CountLines(text, index, end.end);

            var errorLineBackgroundColor = ConsoleColor.Black;
            int lineNumberWidth = (end.line + linesAfter).ToString().Length;

            // lines before current line
            {
                List<string> previousLines = new List<string>();
                int startIndex = lineStart;
                for (int i = 0; i < linesBefore && startIndex > 0; i++)
                {
                    var prevLineEnd = startIndex - 1;
                    var prevLineStart = GetLineStartIndex(text, prevLineEnd);
                    previousLines.Add(text.Text.Substring(prevLineStart, prevLineEnd - prevLineStart));

                    startIndex = prevLineStart;
                }

                for (int i = previousLines.Count - 1; i >= 0; i--)
                {
                    int line = lineNumber - 1 - i;
                    LogInline(string.Format($"{{0,{lineNumberWidth}}}> ", line), ConsoleColor.White);
                    Log(previousLines[i], textColor);
                }
            }

            // line containing error (may be multiple lines)
            {
                var firstLine = beginning.line;
                var ls = lineStart; // lineStart
                var le = GetLineEndIndex(text, index); // lineEnd
                var ei = Math.Min(le, end.end); // endIndex
                var i = index;

                for (var line = 0; line < linesSpread; ++line)
                {
                    var part1 = text.Text.Substring(ls, i - ls);
                    var part2 = text.Text.Substring(i, ei - i);
                    var part3 = text.Text.Substring(ei, le - ei);

                    LogInline(string.Format($"{{0,{lineNumberWidth}}}> ", line + firstLine), ConsoleColor.White);

                    LogInline(part1, textColor, errorLineBackgroundColor);
                    LogInline(part2, highlightColor, errorLineBackgroundColor);
                    Log(part3, textColor, errorLineBackgroundColor);

                    ls = le + 1;
                    i = ls;
                    le = GetLineEndIndex(text, i);
                    ei = Math.Min(le, end.end);
                }
            }

            // underline
            if (linesSpread == 1 && underline)
            {
                char firstChar = '^'; // ^ ~
                char underlineChar = '—'; // — ~
                var str = new string(' ', index - lineStart + lineNumberWidth + 2) + firstChar;
                if (end.end - index - 1 > 0)
                    str += new string(underlineChar, end.end - index - 1);
                Log(str, GetDarkColor(highlightColor));
            }

            // lines after current line
            {
                var sb = new StringBuilder();
                int lineBegin = lineEnd + 1;
                for (int i = 0; i < linesAfter; i++)
                {
                    int line = end.line + i + 1;
                    lineEnd = GetLineEndIndex(text, lineBegin);
                    if (lineEnd >= text.Text.Length)
                        break;
                    var str = text.Text.Substring(lineBegin, lineEnd - lineBegin);
                    Log(string.Format($"{{0,{lineNumberWidth}}}> "), ConsoleColor.White);
                    Log(str, textColor);
                    lineBegin = lineEnd + 1;
                }
            }
        }

        private ConsoleColor GetDarkColor(ConsoleColor color)
        {
            switch (color)
            {
            case ConsoleColor.Blue: return ConsoleColor.DarkBlue;
            case ConsoleColor.Cyan: return ConsoleColor.DarkCyan;
            case ConsoleColor.Gray: return ConsoleColor.DarkGray;
            case ConsoleColor.Green: return ConsoleColor.DarkGreen;
            case ConsoleColor.Magenta: return ConsoleColor.Magenta;
            case ConsoleColor.Red: return ConsoleColor.DarkRed;
            case ConsoleColor.Yellow: return ConsoleColor.DarkYellow;
            default: return color;
            }
        }

        private int CountLines(IText text, int start, int end)
        {
            int lines = 1;
            for (; start < end && start < text.Text.Length; start++)
            {
                if (text.Text[start] == '\n')
                    lines++;
            }

            return lines;
        }

        private int GetLineEndIndex(IText text, int currentIndex)
        {
            for (; currentIndex < text.Text.Length; currentIndex++)
            {
                if (text.Text[currentIndex] == '\n')
                    break;
            }

            return currentIndex;
        }

        private int GetLineStartIndex(IText text, int currentIndex)
        {
            if (currentIndex >= text.Text.Length)
                currentIndex = text.Text.Length - 1;

            if (text.Text[currentIndex] == '\n')
                currentIndex--;

            for (; currentIndex >= 0; currentIndex--)
            {
                if (text.Text[currentIndex] == '\n')
                    return currentIndex + 1;
            }

            return 0;
        }

        private void Log(string message, ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black)
        {
            var colf = Console.ForegroundColor;
            var colb = Console.BackgroundColor;
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = colf;
            Console.BackgroundColor = colb;
        }

        private void LogInline(string message, ConsoleColor foreground, ConsoleColor background = ConsoleColor.Black)
        {
            var colf = Console.ForegroundColor;
            var colb = Console.BackgroundColor;
            Console.ForegroundColor = foreground;
            Console.BackgroundColor = background;
            Console.Error.Write(message);
            Console.ForegroundColor = colf;
            Console.BackgroundColor = colb;
        }
    }
}