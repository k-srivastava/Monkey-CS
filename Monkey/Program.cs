using Monkey;
using Environment = System.Environment;

string username = Environment.UserName;

Console.WriteLine($"Hello, {username}! This is the Monkey programming language!");
Console.WriteLine("Feel free to type in commands.");
Repl.Start();
