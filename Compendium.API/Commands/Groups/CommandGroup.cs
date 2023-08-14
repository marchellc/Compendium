using helpers.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Compendium.Commands.Groups
{
    public class CommandGroup : ICommandGroup
    {
        private string _name;

        private ICommandGroup _parent;

        private HashSet<ICommand> _commands;
        private HashSet<ICommandGroup> _children;

        public string Name => _name;

        public ICommandGroup Parent => _parent;

        public IReadOnlyCollection<ICommand> Commands => _commands;
        public IReadOnlyCollection<ICommandGroup> Children => _children;

        public CommandGroup(string name, ICommandGroup parent = null)
        {
            _name = name;
            _parent = parent;

            _commands = new HashSet<ICommand>();
            _children = new HashSet<ICommandGroup>();
        }

        public void Add(ICommandGroup child)
        {
            if (!_children.TryGetValue(child, out _))
            {
                child.SetParent(this);
                _children.Add(child);
            }
        }

        public void Add(ICommand command)
        {
            if (_commands.Contains(command))
                return;

            _commands.Add(command);
        }

        public void Remove(ICommandGroup child)
        {
            if (_children.TryGetValue(child, out var group))
            {
                group.SetParent(null);
                _children.Remove(group);
            }
        }

        public void Remove(ICommand command)
        {
            if (_commands.Remove(command))
                command.SetParent(null);
        }

        public void SetParent(ICommandGroup group)
            => _parent = group;

        public ICommand[] QueryCommands(string name, out int pos)
        {
            pos = 0;

            var cmdList = ListPool<ICommand>.Pool.Get();
            var parts = name.Split(' ');

            if (parts.Length > CommandHandler.TopPartCount)
                parts = parts.Take(CommandHandler.TopPartCount).ToArray();

            if (parts.Length != 1)
            {
                for (int i = 0; i < Children.Count; i++)
                {
                    if (i >= parts.Length && Children.Count == parts.Length)
                    {
                        cmdList.AddRange(Children.ElementAt(i).Commands.Where(c => CompareStrings(c.Name, parts.Last())));
                        pos = i;
                        break;
                    }

                    if (!CompareStrings(parts[i], Children.ElementAt(i).Name))
                        break;
                }
            }
            else
            {
                foreach (var cmd in Commands)
                {
                    if (cmd.Parent is null)
                        continue;

                    if (CompareStrings(cmd.Name, parts[0]))
                    {
                        cmdList.Add(cmd);
                        break;
                    }
                }
            }

            var cmds = cmdList.ToArray();

            ListPool<ICommand>.Pool.Push(cmdList);

            return cmds;
        }

        public static bool CompareStrings(string one, string two)
            => string.Equals(one, two, CommandHandler.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
    }
}
