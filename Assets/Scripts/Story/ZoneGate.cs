using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class ZoneGate : MonoBehaviour
{
    public string gateId = "Zone2";
    public string zoneLabel = "Zona 2";
    public int price = 150;
    public string targetSceneName = "";
    public GameObject barrier;
    public GameObject lockedSign;
    public GameObject unlockedSign;
    public float transitionDelay = 0.9f;

    private bool playerInside;
    private int lastPurchaseFrame = -1;

    private void Awake()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null) col.isTrigger = true;
        RefreshVisuals();
    }

    private void Start()
    {
        RefreshVisuals();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = true;
        UpdatePrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInside = false;
        UIManager.instance?.ClearInteractionPrompt("ZoneGate_" + gateId);
    }

    private void Update()
    {
        if (!playerInside || IsUnlocked()) return;
        UpdatePrompt();

        if (StoryProgress.IsGameplayInputBlocked) return;
        if (Input.GetKeyDown(KeyCode.E)) TryBuy();
    }

    private bool IsUnlocked()
    {
        if (gateId == "Zone2") return StoryProgress.zone2GateUnlocked;
        if (gateId == "Zone3") return StoryProgress.zone3GateUnlocked;
        return false;
    }

    private bool HasPrerequisite()
    {
        if (gateId == "Zone2") return StoryProgress.tutorialDone && StoryProgress.zone1FollowUpDone;
        if (gateId == "Zone3") return StoryProgress.zone2GateUnlocked && StoryProgress.zone2IntroDone;
        return false;
    }

    private void UpdatePrompt()
    {
        if (IsUnlocked())
        {
            UIManager.instance?.ClearInteractionPrompt("ZoneGate_" + gateId);
            return;
        }

        ScoreManager score = ScoreManager.instance;
        int balance = score != null ? score.currentPoin : 0;
        string message = "[E] Buka akses " + zoneLabel + " — " + price + " Poin";
        if (balance < price)
            message = "Kurang " + (price - balance) + " poin untuk akses " + zoneLabel + " (harga " + price + ")";
        else if (!HasPrerequisite())
            message = "Selesaikan tugas sebelumnya dulu untuk membuka " + zoneLabel;

        UIManager.instance?.SetInteractionPrompt("ZoneGate_" + gateId, message);
    }

    public void TryBuy()
    {
        if (IsUnlocked() || Time.frameCount == lastPurchaseFrame) return;
        lastPurchaseFrame = Time.frameCount;

        if (!HasPrerequisite())
        {
            UIManager.instance?.ShowFeedback("Selesaikan progresi sebelumnya sebelum membeli akses " + zoneLabel + ".",
                new Color(1f, 0.45f, 0.4f));
            return;
        }

        ScoreManager score = ScoreManager.instance;
        if (score == null || score.currentPoin < price)
        {
            int balance = score != null ? score.currentPoin : 0;
            UIManager.instance?.ShowFeedback("Poin belum cukup! Kurang " + (price - balance) + " poin.",
                new Color(1f, 0.45f, 0.4f));
            return;
        }

        if (!score.TrySpend(price)) return;

        StoryFlow.Instance?.UnlockGate(gateId);
        RefreshVisuals();
        UIManager.instance?.ClearInteractionPrompt("ZoneGate_" + gateId);
        UIManager.instance?.ShowFeedback("Akses " + zoneLabel + " berhasil dibuka!", new Color(0.55f, 1f, 0.62f), 3f);
        Debug.Log("[ZoneGate] " + gateId + " unlocked. Poin tersisa: " + score.currentPoin);

        if (!string.IsNullOrEmpty(targetSceneName)) StartCoroutine(TransitionAfterDelay());
    }

    private void RefreshVisuals()
    {
        bool unlocked = IsUnlocked();
        if (barrier != null) barrier.SetActive(!unlocked);
        if (lockedSign != null) lockedSign.SetActive(!unlocked);
        if (unlockedSign != null) unlockedSign.SetActive(unlocked);
    }

    private IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSeconds(transitionDelay);
        UIManager.instance?.ClearInteractionPrompt("ZoneGate_" + gateId);
        GameSession.SaveFromCurrentScene();
        SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
    }
}
