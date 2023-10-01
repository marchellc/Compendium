using helpers;
using System;
using System.Net.Http;

namespace Compendium.Http
{
    public class HttpDispatchData
    {
        private int _requeueCount;
        private string _response;
        private Action<HttpDispatchData> _onResponse;
        private HttpRequestMessage _request;

        public string Target { get; }
        public string Response => _response;

        public int RequeueCount => _requeueCount;

        public HttpRequestMessage Request => _request;

        public HttpDispatchData(string target, HttpRequestMessage httpRequestMessage, Action<HttpDispatchData> onResponse)
        {
            _requeueCount = 0;
            _response = null;
            _onResponse = onResponse;

            Target = target;
            _request = httpRequestMessage;
        }

        internal void OnRequeued()
            => _requeueCount++;

        internal void OnReceived(string response)
        {
            _response = response;
            Calls.Delegate(_onResponse, this);
        }

        internal void RefreshRequest()
        {
            if (_requeueCount <= 0)
                return;

            if (_request != null)
            {
                var newReq = new HttpRequestMessage(_request.Method, _request.RequestUri);

                newReq.Content = _request.Content;
                newReq.Headers.Clear();

                _request.Headers.ForEach(header =>
                {
                    newReq.Headers.Add(header.Key, header.Value);
                });

                _request.Dispose();
                _request = newReq;
            }
        }
    }
}
