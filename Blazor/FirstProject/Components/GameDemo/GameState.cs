namespace FirstProject.Components.GameDemo;

public class GameState
{
    private readonly List<Player> players = [];
    private bool gameOver;
    private int gameRoungsPlayed;

    public GameState()
    {
        players.Add(new Player { Name = "Player" });
        players.Add(new Player { Name = "Opponent" });
    }

    public void EndGame()
    {
        gameOver = true;
        gameRoungsPlayed = 0;
    }

    public void ResetGame()
    {
        gameOver = true;

        foreach (var player in players)
        {
            player.Points = 0;
        }
    }

    public async Task PlayPiece(int position)
    {
        await Task.CompletedTask;
    }
}
