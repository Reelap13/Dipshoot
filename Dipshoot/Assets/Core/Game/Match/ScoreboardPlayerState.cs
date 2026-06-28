using System;

namespace Game.MatchMode
{
    [Serializable]
    public struct ScoreboardPlayerState : IEquatable<ScoreboardPlayerState>
    {
        public int PlayerId;
        public string Nickname;
        public TeamId TeamId;
        public int Kills;
        public int Deaths;
        public int CapturePresenceSeconds;

        public bool Equals(ScoreboardPlayerState other)
        {
            return PlayerId == other.PlayerId &&
                Nickname == other.Nickname &&
                TeamId == other.TeamId &&
                Kills == other.Kills &&
                Deaths == other.Deaths &&
                CapturePresenceSeconds == other.CapturePresenceSeconds;
        }

        public override bool Equals(object obj)
        {
            return obj is ScoreboardPlayerState other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                PlayerId,
                Nickname,
                TeamId,
                Kills,
                Deaths,
                CapturePresenceSeconds);
        }
    }
}
