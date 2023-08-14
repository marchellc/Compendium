using System.Collections.Generic;

namespace Compendium.Commands
{
    public interface ICommandGroup
    {
        string Name { get; }

        ICommandGroup Parent { get; }

        IReadOnlyCollection<ICommand> Commands { get; }
        IReadOnlyCollection<ICommandGroup> Children { get; }

        void Add(ICommandGroup child);
        void Add(ICommand command);

        void Remove(ICommandGroup child);
        void Remove(ICommand command);

        void SetParent(ICommandGroup group);

        ICommand[] QueryCommands(string name, out int pos);
    }
}