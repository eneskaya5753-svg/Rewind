using EcoGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>Start screen shown over the paused game until the player presses Start (or Enter); then the intro cinematic.</summary>
    public class TitleScreen : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Button btnStart;
        [SerializeField] private Button btnQuit;
        [SerializeField] private CinematicPlayer cinematic;

        public static bool IsOpen { get; private set; }

        private void Start()
        {
            IsOpen = true;
            gameManager.PushModal();
            btnStart.onClick.AddListener(Begin);
            if (btnQuit != null) btnQuit.onClick.AddListener(Quit);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (IsOpen && keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame))
                Begin();
        }

        private void Begin()
        {
            if (!IsOpen) return;
            IsOpen = false;
            gameObject.SetActive(false);
            if (cinematic != null) cinematic.Play();
            gameManager.PopModal();
        }

        private void OnDestroy() => IsOpen = false;
    }
}
