using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameManager : Singleton<GameManager>
{
    public event Action<bool> PauseChanged;

    public bool IsPaused { get; private set; }

    public static bool CanPlayerAct =>
        (Instance == null || !Instance.IsPaused) && !DialogueManager.IsDialogueOpen;

    // ── Pause ──────────────────────────────────────────────────────────────────

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused) return;
        IsPaused          = paused;
        Time.timeScale    = paused ? 0f : 1f;
        PauseChanged?.Invoke(paused);
    }

    public void TogglePause() => SetPaused(!IsPaused);

    // ── Scene ─────────────────────────────────────────────────────────────────

    public void LoadScene(string sceneName)
    {
        SetPaused(false);
        SceneManager.LoadScene(sceneName);
    }

    public void GoToMainMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
