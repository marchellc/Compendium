using System.Collections.Generic;

namespace Compendium.Punishments
{
    public interface IPunishmentHandler
    {
        void Load();
        void Save();

        void Advance();

        void Issue(IPunishment punishment);
        void Remove(IPunishment punishment);

        IList<IPunishment> ListAll();
        IList<IPunishment> ListTarget(string targetId);
        IList<IPunishment> ListIssuer(string issuerId);
    }
}