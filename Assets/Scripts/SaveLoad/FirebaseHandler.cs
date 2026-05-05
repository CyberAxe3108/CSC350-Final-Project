using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;

public class FirebaseHandler : MonoBehaviour
{
    private static string GetSlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, UserSession.Username + "-SLOT" + slot + ".json");

    private static GameObject LoadPrefab(string prefabName)
    {
        string[] folders = { "Prefabs/PlaceableObjects/", "Prefabs/DepercatedObjects/", "Prefabs/" };
        foreach (string folder in folders)
        {
            GameObject prefab = Resources.Load<GameObject>(folder + prefabName);
            if (prefab != null) return prefab;
        }
        return null;
    }

    private void DestroyObjects()
    {
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Selectable"))
            Destroy(obj);
        BuildingSystem.ObjectCount = 0;
    }

    private string Serialize(GameObjectData[] items, string mapName) =>
        JsonUtility.ToJson(new Environment(items, mapName), true);

    private Environment Deserialize(string json) =>
        JsonUtility.FromJson<Environment>(json);

    public void SaveEnvironment(int slot) => SaveEnvironment(slot, "Slot " + slot);

    public void SaveEnvironment(int slot, string mapName)
    {
        List<GameObjectData> objectData = new List<GameObjectData>();
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Selectable"))
        {
            GameObjectData data = new(obj);
            if (data.IsValid) objectData.Add(data);
        }

        string json = Serialize(objectData.ToArray(), mapName);
        File.WriteAllText(GetSlotPath(slot), json);
        Debug.Log("Saved to: " + GetSlotPath(slot));
    }

    public void LoadEnvironment(int slot)
    {
        DestroyObjects();

        if (slot == 0) return;

        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No save found at: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        Environment env = Deserialize(json);

        foreach (GameObjectData data in env.Items)
        {
            GameObject prefab = LoadPrefab(data.prefabName);
            if (prefab == null)
            {
                Debug.LogError("Prefab not found: " + data.prefabName);
                continue;
            }

            GameObject obj = Instantiate(prefab);

            PlaceableObject placeableObj = obj.GetComponent<PlaceableObject>();
            if (placeableObj != null)
            {
                placeableObj.prefabName = data.prefabName;
                placeableObj.Place();
            }

            obj.name = data.prefabName + " #" + BuildingSystem.ObjectCount++;
            obj.transform.SetPositionAndRotation(data.position, data.rotation);
            obj.transform.localScale = data.scale;
            obj.transform.SetParent(null);
            obj.SetActive(true);
        }
    }

    public void GetSlotName(int slot, Action<string> onResult)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            onResult?.Invoke(null);
            return;
        }

        try
        {
            string json = File.ReadAllText(path);
            Environment env = Deserialize(json);
            string display = string.IsNullOrEmpty(env.SavedAt)
                ? env.Name
                : env.Name + "\n" + env.SavedAt;
            onResult?.Invoke(display);
        }
        catch
        {
            onResult?.Invoke(null);
        }
    }

    #if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void DownloadTextFile(string filename, string content);
    #endif

    public string DownloadMap(int slot)
    {
        string path = GetSlotPath(slot);
        if (!File.Exists(path))
        {
            Debug.LogWarning("No save found at slot " + slot);
            return "No map saved in slot " + slot;
        }

        string json     = File.ReadAllText(path);
        string filename = UserSession.Username + "-SLOT" + slot + ".json";

    #if UNITY_WEBGL && !UNITY_EDITOR
            DownloadTextFile(filename, json);
            return "Map downloaded";
    #elif UNITY_IOS && !UNITY_EDITOR
            GUIUtility.systemCopyBuffer = json;
            Debug.Log("Map JSON copied to clipboard");
            return "Map copied to clipboard";
    #else
        string dest = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop),
            filename
        );
        File.WriteAllText(dest, json);
        Debug.Log("Map saved to Desktop: " + dest);
        return "Map saved to Desktop";
    #endif
    }

}

[Serializable]
public class GameObjectData
{
    public string name;
    public string prefabName;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public bool IsValid => !string.IsNullOrEmpty(prefabName);

    public GameObjectData(GameObject gameObject)
    {
        PlaceableObject obj = gameObject.GetComponent<PlaceableObject>();
        if (obj == null)
        {
            Debug.LogWarning("No PlaceableObject on: " + gameObject.name);
            return;
        }

        name = gameObject.name;
        prefabName = obj.prefabName;
        position = gameObject.transform.position;
        rotation = gameObject.transform.rotation;
        scale = gameObject.transform.localScale;
    }
}

[Serializable]
public class Environment
{
    public string Name;
    public string SavedAt;
    public int ObjectCount;
    public GameObjectData[] Items;

    public Environment(GameObjectData[] items, string mapName = "Untitled")
    {
        Name = string.IsNullOrWhiteSpace(mapName) ? "Untitled" : mapName;
        SavedAt = DateTime.Now.ToString("MM/dd/yyyy HH:mm");
        Items = items;
        ObjectCount = BuildingSystem.ObjectCount;
    }
}
