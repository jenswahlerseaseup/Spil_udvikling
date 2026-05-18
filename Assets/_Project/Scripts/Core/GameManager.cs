using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : Singleton<GameManager>
{
    // True whenever the player can act (move, interact, attack).
    // Systems that block input (dialogue, cutscenes) set their own flags here.
    public static bool CanPlayerAct =>
        Instance == null || !DialogueManager.IsDialogueOpen;

    public void LoadScene(string sceneName) =>
        SceneManager.LoadScene(sceneName);

    public void GoToMainMenu() =>
        SceneManager.LoadScene(SceneNames.MainMenu);

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
