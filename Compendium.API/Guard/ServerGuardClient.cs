namespace Compendium.Guard
{
    public class ServerGuardClient
    {
        public virtual void Reload() { }
        public virtual bool TryCheck(ReferenceHub hub) => true;
    }
}