using BetterCommands;
using BetterCommands.Permissions;

using Compendium.IO.Saving;

using helpers;
using helpers.Attributes;
using helpers.Random;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Compendium.HttpServer.Authentification
{
    public static class HttpAuthentificator
    {
        private static SaveFile<CollectionSaveData<HttpAuthentificationKey>> _authedKeys;

        [Load]
        public static void Load()
        {
            if (_authedKeys != null)
            {
                _authedKeys.Load();
                return;
            }

            _authedKeys = new SaveFile<CollectionSaveData<HttpAuthentificationKey>>(Directories.GetDataPath("HttpKeys", "httpKeys"));
        }

        public static HttpAuthentificationResult TryAuthentificate(string id, string perm)
        {
            if (!TryGetKey(id, out var key))
                return HttpAuthentificationResult.InvalidKey;

            if (!key.IsPermitted(perm))
                return HttpAuthentificationResult.Unauthorized;

            return HttpAuthentificationResult.Authorized;
        }

        public static bool TryGetKey(string id, out HttpAuthentificationKey key)
            => _authedKeys.Data.TryGetFirst(x => x.Id == id, out key);

        public static HttpAuthentificationKey Generate(params string[] permits)
        {
            var key = RandomGeneration.Default.GetReadableString(15);

            while (_authedKeys.Data.Any(x => x.Id == key))
                key = RandomGeneration.Default.GetReadableString(15);

            var authKey = new HttpAuthentificationKey
            {
                Id = key,
                Permits = permits
            };

            _authedKeys.Data.Add(authKey);
            _authedKeys.Save();

            return authKey;
        }

        [Command("httpcreatekey", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Creates a new HTTP key.")]
        [Permission(PermissionLevel.Administrator)]
        public static string GenerateKeyCommand(ReferenceHub sender)
        {
            var key = Generate(CachedArray<string>.Array);
            File.WriteAllText(Directories.GetDataPath("LastGeneratedKey.txt", "generated_keys"), key.Id);
            return $"Generated key: {key.Id}";
        }

        [Command("httpaddperm", CommandType.RemoteAdmin, CommandType.GameConsole)]
        [Description("Adds a new perm to a HTTP key.")]
        [Permission(PermissionLevel.Administrator)]
        public static string AddPermKeyCommand(ReferenceHub sender, string keyId, string permit)
        {
            if (!TryGetKey(keyId, out var key))
            {
                key = new HttpAuthentificationKey() { Id = keyId, Permits = new string[] { permit } };

                _authedKeys.Data.Add(key);
                _authedKeys.Save();

                return $"Generated a new auth key: {key.Id}";
            }
            else
            {
                var list = new List<string>(key.Permits ?? CachedArray<string>.Array);

                if (!list.Contains(permit))
                    list.Add(permit);

                key.Permits = list.ToArray();

                _authedKeys.Save();

                return $"Added perm '{permit}' to key.";
            }
        }
    }
}