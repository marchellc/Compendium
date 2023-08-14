namespace Compendium.Commands
{
    public interface IResponse
    {
        bool IsContinued { get; }

        string FormulateString();
    }
}