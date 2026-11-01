using EcoGame.Environment;
using EcoGame.Player;
using EcoGame.UI;
using UnityEngine;

namespace EcoGame.Audio
{
    public enum Sfx { Loot, Build, Water, FillWater, QuizCorrect, Win, Lose }

    /// <summary>
    /// All game audio. One-shots go through <see cref="Play"/>; ambience blends between bright (clean air, birds)
    /// and dark (polluted) loops, with the river/sea always underneath and footsteps while the player walks.
    /// The title screen plays the bright loop, the cinematic plays its own music and silences the rest.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const float FADE_SPEED = 0.6f;
        private const float FADE_IN_SPEED = 0.12f;

        [Header("One-shots")]
        [SerializeField] private AudioClip loot;
        [SerializeField] private AudioClip build;
        [SerializeField] private AudioClip water;
        [SerializeField] private AudioClip fillWater;
        [SerializeField] private AudioClip quizCorrect;
        [SerializeField] private AudioClip win;
        [SerializeField] private AudioClip lose;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;
        [Tooltip("Hold actions last as long as their sound, but never longer than this (the watering sound is 22 s).")]
        [SerializeField] private float maxHoldSeconds = 4f;

        [Header("Loops")]
        [SerializeField] private AudioClip brightAmbient;
        [SerializeField] private AudioClip darkAmbient;
        [SerializeField] private AudioClip birds;
        [SerializeField] private AudioClip riverSea;
        [SerializeField] private AudioClip footsteps;
        [SerializeField] private AudioClip cinematicMusic;
        [SerializeField, Range(0f, 1f)] private float ambientVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float birdsVolume = 0.35f;
        [SerializeField, Range(0f, 1f)] private float riverVolume = 0.25f;
        [SerializeField, Range(0f, 1f)] private float footstepsVolume = 0.15f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.7f;

        [Tooltip("Air pollution where the mood starts turning dark, and where it is fully dark.")]
        [SerializeField] private Vector2 darkRange = new(30f, 65f);

        private static AudioManager _instance;
        private AudioSource _sfx, _hold, _bright, _dark, _birds, _river, _steps, _music;
        private CharacterController _player;
        private bool _musicStarted;

        public static bool CinematicPlaying { get; set; }
        /// <summary>Length of the cinematic and seconds left in it; the music is centered in the video.</summary>
        public static float CinematicLength { get; set; }
        public static float CinematicTimeLeft { get; set; }

        /// <summary>Starts the sound of a hold action and returns how long the hold takes.</summary>
        public static float StartHold(Sfx sfx)
        {
            if (_instance == null) return 1f;
            var clip = _instance.ClipFor(sfx);
            if (clip == null) return 1f;
            _instance._hold.clip = clip;
            _instance._hold.volume = _instance.sfxVolume;
            _instance._hold.Play();
            return Mathf.Min(clip.length, _instance.maxHoldSeconds);
        }

        public static void StopHold()
        {
            if (_instance != null) _instance._hold.Stop();
        }

        public static void Play(Sfx sfx)
        {
            if (_instance == null) return;
            var clip = _instance.ClipFor(sfx);
            if (clip != null) _instance._sfx.PlayOneShot(clip, _instance.sfxVolume);
        }

        private AudioClip ClipFor(Sfx sfx) => sfx switch
        {
            Sfx.Loot => loot,
            Sfx.Build => build,
            Sfx.Water => water,
            Sfx.FillWater => fillWater,
            Sfx.QuizCorrect => quizCorrect,
            Sfx.Win => win,
            _ => lose
        };

        private void Awake()
        {
            _instance = this;
            _sfx = gameObject.AddComponent<AudioSource>();
            _hold = gameObject.AddComponent<AudioSource>();
            _hold.playOnAwake = false;
            _bright = Loop(brightAmbient);
            _dark = Loop(darkAmbient);
            _birds = Loop(birds);
            _river = Loop(riverSea);
            _steps = Loop(footsteps);
            _music = gameObject.AddComponent<AudioSource>();
            _music.clip = cinematicMusic;
            _music.playOnAwake = false;
        }

        private AudioSource Loop(AudioClip clip)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = 0f;
            source.playOnAwake = false;
            if (clip != null) source.Play();
            return source;
        }

        private void Update()
        {
            float bright, dark, bird, river, steps, music;
            if (CinematicPlaying)
            {
                bright = dark = bird = river = steps = 0f;
                music = musicVolume;
                // Played once, centered in the video (equal silence before and after).
                if (!_musicStarted && cinematicMusic != null && CinematicTimeLeft <= (CinematicLength + cinematicMusic.length) * 0.5f)
                {
                    _musicStarted = true;
                    _music.time = 0f;
                    _music.volume = musicVolume;
                    _music.Play();
                }
            }
            else if (TitleScreen.IsOpen)
            {
                bright = ambientVolume;
                dark = bird = river = steps = music = 0f;
            }
            else
            {
                float pollution = EnvironmentManager.Instance != null ? EnvironmentManager.Instance.Pollution : 0f;
                float gloom = Mathf.InverseLerp(darkRange.x, darkRange.y, pollution);
                bright = ambientVolume * (1f - gloom);
                dark = ambientVolume * gloom;
                bird = birdsVolume * (1f - gloom);
                river = riverVolume;
                steps = IsWalking() ? footstepsVolume : 0f;
                music = 0f;
            }

            Fade(_bright, bright);
            Fade(_dark, dark);
            Fade(_birds, bird);
            Fade(_river, river);
            Fade(_music, music);
            if (!CinematicPlaying && _music.isPlaying && _music.volume <= 0f) _music.Stop();
            _steps.volume = steps; // footsteps start and stop at once
        }

        private bool IsWalking()
        {
            if (Time.timeScale == 0f) return false;
            if (_player == null)
            {
                if (PlayerInteractor.Instance == null) return false;
                _player = PlayerInteractor.Instance.GetComponent<CharacterController>();
                if (_player == null) return false;
            }

            Vector3 velocity = _player.velocity;
            velocity.y = 0f;
            return _player.isGrounded && velocity.sqrMagnitude > 0.5f;
        }

        // Sounds come in slowly (after the cinematic the world fades in gently) and go out a bit faster.
        private static void Fade(AudioSource source, float target)
        {
            float speed = target > source.volume ? FADE_IN_SPEED : FADE_SPEED;
            source.volume = Mathf.MoveTowards(source.volume, target, speed * Time.unscaledDeltaTime);
        }
    }
}
