using System;
using System.CommandLine;
using System.Text;

//Example
//App bundle --output D:\Users\User\Desktop\תכנות\פרקטיקוד\1\bundleFile.txt
//App bundle -o bundleFile.txt

//Options
var bundleOptionOutput = new Option<string>("--output", "-o")
{
    Description = "file path and name",
    Required = true,
};
var bundleOptionLanguage = new Option<string[]>("--language", "-l")
{
    Description = "list of programming languages",
    Required = true,
    AllowMultipleArgumentsPerToken = true
};
var bundleOptionNote = new Option<bool>("--note", "-n")
{
    Description = "source code as a comment",
};
var bundleOptionSort = new Option<string>("--sort", "-s")
{
    Description = "sort files by 'name' or 'type'",
    DefaultValueFactory = _ => "name"
};
bundleOptionSort.AcceptOnlyFromAmong("name", "type");
var bundleOptionRemove = new Option<bool>("--remove", "-r")
{
    Description = "delete empty rows",
};
var bundleOptionAuthor = new Option<string>("--author", "-a")
{
    Description = "name of the file creator",
};
//create Command
var bundleCommand = new Command("bundle", "bundle code files to a single file");
var create_rspCommand = new Command("create-rsp", "create a response file with a ready bundle command");
//connect options to bundle
bundleCommand.Options.Add(bundleOptionOutput);
bundleCommand.Options.Add(bundleOptionLanguage);
bundleCommand.Options.Add(bundleOptionNote);
bundleCommand.Options.Add(bundleOptionSort);
bundleCommand.Options.Add(bundleOptionRemove);
bundleCommand.Options.Add(bundleOptionAuthor);
//inputAction
bundleCommand.SetAction(parseResult =>
{
    string file = parseResult.GetValue(bundleOptionOutput);
    string[] languages = parseResult.GetValue(bundleOptionLanguage);
    bool note = parseResult.GetValue(bundleOptionNote);
    string sort = parseResult.GetValue(bundleOptionSort);
    bool remove = parseResult.GetValue(bundleOptionRemove);
    string? author = parseResult.GetValue(bundleOptionAuthor);

    //languages
    var languageEnd = new Dictionary<string, string> {
        {"csharp",".cs" },
        {"java",".java" },
        {"python",".py" },
        {"javascript",".js" },
        {"typescript",".ts" },
        {"html",".html" },
        {"css",".css" },
        {"sql",".sql" }
    };
    languages = languages.Select(l => l.ToLower()).ToArray();

    var unknown = languages.Where(l => l != "all" && !languageEnd.ContainsKey(l)).ToList();
    if (unknown.Any())
    {
        Console.WriteLine($"Error: unknown language: {string.Join(", ", unknown)}");
        Console.WriteLine($"Supported: all, {string.Join(", ", languageEnd.Keys)}");
        return;
    }
    string currentFolder = Directory.GetCurrentDirectory();
    string[] allFiles = Directory.GetFiles(currentFolder, "*", SearchOption.AllDirectories);
    List<string> end;
    if (languages.Contains("all"))
    {
        end = languageEnd.Values.ToList();
    }
    else
    {
        end = languages.Where(lang => languageEnd.ContainsKey(lang)).Select(lang => languageEnd[lang]).ToList();
    }

    string[] excludedFolders = { "bin", "obj", "debug", "release", ".vs", ".git", "node_modules" };

    var codeFiles = allFiles.Where(f =>
    {
        var parts = Path.GetRelativePath(currentFolder, f)
                        .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        bool inExcluded = parts.Take(parts.Length - 1)
                               .Any(p => excludedFolders.Contains(p.ToLower()));
        return !inExcluded && end.Contains(Path.GetExtension(f).ToLower());
    }).ToList();
    if (codeFiles.Count == 0)
    {
        Console.WriteLine("No matching code files were found.");
        return;
    }
    //sort
    if (sort == "type")
    {
        codeFiles.Sort((a, b) => string.Compare(Path.GetExtension(a), Path.GetExtension(b)));
    }
    else
    {
        codeFiles.Sort((a, b) => string.Compare(Path.GetFileName(a), Path.GetFileName(b)));
    }
    //note + //remove + //author
    StringBuilder stringBuilder = new StringBuilder();
    if (author != null)
    {
        stringBuilder.AppendLine("//Author: " + author);
    }
    foreach (var code in codeFiles)
    {
        if (note)
        {
            var source = Path.GetRelativePath(currentFolder, code);
            stringBuilder.AppendLine("//" + source);
        }
        if (remove)
        {
            string[] lines = File.ReadAllLines(code);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    stringBuilder.AppendLine(line);
                }
            }
        }
        else
        {
            stringBuilder.AppendLine(File.ReadAllText(code));
        }
    }

    //file
    try
    {
        File.WriteAllText(file, stringBuilder.ToString());
        Console.WriteLine($"File created: {Path.GetFullPath(file)}");
    }
    catch (DirectoryNotFoundException)
    {
        Console.WriteLine("Error: the folder path is invalid");
    }
    catch (UnauthorizedAccessException)
    {
        Console.WriteLine("Error: no permission to write to this location");
    }
    catch (IOException ex)
    {
        Console.WriteLine($"Error: could not write the file ({ex.Message})");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Unexpected error: {ex.Message}");
    }
});
//create-rsp
create_rspCommand.SetAction(parseResult =>
{
    //questions and answers
    Console.WriteLine("enter the programming languages you wish to include, or all for everything:");
    string langs = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(langs))
    {
        Console.WriteLine("at least one language is required or enter \"all\"!");
        langs = Console.ReadLine();
    }
    Console.WriteLine("enter full path or file name:");
    string output = Console.ReadLine();
    while (string.IsNullOrWhiteSpace(output))
    {
        Console.WriteLine("output is required. enter full path or file name:");
        output = Console.ReadLine();
    }
    Console.WriteLine("include the source of the code as a comment in the bundle file? (y/n)");
    bool note = Console.ReadLine()?.Trim().ToLower() == "y";
    Console.WriteLine("the order of copying code files based on the type? (y/n)");
    bool sort = Console.ReadLine()?.Trim().ToLower() == "y";
    Console.WriteLine("delete empty rows? (y/n)");
    bool remove = Console.ReadLine()?.Trim().ToLower() == "y";
    Console.WriteLine("write the name of the file creator as a comment at the top of the bundle file? If so, enter the name (or press Enter to skip):");
    string author = Console.ReadLine();
    Console.WriteLine("enter a name for the response file (press Enter for 'bundle'):");
    string rspName = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(rspName)) rspName = "bundle";
    if (!rspName.EndsWith(".rsp")) rspName += ".rsp";
    //build the inputs
    StringBuilder stringBuilder = new StringBuilder();
    stringBuilder.AppendLine("bundle");
    stringBuilder.AppendLine($"--output \"{output}\"");
    stringBuilder.AppendLine($"--language {langs}");
    if (note) stringBuilder.AppendLine("--note");
    if (sort) stringBuilder.AppendLine("--sort type");
    if (remove) stringBuilder.AppendLine("--remove");
    if (!string.IsNullOrWhiteSpace(author)) stringBuilder.AppendLine($"--author \"{author}\"");
    //write to file
    try
    {
        File.WriteAllText(rspName, stringBuilder.ToString());
        Console.WriteLine($"Response file created: {Path.GetFullPath(rspName)}");
        Console.WriteLine("Run it with: App @" + rspName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: could not create the response file ({ex.Message})");
    }

});

var rootCommand = new RootCommand("Root command for file Bundler CLI");
rootCommand.Subcommands.Add(bundleCommand);
rootCommand.Subcommands.Add(create_rspCommand);
return rootCommand.Parse(args).Invoke();