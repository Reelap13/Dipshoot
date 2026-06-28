using System;
using Game.Players;

namespace Game.MatchMode
{
    public enum MatchKillWeapon : byte
    {
        None = 0,
        Primary = 1,
        Secondary = 2,
    }

    [Flags]
    public enum MatchKillTags : byte
    {
        None = 0,
        Body = 1,
        Head = 2,
    }

    [Serializable]
    public struct MatchKillPlayerInfo : IEquatable<MatchKillPlayerInfo>
    {
        public int PlayerId;
        public string Nickname;
        public TeamId TeamId;

        public bool Equals(MatchKillPlayerInfo other)
        {
            return PlayerId == other.PlayerId &&
                Nickname == other.Nickname &&
                TeamId == other.TeamId;
        }

        public override bool Equals(object obj)
        {
            return obj is MatchKillPlayerInfo other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(PlayerId, Nickname, TeamId);
        }
    }

    [Serializable]
    public struct MatchKillEvent : IEquatable<MatchKillEvent>
    {
        public int KillId;
        public MatchKillPlayerInfo Killer;
        public MatchKillPlayerInfo Victim;
        public MatchKillWeapon Weapon;
        public MatchKillTags Tags;

        public bool Equals(MatchKillEvent other)
        {
            return KillId == other.KillId &&
                Killer.Equals(other.Killer) &&
                Victim.Equals(other.Victim) &&
                Weapon == other.Weapon &&
                Tags == other.Tags;
        }

        public override bool Equals(object obj)
        {
            return obj is MatchKillEvent other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(KillId, Killer, Victim, Weapon, Tags);
        }

        public static MatchKillWeapon GetWeapon(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Primary => MatchKillWeapon.Primary,
                WeaponSlot.Pistol => MatchKillWeapon.Secondary,
                _ => MatchKillWeapon.None,
            };
        }

        public static MatchKillTags GetTags(PlayerHitboxType hitbox_type)
        {
            return hitbox_type == PlayerHitboxType.Head
                ? MatchKillTags.Head
                : MatchKillTags.Body;
        }
    }
}
