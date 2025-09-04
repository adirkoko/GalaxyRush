using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public enum Scene
    {
        MainMenuScene,
        GameScene,
        StoreScene,
    }


    public static void LoadScene(Scene scene)
        => SceneManager.LoadScene(scene.ToString());

    public static AsyncOperation LoadSceneAsync(Scene scene, LoadSceneMode mode = LoadSceneMode.Single)
        => SceneManager.LoadSceneAsync(scene.ToString(), mode);

    public static void ReloadCurrent()
        => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public static AsyncOperation Unload(Scene scene)
        => SceneManager.UnloadSceneAsync(scene.ToString());
}
