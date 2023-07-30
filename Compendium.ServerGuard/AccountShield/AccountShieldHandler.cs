using Compendium.Features;
using Compendium;

using helpers.Attributes;
using helpers.IO.Storage;

using System;

namespace Compendium.ServerGuard.AccountShield
{
    public static class AccountShieldHandler
    {
        private static SingleFileStorage<AccountShieldData> _accStorage;

        [Load]
        public static void Initialize()
        {
            if (_accStorage != null)
            {
                _accStorage.Save();
                _accStorage = null;
            }

            _accStorage = new SingleFileStorage<AccountShieldData>($"{FeatureManager.DirectoryPath}/account_cache");
            _accStorage.Load();
        }

        public static void Check(ReferenceHub hub)
        {
            GetData(hub.UserId(), data =>
            {

            });
        }

        private static void GetData(string userId, Action<AccountData> callback)
        {

        }
    }
}