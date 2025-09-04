using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance { get; private set; }
    [SerializeField] private Button backButton;

    private float gameTime;
    private void Awake()
    {
        instance = this;
        backButton.onClick.AddListener(() =>
        {
            SceneLoader.LoadScene(SceneLoader.Scene.MainMenuScene);
        });
    }

    private void Update()
    {
        gameTime += Time.deltaTime;
    }

    public float GetGameTime() => gameTime;
}
