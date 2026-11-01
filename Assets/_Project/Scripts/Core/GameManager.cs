using System;
using EcoGame.Cameras;
using EcoGame.Environment;
using EcoGame.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EcoGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private EnvironmentManager environmentManager;
        [SerializeField] private ObjectiveManager objectiveManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private CameraModeManager cameraModeManager;

        private int _modalCount;

        public GameState State { get; private set; } = GameState.Playing;
        public bool IsModalOpen => _modalCount > 0;
        public event Action<GameState> OnStateChanged;

        private void Awake()
        {
            Instance = this;
            ApplyTime();
        }

        private void OnEnable()
        {
            environmentManager.OnLimitReached += Lose;
            objectiveManager.OnRecoveryFinished += Win;
        }

        private void OnDisable()
        {
            environmentManager.OnLimitReached -= Lose;
            objectiveManager.OnRecoveryFinished -= Win;
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || IsModalOpen || !keyboard.escapeKey.wasPressedThisFrame) return;

            if (State == GameState.Playing) SetState(GameState.Paused);
            else if (State == GameState.Paused) SetState(GameState.Playing);
        }

        public void PushModal()
        {
            _modalCount++;
            ApplyTime();
        }

        public void PopModal()
        {
            _modalCount = Mathf.Max(0, _modalCount - 1);
            ApplyTime();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>End screen "Devam Et" (won or lost): close it and keep playing in the same world.</summary>
        public void ContinueAfterEnd()
        {
            if (State == GameState.Won || State == GameState.Lost) SetState(GameState.Playing);
        }

        /// <summary>Pause menu "Devam Et".</summary>
        public void Resume()
        {
            if (State == GameState.Paused) SetState(GameState.Playing);
        }

        private void Win() => SetState(GameState.Won);

        private void Lose()
        {
            if (State == GameState.Playing) SetState(GameState.Lost);
        }

        private void SetState(GameState state)
        {
            State = state;
            ApplyTime();
            uiManager.ShowState(state);
            if (state == GameState.Won) Audio.AudioManager.Play(Audio.Sfx.Win);
            else if (state == GameState.Lost) Audio.AudioManager.Play(Audio.Sfx.Lose);
            OnStateChanged?.Invoke(state);
        }

        private void ApplyTime()
        {
            bool frozen = State != GameState.Playing || IsModalOpen;
            Time.timeScale = frozen ? 0f : 1f;
            if (cameraModeManager != null) cameraModeManager.SetCursorFree(frozen);
        }
    }
}
