namespace Compendium.Commands.Modules
{
    public interface IModuleCommand : ICommand
    {
        ICommandModule Module { get; }
    }
}