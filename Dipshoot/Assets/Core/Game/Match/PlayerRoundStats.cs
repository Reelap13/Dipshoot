namespace Game.MatchMode
{
    public class PlayerRoundStats
    {
        public int PlayerId;
        public TeamId TeamId;
        public int Kills;
        public int Deaths;
        public int DamageDealt;
        public int DamageTaken;
        public float CapturePresenceTime;

        public void ResetRound()
        {
            Kills = 0;
            Deaths = 0;
            DamageDealt = 0;
            DamageTaken = 0;
            CapturePresenceTime = 0f;
        }
    }
}
