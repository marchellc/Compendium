using System;

namespace Compendium.Commands.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RemoteAdminCommandAttribute : CommandAttributeBase
    {
        public override CommandSource Source => CommandSource.RemoteAdmin;
    }
}