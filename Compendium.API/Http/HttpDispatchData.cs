using Compendium.Calls;

using System;
using System.Net.Http;

namespace Compendium.Http
{
    public class HttpDispatchData
    {
        private int _requeueCount;
        private string _response;
        private Action<HttpDispatchData> _onResponse;

        public string Target { get; }
        public string Response => _response;

        public int RequeueCount => _requeueCount;

        public HttpRequestMessage Request { get; }

        public HttpDispatchData(string target, HttpRequestMessage httpRequestMessage, Action<HttpDispatchData> onResponse)
        {
            _requeueCount = 0;
            _response = null;
            _onResponse = onResponse;

            Target = target;
            Request = httpRequestMessage;
        }

        internal void OnRequeued()
            => _requeueCount++;

        internal void OnReceived(string response)
        {
            _response = response;
            CallHelper.SafeDelegate(_onResponse, this);
        }
    }
}
