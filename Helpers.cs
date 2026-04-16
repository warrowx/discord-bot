using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace DiscordBot
{
    internal class Helpers
    {
        // Получить пользователя
        public static SocketGuildUser GetUserFromCommand(SocketSlashCommand command)
        {
            return (SocketGuildUser)command.Data.Options.First().Value;
        }

        // Получить любой параметр по имени

        public static T GetOptions<T>(SocketSlashCommand command, string name)
        {
            return (T)command.Data.Options.First(o => o.Name == name).Value;
        }

        // Проверить, есть ли у пользователя конкретная роль
        public static bool HasRole(SocketGuildUser user, string roleName)
        {
            return user.Roles.Any(r => r.Name == roleName);
        }

        // Проверить, есть ли у пользователя роль по ID
        public static bool HasRole(SocketGuildUser user, ulong roleId)
        {
            return user.Roles.Any(r => r.Id == roleId);
        }

        // Проверить, есть ли у пользователя права на управление ролями
        public static bool CanManageRoles(SocketGuildUser user)
        {
            return user.GuildPermissions.Administrator ||
                user.GuildPermissions.ManageRoles ||
                HasRole(user, "don pollo");
        }

        // Получить роль по имени
        public static SocketRole GetRole(SocketGuild guild, string roleName)
        {
            return guild.Roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
        }

    }
}