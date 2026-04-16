using Discord;
using Discord.Addons.Hosting;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Windows.Input;


namespace DiscordBot
{
    internal class Program
    {
        private static DiscordSocketClient client;

        private static async Task Main()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            client = new DiscordSocketClient();
            client.Log += Log;
            client.Ready += Client_Ready;
            client.SlashCommandExecuted += SlashCommandHandler;

            // Токен бота
            var token = config["Token"];

            // Запуск бота
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }

        // Логи
        private static Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        
        // Сам Бот
        
        public static async Task Client_Ready()
        {
            Console.WriteLine("Создаем команды...");

            ulong guildId = 877451380879683584;
            var guild = client.GetGuild(guildId);


            if (guild == null)
            {
                Console.WriteLine("Сервер не найден. Проверь guildId");
                return;
            }

            var commands = new[]
            {
                new SlashCommandBuilder()
                .WithName("list-roles")
                .WithDescription("Показывает все роли пользователя")
                .AddOption("user", ApplicationCommandOptionType.User, "Пользователи, роли которых вы хотите увидеть", isRequired: true),

                new SlashCommandBuilder()
                .WithName("remind")
                .WithDescription("Простая напоминалка")
                .AddOption("time", ApplicationCommandOptionType.Integer, "Когда напомнить?", isRequired: true)
                .AddOption("message", ApplicationCommandOptionType.String, "Что напомнить?", isRequired: true),

                new SlashCommandBuilder()
                .WithName("67")
                .WithDescription("Проверка на крутость")
                .AddOption("user", ApplicationCommandOptionType.User, "Пользователи, которых надо проверить на крутость", isRequired: true),

                new SlashCommandBuilder()
                .WithName("addrole")
                .WithDescription("Выдать роль")
                .AddOption("user", ApplicationCommandOptionType.User, "Пользователь", isRequired: true)
                .AddOption("role", ApplicationCommandOptionType.Role, "Роль", isRequired: true),

                new SlashCommandBuilder()
                .WithName("removerole")
                .WithDescription("Убрать роль")
                .AddOption("user", ApplicationCommandOptionType.User, "Пользователь", isRequired: true)
                .AddOption("role", ApplicationCommandOptionType.Role, "Роль", isRequired: true),

            };

            foreach (var cmd in commands)
            {
                try
                {
                    await client.Rest.CreateGuildCommand(cmd.Build(), guildId);
                    Console.WriteLine($"Команда {cmd.Name} создана!");
                }
                catch (HttpException ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                }
            }

        }
        // Слэш-команды
        private static async Task SlashCommandHandler(SocketSlashCommand command)
        {
            // Проверка на сообщение на сервере
            if (!command.GuildId.HasValue)
            {
                await command.RespondAsync("Команда работает только на сервере", ephemeral: true);
                return;

            }

            var guild = client.GetGuild(command.GuildId.Value);

            // Есть ли человек на сервере
            if (guild == null)
            {
                await command.RespondAsync("Не удалось найти сервер", ephemeral: true);
                return;
            }

            // Удалось ли найти пользователя
            var user = guild.GetUser(command.User.Id);

            if (user == null)
            {
                await command.RespondAsync("Не удалось найти пользователя", ephemeral: true);
                return;
            }

            // Проверка на права админа
            if (user.Id != guild.OwnerId && !user.GuildPermissions.Administrator)
            {
                await command.RespondAsync("У вас нет прав администратора для этой команды", ephemeral: true);
                return;
            }

            // Команды
            switch (command.Data.Name)
            {
                case "list-roles":
                    {
                        var targetUser = Helpers.GetUserFromCommand(command);

                        var roleList = string.Join(",\n", targetUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

                        var embedBuilder = new EmbedBuilder()
                            .WithAuthor(targetUser.ToString(), targetUser.GetAvatarUrl() ?? targetUser.GetDefaultAvatarUrl())
                            .WithTitle("Роли пользователя")
                            .WithDescription(roleList)
                            .WithColor(Color.Red)
                            .WithCurrentTimestamp();

                        await command.RespondAsync(embed: embedBuilder.Build());
                        break;
                    }
                case "remind":
                    {
                        var minutes = Helpers.GetOptions<long>(command, "time");
                        var reminderMessage = Helpers.GetOptions<string>(command, "message");

                        await command.RespondAsync($"Напомню через {minutes} минут(ы): \"{reminderMessage}\"", ephemeral: true);
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay((int)minutes * 60 * 1000);
                            await command.User.SendMessageAsync($"**Напоминание!**\n{reminderMessage}");
                        });
                        break;
                    }
                case "67":
                    {
                        var targetUser = Helpers.GetUserFromCommand(command);
                        var isCool = Helpers.HasRole(targetUser, "67");
                        if (isCool)
                        {
                            await command.RespondAsync($"{targetUser.Mention} - настоящий крутыш бурмалдыш 67");
                        }
                        else
                        {
                            await command.RespondAsync($"{targetUser.Mention} - лох копченый, не 67");
                        }
                        break;
                    }
                case "addrole":
                    {
                        var executor = command.User as SocketGuildUser;
                        var targetUser = Helpers.GetUserFromCommand(command);
                        var role = Helpers.GetOptions<SocketRole>(command, "role");

                        // Проверка прав у исполнителя
                        if (!Helpers.CanManageRoles(executor))
                        {
                            await command.RespondAsync("У вас нет прав на управление ролями.", ephemeral: true);
                            break;
                        }

                        // Существует ли роль?
                        if (role == null)
                        {
                            await command.RespondAsync("Роль не найдена.", ephemeral: true);
                            break;
                        }

                        // Есть ли уже такая роль у пользователя?
                        if (Helpers.HasRole(targetUser, role.Id))
                        {
                            await command.RespondAsync($"У {targetUser.Mention} уже есть такая роль {role.Mention}", ephemeral: true);
                            break;
                        }

                        // Нельзя выдавать роль выше своей
                        if (executor.Roles.Max(r => r.Position) <= role.Position && !executor.GuildPermissions.Administrator)
                        {
                            await command.RespondAsync("Вы не можете выдавать роль выше своей или равную вашей самой высокой роли", ephemeral: true);
                            break;
                        }
                        try
                        {
                            await targetUser.AddRoleAsync(role);
                            await command.RespondAsync($"{targetUser.Mention} выдана роль {role.Mention}");
                        }
                        catch (Exception ex)
                        {
                            await command.RespondAsync($"Ошибка: {ex.Message}", ephemeral: true);
                        }
                        break;
                    }
                case "removerole":
                    {
                        var executor = command.User as SocketGuildUser;
                        var targetUser = Helpers.GetUserFromCommand(command);
                        var role = Helpers.GetOptions<SocketRole>(command, "role");

                        // Проверка прав у исполнителя
                        if (!Helpers.CanManageRoles(executor))
                        {
                            await command.RespondAsync("У вас нет прав на управление ролями.", ephemeral: true);
                            break;
                        }

                        // Существует ли роль?
                        if (role == null)
                        {
                            await command.RespondAsync("Роль не найдена.", ephemeral: true);
                            break;
                        }

                        // Есть ли уже такая роль у пользователя?
                        if (!Helpers.HasRole(targetUser, role.Id))
                        {
                            await command.RespondAsync($"У {targetUser.Mention} нет роли {role.Mention}", ephemeral: true);
                            break;
                        }

                        // Нельзя снимать роль выше своей
                        if (executor.Roles.Max(r => r.Position) <= role.Position && !executor.GuildPermissions.Administrator)
                        {
                            await command.RespondAsync("Вы не можете снять роль выше своей или равную вашей самой высокой роли", ephemeral: true);
                            break;
                        }
                        try
                        {
                            await targetUser.RemoveRoleAsync(role);
                            await command.RespondAsync($"У {targetUser.Mention} удалена роль {role.Mention}");
                        }
                        catch (Exception ex)
                        {
                            await command.RespondAsync($"Ошибка: {ex.Message}", ephemeral: true);
                        }
                        break;
                    }

                default:
                    await command.RespondAsync("Неизвестная команда");
                    break;
            }





        }
    }
}