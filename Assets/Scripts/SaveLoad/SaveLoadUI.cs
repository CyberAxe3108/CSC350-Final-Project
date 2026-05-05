using System.Collections;
using TMPro;
using UnityEngine;

public class SaveLoadUI : MonoBehaviour {
    [SerializeField] GameObject saveLoadPanel;
    [SerializeField] FirebaseHandler firebaseHandler;
    // Assign one label per load button in the Inspector (slots 1–4)
    [SerializeField] TextMeshProUGUI[] loadButtonLabels;
    [SerializeField] TextMeshProUGUI feedbackText;

    private int selectedSlot = 0;

    public void SetVisibility(bool state) {
        saveLoadPanel.SetActive(state);
        if (state) RefreshSlotNames();
    }

    public void ToggleVisibility() {
        bool next = !saveLoadPanel.activeSelf;
        saveLoadPanel.SetActive(next);
        if (next) RefreshSlotNames();
    }

    public void Save(int slot) {
        firebaseHandler.SaveEnvironment(slot, "Slot " + slot);
    }

    public void SelectSlot(int slot) => selectedSlot = slot;

    public void Load(int slot) => firebaseHandler.LoadEnvironment(slot);

    public void DownloadSelected()
    {
        if (selectedSlot == 0)
        {
            if (feedbackText != null)
                StartCoroutine(ShowFeedback("No slot selected"));
            return;
        }
        Download(selectedSlot);
    }

    public void Download(int slot)
    {
        string msg = firebaseHandler.DownloadMap(slot);
        if (feedbackText != null)
            StartCoroutine(ShowFeedback(msg));
    }

    private IEnumerator ShowFeedback(string msg, float duration = 2f)
    {
        feedbackText.text = msg;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(duration);
        feedbackText.gameObject.SetActive(false);
    }


    private void RefreshSlotNames() {
        if (loadButtonLabels == null) return;
        for (int i = 0; i < loadButtonLabels.Length; i++) {
            TextMeshProUGUI label = loadButtonLabels[i];
            if (label == null) continue;
            int slot = i + 1;
            firebaseHandler.GetSlotName(slot, name => {
                label.text = string.IsNullOrEmpty(name) ? "Empty" : name;
            });
        }
    }
}
