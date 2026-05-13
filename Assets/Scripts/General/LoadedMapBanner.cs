using TMPro;
using UnityEngine;

public class LoadedMapBanner : MonoBehaviour
{
    [SerializeField] FirebaseHandler firebaseHandler;
    [SerializeField] TextMeshProUGUI bannerText;
    [SerializeField] string emptyLabel = "Untitled Map";
    [SerializeField] string prefix = "Loaded: ";
    [SerializeField] float refreshInterval = 0.25f;

    float timer;
    string lastShown = null;

    void Update()
    {
        timer += Time.unscaledDeltaTime;
        if (timer < refreshInterval) return;
        timer = 0f;

        string name = firebaseHandler.CurrentMapName;
        string display = string.IsNullOrEmpty(name) ? emptyLabel : prefix + name;
        if (display == lastShown) return;

        bannerText.text = display;
        lastShown = display;
    }
}
