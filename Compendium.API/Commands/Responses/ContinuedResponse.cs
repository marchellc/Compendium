using System;

namespace Compendium.Commands.Responses
{
    public class ContinuedResponse<TType> : ContinuedResponseBase
    {
        public override Type ResponseType { get; } = typeof(TType);

        public ContinuedResponse(Func<TType, IResponse> callback, IResponse originalResponse) : base(callback, originalResponse) { }
    }
}