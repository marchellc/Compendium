using helpers.Dynamic;

using System;

namespace Compendium.Update
{
    public class UpdateHandlerData
    {
        public DynamicMethodDelegate Delegate { get; }

        public UpdateHandlerType Type { get; } = UpdateHandlerType.Engine;

        public DateTime LastExecute { get; set; }

        public bool ExecuteOnMain { get; set; }

        public int TickRate { get; set; } = 10;

        public object Handle { get; set; }

        public UpdateHandlerData(DynamicMethodDelegate del, UpdateHandlerType type, bool executeOnMain, int tickRate, object handle)
        {
            Delegate = del;
            Type = type;
            ExecuteOnMain = executeOnMain;
            TickRate = tickRate;
            Handle = handle;
        }
    }
}