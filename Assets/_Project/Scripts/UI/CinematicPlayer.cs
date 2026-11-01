using System.Collections;
using EcoGame.Audio;
using EcoGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;

namespace EcoGame.UI
{
    /// <summary>Intro video shown after the title screen. "Geç" (click or E) skips it; the game starts when it ends.</summary>
    public class CinematicPlayer : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage screen;
        [SerializeField] private Button btnSkip;

        [Tooltip("The skip button shows this long at the start (and again briefly when the mouse moves).")]
        [SerializeField] private float skipVisibleSeconds = 4f;
        [SerializeField] private float fadeInSeconds = 2.5f;

        private RenderTexture _target;
        private bool _playing;
        private float _skipTimer;

        /// <summary>Called by the title screen. The game stays paused until the video ends or is skipped.</summary>
        public void Play()
        {
            if (videoPlayer.clip == null) return;
            _playing = true;
            gameObject.SetActive(true);
            gameManager.PushModal();
            AudioManager.CinematicPlaying = true;

            _target = new RenderTexture((int)videoPlayer.clip.width, (int)videoPlayer.clip.height, 0);
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = _target;
            screen.texture = _target;
            videoPlayer.loopPointReached += OnFinished;
            btnSkip.onClick.AddListener(Finish);
            _skipTimer = skipVisibleSeconds;
            AudioManager.CinematicLength = (float)videoPlayer.clip.length;
            AudioManager.CinematicTimeLeft = (float)videoPlayer.clip.length;
            videoPlayer.Play();
        }

        public bool WillPlay => videoPlayer != null && videoPlayer.clip != null;

        private void Update()
        {
            if (!_playing) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.eKey.wasPressedThisFrame)
            {
                Finish();
                return;
            }

            if (videoPlayer.isPlaying)
                AudioManager.CinematicTimeLeft = (float)(videoPlayer.clip.length - videoPlayer.time);

            // E still skips while the button is hidden; moving the mouse brings it back for a moment.
            var mouse = Mouse.current;
            if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 4f) _skipTimer = skipVisibleSeconds;
            _skipTimer -= Time.unscaledDeltaTime;
            bool showSkip = _skipTimer > 0f;
            if (btnSkip.gameObject.activeSelf != showSkip) btnSkip.gameObject.SetActive(showSkip);
        }

        private void OnFinished(VideoPlayer source) => Finish();

        private void Finish()
        {
            if (!_playing) return;
            _playing = false;
            videoPlayer.Stop();
            videoPlayer.loopPointReached -= OnFinished;
            AudioManager.CinematicPlaying = false;
            screen.gameObject.SetActive(false);
            btnSkip.gameObject.SetActive(false);
            if (_target != null) _target.Release();
            gameManager.PopModal();
            StartCoroutine(FadeIn());
        }

        /// <summary>The video ends on black; the world fades in from that black instead of popping in.</summary>
        private IEnumerator FadeIn()
        {
            var black = transform.Find("Img_Black").GetComponent<Graphic>();
            black.raycastTarget = false;
            for (float t = 0f; t < fadeInSeconds; t += Time.unscaledDeltaTime)
            {
                black.color = new Color(0f, 0f, 0f, 1f - t / fadeInSeconds);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
