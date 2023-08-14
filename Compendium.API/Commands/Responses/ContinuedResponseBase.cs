using System;

namespace Compendium.Commands.Responses
{
    public class ContinuedResponseBase : IResponse
    {
        public Delegate Callback { get; }
        public IResponse Response { get; }

        public virtual Type ResponseType { get; }

        public bool IsContinued => true;

        public string FormulateString() => Response.FormulateString();

        public ContinuedResponseBase(Delegate callback, IResponse originalResponse)
        {
            Callback = callback;
            Response = originalResponse;
        }
    }
}