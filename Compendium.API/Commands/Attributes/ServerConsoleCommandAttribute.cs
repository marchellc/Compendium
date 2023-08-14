using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ServerConsoleCommandAttribute : CommandAttributeBase
    {
        public override CommandSource Source => CommandSource.ServerConsole;
    }
}