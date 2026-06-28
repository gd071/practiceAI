using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
    MainMenu,
    Field,
    Battle,
    Event,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    public static event System.Action<GameState> OnGameStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ChangeState(GameState.MainMenu);
    }

    public void ChangeState(GameState newState)
    {
        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
        Debug.Log($"[GameManager] State changed to: {newState}");
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartBattle()
    {
        ChangeState(GameState.Battle);
        LoadScene("BattleScene");
    }

    public void GoToField()
    {
        ChangeState(GameState.Field);
        LoadScene("FieldScene");
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
        LoadScene("GameOverScene");
    }

    public void GoToMainMenu()
    {
        ChangeState(GameState.MainMenu);
        LoadScene("MainMenuScene");
    }
}
