using System.IO;
using UnityEngine;

public static class SaveLoadManager
{
    private static string PathFor(string fileName)
        => Path.Combine(Application.persistentDataPath, fileName);

    public static void Save<T>(string fileName, T data)
    {
        string path = PathFor(fileName);
        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(path, json);
#if UNITY_EDITOR
        Debug.Log($"Saved to {path}");
#endif
    }

    public static bool Load<T>(string fileName, out T data) where T : new()
    {
        string path = PathFor(fileName);
        if (!File.Exists(path))
        {
            data = new T();
            return false;
        }
        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<T>(json);
        return true;
    }
}
