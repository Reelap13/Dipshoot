using UnityEngine;

public static class ClientConnectionSettings
{
    public const string PlayerIdPrefsKey = "ClientConnection.PlayerId";

    public static string PlayerId => PlayerPrefs.GetString(PlayerIdPrefsKey, string.Empty).Trim();

    public static void SavePlayerId(string playerId)
    {
        PlayerPrefs.SetString(PlayerIdPrefsKey, playerId.Trim());
        PlayerPrefs.Save();
    }
}
