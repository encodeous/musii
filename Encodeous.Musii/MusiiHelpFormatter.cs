using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace Encodeous.Musii
{
    public class MusiiHelpFormatter : BaseHelpFormatter
    {
        protected DiscordEmbedBuilder _embed;
        protected CommandContext _context;
        private IConfiguration _config;
        private string _pre;
        private bool _root = true;
        public MusiiHelpFormatter(CommandContext ctx, IConfiguration config) : base(ctx)
        {
            _config = config;
            _embed = new DiscordEmbedBuilder();
            _embed.WithTitle($"{_config["musii:BotName"]} help");
            _embed.WithFooter("No help found");
            _embed.WithColor(DiscordColor.Cyan);
            _context = ctx;
            _pre = Context.Prefix;
        }
        
        public string[] GetRequiredBotPermissions(Command command)
        {
            List<string> lst = new();
            var perms = command.ExecutionChecks.Select(x =>
            {
                if (x is RequireBotPermissionsAttribute p)
                {
                    // dirty trick xD
                    var q = p.Permissions.ToPermissionString().Split(", ");
                    if (q.Any()) return q;
                    return new string[]{};
                }
                return new string[]{};
            });
            foreach (var x in perms)
            {
                lst.AddRange(x);
            }

            return lst.ToArray();
        }

        public string[] GetRequiredPermissions(Command command)
        {
            List<string> lst = new();
            var perms = command.ExecutionChecks.Select(x =>
            {
                if (x is RequireUserPermissionsAttribute p)
                {
                    // dirty trick xD
                    var q = p.Permissions.ToPermissionString().Split(", ");
                    if (q.Any()) return q;
                    return new string[]{};
                }

                if (x is RequireOwnerAttribute)
                {
                    return new []{"Bot Owner"};
                }

                if (x is RequireGuildAttribute)
                {
                    return new []{"Run in Guild"};
                }

                if (x is RequireDirectMessageAttribute)
                {
                    return new []{"Run in DMs"};
                }

                if (x is RequireNsfwAttribute)
                {
                    return new []{"Run in a NSFW channel"};
                }
                return new string[]{};
            });
            foreach (var x in perms)
            {
                lst.AddRange(x);
            }

            return lst.ToArray();
        }

        public string[] GetUsages(Command command)
        {
            return command.Overloads.Select(x =>
            {
                return string.Join(" ", x.Arguments.Select(q =>
                {
                    var str = q.Name;
                    if (q.IsCatchAll)
                    {
                        str += "...";
                    }

                    if (q.DefaultValue is not null)
                    {
                        str += " = " + q.DefaultValue.ToString();
                    }

                    if (q.IsOptional)
                    {
                        return $"[{str}]";
                    }
                    else
                    {
                        return str;
                    }
                }));
            }).ToArray();
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            _root = false;
            if (command.IsHidden &&
                !command.RunChecksAsync(_context, true).GetAwaiter().GetResult().Any()
                && !_context.IsExecutedByBotOwner()) return this;
            _embed.WithTitle($"Showing help for command `{command.Name}`");
            _embed.WithDescription(command.Description);
            // usage
            var uSb = new StringBuilder();
            var usName = "";
            var usage = GetUsages(command);
            if (usage.Any() && !string.IsNullOrEmpty(usage[0]))
            {
                if (usage.Length == 1)
                {
                    usName = "Usage:";
                    uSb.AppendLine($"`{_pre}{command.QualifiedName} {usage[0]}`");
                }
                else
                {
                    usName = "Usages:";
                    foreach (var u in usage)
                    {
                        uSb.AppendLine($" - `{_pre}{command.QualifiedName} {u}`");
                    }
                }
            }
            else
            {
                usName = "Usage:";
                uSb.AppendLine($"`{_pre}{command.QualifiedName}`");
            }
            _embed.AddField(usName, uSb.ToString(), true);
            // permissions
            var reqPerms = GetRequiredPermissions(command);
            if (reqPerms.Any())
            {
                _embed.AddField("User Requirements", string.Join(", ", reqPerms.Select(x=>$"`{x}`")), true);
            }
            // bot permissions
            var reqBotPerms = GetRequiredBotPermissions(command);
            if (reqBotPerms.Any())
            {
                _embed.AddField("Bot Requirements", string.Join(", ", reqBotPerms.Select(x=>$"`{x}`")), true);
            }
            // aliases
            if (command.Aliases.Any())
            {
                _embed.AddField("Aliases", string.Join(", ", command.Aliases.Select(x=>$"`{x}`")), true);
            }
            _embed.WithFooter($"Showing detailed info.");
            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            var cmds = subcommands.ToList();
            if (_root)
            {
                // display groups as fields
                var groups = cmds.GroupBy(x => x.Module.ModuleType);
                foreach (var g in groups)
                {
                    if(g.Key == typeof(CommandsNextExtension.DefaultHelpModule)) continue;
                    StringBuilder sb = new();
                    string title = "";
                    if (g.Key.GetCustomAttributes().Any(x => x is DescriptionAttribute))
                    {
                        var attr = g.Key.GetCustomAttributes().OfType<DescriptionAttribute>().First();
                        title = attr.Description;
                    }
                    else
                    {
                        title = g.Key.Name;
                    }

                    sb.AppendJoin(", ", g.Select(x =>
                    {
                        if (x is CommandGroup)
                        {
                            return $"`{x.Name}*`";
                        }

                        return $"`{x.Name}`";
                    }));
                    _embed.AddField(title, sb.ToString());
                }
            }
            else
            {
                StringBuilder sb = new();
                // put under subcommands
                sb.AppendJoin(", ", cmds.Select(x =>
                {
                    if (x is CommandGroup)
                    {
                        return $"`{x.Name}*`";
                    }

                    return $"`{x.Name}`";
                }));
                _embed.AddField("Subcommands", sb.ToString());
            }
            if(cmds.Any()) _embed.WithFooter("Subcommands with a * prefix have more subcommands.");
            return this;
        }

        public override CommandHelpMessage Build()
        {
            return new (embed: _embed);
        }
    }
}