using helpers.Dynamic;

using System;

namespace Compendium.Scheduling.Execution
{
    public struct ExecutionData
    {
        public DynamicMethodDelegate Target;

        public Action<ExecutionResult> Callback;

        public object Handle;
        public object[] Args;

        public ExecutionThread Thread;

        public DateTime Created;
        public DateTime? At;

        public int? Repeat;

        public bool IsMeasured;

        public ExecutionData(DynamicMethodDelegate target, object handle, object[] args, DateTime created, DateTime? at, int? repeat, bool measure, ExecutionThread thread)
        {
            Target = target;

            Handle = handle;
            Args = args;

            Created = created;

            At = at;

            Repeat = repeat;

            IsMeasured = measure;

            Thread = thread;
        }
    }
}
