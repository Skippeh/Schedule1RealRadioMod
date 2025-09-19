using System;
using System.Collections.Generic;
using System.Reflection;
using Console = ScheduleOne.Console;

namespace RealRadio.Compnoents.ConsoleCommands;

public static class ConsoleCommandsManager
{
    private static bool commandsRegistered;

    public static void RegisterCommands()
    {
        if (commandsRegistered)
            return;

        commandsRegistered = true;

        var commands = CreateAllCommandsInAssembly();

        foreach (var command in commands)
        {
            if (Console.commands.ContainsKey(command.CommandWord))
            {
                Logger.LogWarning($"Skipping duplicate console command: {command.CommandWord}");
                continue;
            }

            Logger.LogDebug($"Registering console command: {command.CommandWord}");

            Console.commands.Add(command.CommandWord, command);
            Console.Commands.Add(command);
        }
    }

    private static IEnumerable<Console.ConsoleCommand> CreateAllCommandsInAssembly()
    {
        var result = new List<Console.ConsoleCommand>();
        Type consoleCommandType = typeof(Console.ConsoleCommand);

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (consoleCommandType.IsAssignableFrom(type) && !type.IsAbstract)
            {
                try
                {
                    result.Add((Console.ConsoleCommand)Activator.CreateInstance(type));
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to create instance of console command '{type.Name}': {ex}");
                }

            }
        }

        return result;
    }
}
