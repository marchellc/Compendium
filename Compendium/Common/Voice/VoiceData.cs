using helpers.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

using VoiceChat;

namespace Compendium.Common.Voice
{
    public class VoiceData
    {
        private readonly HashSet<ReferenceHub> m_AllowedReceivers = new HashSet<ReferenceHub>();
        private readonly HashSet<ReferenceHub> m_BlacklistedReceivers = new HashSet<ReferenceHub>();

        private Func<ReferenceHub, bool> m_Validator;

        private bool m_Active;
        private bool m_ResetOnRole = true;

        private string m_String;

        private VoiceChatChannel? m_ChannelOverride;

        public bool IsActive => m_Active;
        public bool IsWhitelistActive => m_AllowedReceivers.Any();
        public bool IsOverrideActive => m_ChannelOverride.HasValue;
        public bool IsStringSet => !string.IsNullOrWhiteSpace(m_String);

        public bool ShouldResetOnRole => m_ResetOnRole;

        public string String => m_String;

        public VoiceChatChannel Override => m_ChannelOverride.Value;     

        public bool IsAllowed(ReferenceHub hub)
        {
            if (m_AllowedReceivers.Any())
            {
                if (!m_AllowedReceivers.Contains(hub))
                {
                    return false;
                }    
            }

            if (m_BlacklistedReceivers.Contains(hub))
            {
                return false;
            }

            if (m_Validator != null)
            {
                if (!m_Validator(hub))
                {
                    return false;
                }
            }

            return true;
        }

        public void DontResetOnRoleChange() => m_ResetOnRole = false;
        public void ResetOnRoleChange() => m_ResetOnRole = true;

        public void SetString(string str) => m_String = str;
        public void RemoveString() => m_String = null;

        public void SetOverride(VoiceChatChannel voiceChatChannel) => m_ChannelOverride = voiceChatChannel;
        public void RemoveOverride() => m_ChannelOverride = null;

        public void Activate() => m_Active = true;
        public void Deactivate() => m_Active = false;

        public void ClearWhitelist() => m_AllowedReceivers.Clear();
        public void ClearBlacklist() => m_BlacklistedReceivers.Clear();

        public bool IsBlacklisted(ReferenceHub hub) => m_BlacklistedReceivers.Contains(hub);
        public bool IsWhitelisted(ReferenceHub hub) => m_AllowedReceivers.Contains(hub);

        public void AddWhitelist(ReferenceHub hub) => m_AllowedReceivers.Add(hub);
        public void AddBlacklist(ReferenceHub hub) => m_BlacklistedReceivers.Add(hub);

        public bool RemoveWhitelist(ReferenceHub hub) => m_AllowedReceivers.Remove(hub);
        public bool RemoveBlacklist(ReferenceHub hub) => m_BlacklistedReceivers.Remove(hub);

        public void SetValidator(Func<ReferenceHub, bool> validator) => m_Validator = validator;
        public void RemoveValidator() => SetValidator(null);

        public void ResetAll()
        {
            RemoveString();
            RemoveValidator();
            ResetOnRoleChange();
            Deactivate();
        }
    }
}