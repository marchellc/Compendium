using System;

namespace Compendium.Scheduling.Execution
{
    public struct ExecutionResult
    {
        public object ReturnValue;

        public bool NoException;

        public Exception Exception;

        public double Time;

        public ExecutionResult(object value, bool exc, Exception exception, double time)
        {
            ReturnValue = value;
            NoException = exc;
            Exception = exception;
            Time = time;
        }
    }
}