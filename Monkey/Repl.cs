namespace Monkey;

public static class Repl
{
    private const string Prompt = ">> ";

    private const string MonkeyFace = "MONKEY!!!";

    public static void Start()
    {
        var environment = new Environment();
        while (true)
        {
            Console.Write(Prompt);
            string? line = Console.ReadLine();

            if (line == null) return;

            var lexer = new Lexer(line);
            var parser = new Parser(lexer);

            Program program = parser.ParseProgram();
            if (parser.Errors.Count != 0)
            {
                PrintParserErrors(parser.Errors);
                continue;
            }

            Object? evaluated = Evaluator.Evaluate(program, environment);
            if (evaluated != null) Console.WriteLine(evaluated.Inspect());
        }
    }

    private static void PrintParserErrors(List<string> errors)
    {
        Console.WriteLine(MonkeyFace);
        Console.WriteLine("Whoops! We ran into some monkey business here!");
        Console.WriteLine(" Parser Errors:");

        foreach (string message in errors) Console.WriteLine($"\t{message}");
    }
}
