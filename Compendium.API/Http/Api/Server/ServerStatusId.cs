namespace Compendium.HttpApi
{
    public enum ServerStatusId
    {
        Idle = 0,
        
        WaitingForPlayers = 1,

        RoundInProgress = 2,
        RoundEnding = 3,
        RoundRestarting = 4
    }
}