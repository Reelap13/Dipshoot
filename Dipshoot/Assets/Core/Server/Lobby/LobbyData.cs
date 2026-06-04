using System.Collections.Generic;

namespace Server.Lobby
{
    [System.Serializable]
    public class LobbyData 
    {
        public int Id;
        public string Code;
        public int PlayersCapacity;
        public int MapId;
        public string SelectedPresetId;
        public int SelectedSeed;
        public int SelectedRoundsCount;
        public string SelectedResultUrl;
        public bool IsConnectionBlocked;
        public List<LobbyPlayerData> Players;

        public LobbyData() { }
        public LobbyData(int lobby_id, string lobby_map, int lobby_capacity, int map_id)
        {
            Id = lobby_id;
            Code = lobby_map;
            PlayersCapacity = lobby_capacity;
            MapId = map_id;
            SelectedPresetId = string.Empty;
            SelectedRoundsCount = 1;
            SelectedResultUrl = string.Empty;
            IsConnectionBlocked = false;
            Players = new();
        }

        public void AddPlayer(LobbyPlayerData data) => Players.Add(data);
        public LobbyPlayerData RemovePlayer(int player_id)
        {
            var player = GetPlayer(player_id);
            if (player == null) return null;

            Players.Remove(player);
            return player;
        }

        public LobbyPlayerData GetPlayer(int player_id)
        {
            foreach (var player in Players)
                if (player.PlayerId == player_id)
                    return player;
            return null;
        }

        public bool IsHasPlace() => Players.Count < PlayersCapacity;
    }
}
