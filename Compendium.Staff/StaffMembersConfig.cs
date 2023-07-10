using Compendium.Features;
using Compendium.Helpers.UserId;

using helpers.Extensions;

using System;
using System.Collections.Generic;

namespace Compendium.Staff
{
    public class StaffMembersConfig
    {
        private Dictionary<UserIdValue, string> _members = new Dictionary<UserIdValue, string>();
        private Action<Dictionary<string, string>> _fillMembers;

        public Dictionary<string, string> Members
        {
            get
            {
                var dict = new Dictionary<string, string>();

                _members.ForEach(member => dict[member.Key.FullId] = member.Value);

                return dict;
            }
        }

        public StaffMembersConfig(Action<Dictionary<string, string>> fillMembers)
        {
            _fillMembers = fillMembers;
        }

        public void Reload()
        {
            _members.Clear();

            var dict = new Dictionary<string, string>();

            _fillMembers?.Invoke(dict);

            dict.ForEach(p =>
            {
                if (string.IsNullOrWhiteSpace(p.Key) || string.IsNullOrWhiteSpace(p.Value) || p.Key is "default")
                    return;

                if (!UserIdHelper.TryParse(p.Key, out var uid))
                {
                    FLog.Warn($"ID {p.Key} is not a valid User ID!");
                    return;
                }

                if (!StaffHandler.TryGetRole(p.Value, out var role))
                {
                    FLog.Warn($"Role {p.Value} is not a valid role!");
                    return;
                }

                _members[uid] = role.Key;
            });
        }

        public void Unload(Action<Dictionary<string, string>> saveMembers)
        {
            if (saveMembers != null)
                saveMembers(Members);

            _members.Clear();
            _fillMembers = null;
        }

        public bool TryGetKey(string userId, out string role)
        {
            role = null;

            if (!UserIdHelper.TryParse(userId, out var uid))
            {
                FLog.Warn($"Failed to get role key: invalid User ID provided! ({userId})");
                return false;
            }

            foreach (var pair in _members)
            {
                if (pair.Key.FullId == uid.FullId)
                {
                    role = pair.Value;
                    return true;
                }
            }

            return false;
        }
    }
}