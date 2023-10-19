namespace CustomAlbums.Data;

public class CustomScore
{
    public float Accuracy;
    public string AccuracyStr;
    public float Clear;
    public int Combo;
    public int Evaluate;
    public int FailCount;
    public bool Passed;
    public int Score;
}

public class CustomData
{
    public List<string> Collections = new();
    public Dictionary<string, List<int>> FullCombo = new();
    public List<string> Hides = new();
    public Dictionary<string, Dictionary<int, CustomScore>> Highest = new();
    public List<string> History = new();
    public string SelectedAlbum;
    public int SelectedDifficulty;
}