namespace Compendium.UserId
{
    public struct UserIdValue
    {
        private string m_FullId;
        private string m_ClearId;
        private string m_Type;

        private long m_ParsedId;

        private UserIdType m_ParsedType;

        public string FullId => m_FullId;
        public string ClearId => m_ClearId;
        public string TypeRepresentation => m_Type;

        public long Id => m_ParsedId;

        public UserIdType Type => m_ParsedType;

        public UserIdValue(string full, string clear, string typeR, long parsed, UserIdType parsedType)
        {
            m_FullId = full;
            m_ClearId = clear;
            m_Type = typeR;
            m_ParsedId = parsed;
            m_ParsedType = parsedType;
        }

        public bool TryMatch(string id)
        {
            if (UserIdHelper.TryParse(id, out var idValue))
            {
                return TryMatch(idValue);
            }

            return false;
        }

        public bool TryMatch(UserIdValue userIdValue) => userIdValue.ToString() == ToString();

        public override string ToString() => m_FullId;
    }
}