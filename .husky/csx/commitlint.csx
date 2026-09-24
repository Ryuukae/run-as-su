using System;
using System.IO;
using System.Text.RegularExpressions;

var msg = File.ReadAllLines(Args[0]);
if (msg.Length == 0) return 1;

var subject = msg[0];
var pattern = @"^(feat|fix|build|chore|ci|docs|style|refactor|perf|test)(\([a-z0-9\-]+\))?!?: [a-z0-9].+";

if (!Regex.IsMatch(subject, pattern))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("Invalid commit message subject: " + subject);
    Console.WriteLine("Must match Conventional Commits format: type(scope): subject");
    Console.ResetColor();
    return 1;
}
return 0;
