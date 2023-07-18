namespace Compendium.ServerGuard.AccountShield
{
    public class AccountShieldData
    {
        public string Id { get; set; }
        public AccountShieldFlags Flags { get; set; } = AccountShieldFlags.Clean;
    }
}