using helpers;

namespace Compendium.HttpServer.Authentification
{
    public class HttpAuthentificationKey
    {
        public string[] Permits { get; set; }
        public string Id { get; set; }

        public bool IsPermitted(string endpointPerm)
        {
            if (Permits is null || !Permits.Any())
                return false;

            if (Permits.Contains("*") || Permits.Contains(endpointPerm))
                return true;

            if (!endpointPerm.Contains("."))
                return false;

            var parts = endpointPerm.Split('.');

            for (int i = 0; i < parts.Length; i++)
            {
                if (Permits.Contains($"*.{parts[i]}") || Permits.Contains($"{parts[i]}."))
                    return true;
            }

            return false;
        }
    }
}