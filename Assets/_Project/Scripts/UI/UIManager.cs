using System.Collections.Generic;
using System.Text;
using EcoGame.Animals;
using EcoGame.Building;
using EcoGame.Core;
using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Player;
using EcoGame.Utils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>Screen-space HUD. Polls game systems and shows messages and the pickup feed from Notifier.</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private ObjectiveManager objectiveManager;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private PlayerInteractor interactor;

        [Header("Metrics")]
        [SerializeField] private RectTransform barPollutionFill;
        [SerializeField] private RectTransform barTemperatureFill;
        [SerializeField] private RectTransform barWaterPollutionFill;
        [SerializeField] private Text txtPollution;
        [SerializeField] private Text txtTemperature;
        [SerializeField] private Text txtWaterPollution;

        [Header("Inventory")]
        [SerializeField] private RectTransform barBagFill;
        [SerializeField] private Text txtBag;
        [SerializeField] private Text[] trashCounts;
        [SerializeField] private Text[] bagCounts;
        [SerializeField] private RectTransform barWaterFill;
        [SerializeField] private Text txtWater;

        [Header("Info")]
        [SerializeField] private GameObject panelInfo;
        [SerializeField] private Text txtInfoTitle;
        [SerializeField] private Text txtInfoStatus;
        [SerializeField] private GameObject infoBar;
        [SerializeField] private RectTransform infoBarFill;
        [Tooltip("Fills while E is held for filling / watering.")]
        [SerializeField] private GameObject holdBar;
        [SerializeField] private RectTransform holdBarFill;
        [SerializeField] private GameObject panelPrompt;
        [SerializeField] private Text txtInteractPrompt;

        [Header("Messages")]
        [SerializeField] private Text txtMessage;
        [SerializeField] private Text txtObjective;
        [SerializeField] private GameObject crosshair;
        [SerializeField] private Text[] feedLines;
        [SerializeField] private GameObject[] feedFrames;
        [SerializeField] private float messageDuration = 3f;
        [SerializeField] private float feedDuration = 4f;

        [Header("Panels")]
        [Tooltip("Bag contents window, toggled with Tab.")]
        [SerializeField] private GameObject panelBag;
        [Tooltip("Key list (bottom right), toggled with T. Hidden at start.")]
        [SerializeField] private GameObject panelControls;
        [Tooltip("Small \"[T] Tuşlar\" hint shown bottom right while the key list is closed.")]
        [SerializeField] private GameObject controlsHint;
        [SerializeField] private Text txtBagItems;
        [SerializeField] private GameObject panelWin;
        [SerializeField] private GameObject panelLose;
        [SerializeField] private GameObject panelPause;
        [SerializeField] private Button btnRestartWin;
        [SerializeField] private Button btnRestartLose;
        [SerializeField] private Button btnContinueWin;
        [SerializeField] private Button btnContinueLose;
        [Tooltip("Pause menu (Esc): continue, or back to the title screen.")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnMainMenu;

        private readonly StringBuilder _builder = new();
        private readonly List<(string text, float time)> _feed = new();
        private float _messageTimer;
        private bool _hiddenByKey;

        private void Awake()
        {
            btnRestartWin.onClick.AddListener(gameManager.Restart);
            btnRestartLose.onClick.AddListener(gameManager.Restart);
            if (btnContinueWin != null) btnContinueWin.onClick.AddListener(gameManager.ContinueAfterEnd);
            if (btnContinueLose != null) btnContinueLose.onClick.AddListener(gameManager.ContinueAfterEnd);
            if (btnResume != null) btnResume.onClick.AddListener(gameManager.Resume);
            if (btnMainMenu != null) btnMainMenu.onClick.AddListener(gameManager.Restart); // reloading shows the title screen
            ShowState(GameState.Playing);
            txtMessage.text = "";
            RefreshFeed();
        }

        private void OnEnable()
        {
            Notifier.OnMessage += ShowMessage;
            Notifier.OnFeed += AddFeed;
        }

        private void OnDisable()
        {
            Notifier.OnMessage -= ShowMessage;
            Notifier.OnFeed -= AddFeed;
        }

        private void Update()
        {
            SetBar(barPollutionFill, environmentManager.Pollution / 100f);
            SetBar(barTemperatureFill, environmentManager.Temperature / 100f);
            SetBar(barWaterPollutionFill, environmentManager.WaterPollution / 100f);
            txtPollution.text = $"Hava Kirliliği  %{Mathf.RoundToInt(environmentManager.Pollution)}";
            txtTemperature.text = $"Sıcaklık  %{Mathf.RoundToInt(environmentManager.Temperature)}";
            txtWaterPollution.text = $"Su Kirliliği  %{Mathf.RoundToInt(environmentManager.WaterPollution)}";

            bool locked = Cursor.lockState == CursorLockMode.Locked;
            if (crosshair.activeSelf != locked) crosshair.SetActive(locked);

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.tabKey.wasPressedThisFrame && Time.timeScale > 0f)
                panelBag.SetActive(!panelBag.activeSelf);

            // The bag window runs the full height on the left, so the pickup feed steps aside while it is open.
            if (feedFrames.Length > 0)
            {
                var feedRoot = feedFrames[0].transform.parent.gameObject;
                if (feedRoot.activeSelf == panelBag.activeSelf) feedRoot.SetActive(!panelBag.activeSelf);
            }

            if (panelControls != null && keyboard != null && keyboard.tKey.wasPressedThisFrame && Time.timeScale > 0f)
                panelControls.SetActive(!panelControls.activeSelf);
            if (controlsHint != null && panelControls != null && controlsHint.activeSelf == panelControls.activeSelf)
                controlsHint.SetActive(!panelControls.activeSelf);

            // H hides the whole HUD (for screenshots); only drawing stops, the HUD keeps updating.
            if (keyboard != null && keyboard.hKey.wasPressedThisFrame) _hiddenByKey = !_hiddenByKey;

            // The HUD also steps aside while an animal is held, so the animal has the screen to itself.
            var canvas = GetComponent<Canvas>();
            bool show = !_hiddenByKey && AnimalPickup.Held == null;
            if (canvas.enabled != show) canvas.enabled = show;

            UpdateInventory();
            UpdateInfo();
            UpdateObjective();
            UpdateTimers();
        }

        public void ShowState(GameState state)
        {
            panelPause.SetActive(state == GameState.Paused);
            panelWin.SetActive(state == GameState.Won);
            panelLose.SetActive(state == GameState.Lost);
        }

        private void UpdateInventory()
        {
            int capacity = inventory.Settings.bagCapacity;
            SetBar(barBagFill, inventory.BagTotal / (float)capacity);
            txtBag.text = $"Çanta  {inventory.BagTotal}/{capacity}";
            foreach (var type in TrashTypeExtensions.All)
            {
                // The per-type count labels were removed from the HUD; skip any that are gone.
                int i = (int)type;
                if (i < trashCounts.Length && trashCounts[i] != null)
                    trashCounts[i].text = $"{type.DisplayName()} <b>{inventory.GetBag(type)}</b>";
                if (i < bagCounts.Length && bagCounts[i] != null)
                    bagCounts[i].text = $"{type.DisplayName()} <b>{inventory.GetRaw(type)}</b>";
            }

            if (panelBag.activeSelf && txtBagItems != null) txtBagItems.text = BagItemsText();

            SetBar(barWaterFill, inventory.Water / (float)inventory.Settings.waterCapacity);
            txtWater.text = $"Su kabı  {inventory.Water}/{inventory.Settings.waterCapacity}";
        }

        /// <summary>"Kahve bardağı x3   Kola kutusu x2 ..." grouped by name, colored by type.</summary>
        private string BagItemsText()
        {
            _builder.Clear();
            var counted = new List<(TrashType type, string name, int count)>();
            foreach (var (type, model) in inventory.BagItems)
            {
                string name = TrashNames.Display(model, type);
                int index = counted.FindIndex(c => c.name == name);
                if (index >= 0) counted[index] = (type, name, counted[index].count + 1);
                else counted.Add((type, name, 1));
            }

            if (counted.Count == 0) return "Çantada çöp yok.";
            foreach (var (type, name, count) in counted)
                _builder.Append("<color=").Append(type.TextColorHex()).Append(">●</color> ").Append(name)
                    .Append(" x").Append(count).Append("     ");
            _builder.Append("\n<size=16>[Q] çöpleri yere boşalt</size>");
            return _builder.ToString();
        }

        private void UpdateInfo()
        {
            var inspected = interactor.Inspected;
            panelInfo.SetActive(inspected != null);
            if (inspected != null)
            {
                txtInfoTitle.text = inspected.Title;
                txtInfoStatus.text = inspected.GetStatus();
                float progress = inspected.Progress;
                infoBar.SetActive(progress >= 0f);
                if (progress >= 0f) SetBar(infoBarFill, progress);
            }

            // E target first; otherwise a wild animal in reach shows "[F]" (our own pet shows nothing).
            var current = interactor.Current;
            var animal = interactor.NearestAnimal;
            bool petHint = current == null && animal != null && !animal.IsPet && AnimalPickup.Held == null;
            panelPrompt.SetActive(current != null || petHint);
            if (current != null)
            {
                string prompt = current.GetPrompt(interactor);
                bool hold = current.GetHoldSound(interactor).HasValue;
                txtInteractPrompt.text = string.IsNullOrEmpty(prompt) ? "[E]"
                    : hold ? $"[E basılı tut]  {prompt}" : $"[E]  {prompt}";
            }
            else if (petHint) txtInteractPrompt.text = "[F]";

            float holdProgress = interactor.HoldProgress;
            if (holdBar != null && holdBar.activeSelf != holdProgress >= 0f) holdBar.SetActive(holdProgress >= 0f);
            if (holdBar != null && holdProgress >= 0f) SetBar(holdBarFill, holdProgress);
        }

        private void UpdateObjective()
        {
            if (objectiveManager.RecoveryStarted)
            {
                txtObjective.text = "Doğa iyileşiyor...";
                return;
            }

            txtObjective.text = $"Yapılar {objectiveManager.CompletedBuildings}/{objectiveManager.BuildingCount}"
                + $"   •   Bahçe {(objectiveManager.GardenDone ? "gübrelendi" : "bekliyor")}"
                + $"   •   Yerdeki çöp {objectiveManager.TrashRemaining}";
        }

        private void UpdateTimers()
        {
            if (_messageTimer > 0f)
            {
                _messageTimer -= Time.unscaledDeltaTime;
                if (_messageTimer <= 0f) txtMessage.text = "";
            }

            int before = _feed.Count;
            _feed.RemoveAll(entry => Time.unscaledTime - entry.time > feedDuration);
            if (_feed.Count != before) RefreshFeed();
        }

        private void ShowMessage(string message)
        {
            txtMessage.text = message;
            _messageTimer = messageDuration;
        }

        private void AddFeed(string line)
        {
            _feed.Add((line, Time.unscaledTime));
            if (_feed.Count > feedLines.Length) _feed.RemoveAt(0);
            RefreshFeed();
        }

        private void RefreshFeed()
        {
            for (int i = 0; i < feedLines.Length; i++)
            {
                bool used = i < _feed.Count;
                feedLines[i].text = used ? _feed[i].text : "";
                if (i < feedFrames.Length) feedFrames[i].SetActive(used);
            }
        }

        private static void SetBar(RectTransform fill, float value) =>
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }
}
