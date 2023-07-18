using System;
using System.Collections.Generic;

namespace Compendium.ServerGuard.Dispatch
{
    public struct HttpDispatchData
    {
        public string address;

        public KeyValuePair<string, string>[] headers;
        public Action<string> callback;
    }
}