using System;

[Serializable]
public class ZoidsGameJoltSavePayload
{
    public int saveVersion = 1;

    public string userId = "";
    public string username = "";
    public string savedAtUtc = "";

    public string playerProfileJson = "";
    public string unitProgressJson = "";
    public string playerTeamsJson = "";
    public string areaBattleStateJson = "";
    public string perkProgressJson = "";
    public string scoreboardProgressJson = "";

    public void Touch(string userId, string username)
    {
        this.userId = userId;
        this.username = username;
        savedAtUtc = DateTime.UtcNow.ToString("o");
    }
}
