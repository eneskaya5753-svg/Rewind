using EcoGame.Environment;
using EcoGame.Interaction;
using UnityEngine;

namespace EcoGame.Machines
{
    public class Generator : MonoBehaviour, IPollutionSource, IInspectable
    {
        private const float HUM_VOLUME = 0.25f;

        [SerializeField] private string displayName = "Jeneratör";
        [SerializeField] private string powers = "";
        [SerializeField] private string shutdownHint = "";
        [SerializeField] private Transform smokeAnchor;
        [SerializeField] private int sitesToShutdown = 1;
        [Tooltip("Engine hum, looped in 3D while the generator runs.")]
        [SerializeField] private AudioClip loopSound;

        private ParticleSystem _smoke;
        private AudioSource _audio;
        private bool _running = true;
        private int _completedSites;

        public bool IsRunning => _running;
        public bool IsPolluting => _running;
        public float PollutionRate => EnvironmentManager.Instance.Settings.generatorPollutionRate;
        public float TemperatureRate => EnvironmentManager.Instance.Settings.generatorTemperatureRate;
        public float WaterPollutionRate => 0f;

        public Vector3 Position => transform.position;
        public string Title => displayName;
        public float Progress => -1f;

        public string GetStatus() => _running
            ? $"{powers}\nFosil yakıtla çalışıyor: havayı kirletiyor ve ısıtıyor.\n{shutdownHint} ({_completedSites}/{sitesToShutdown})"
            : $"{powers}\nKapatıldı. Artık temiz enerjiyle çalışıyor!";

        private void Awake()
        {
            if (smokeAnchor != null) _smoke = smokeAnchor.GetComponentInChildren<ParticleSystem>();
            _audio = GetComponent<AudioSource>();
            if (_audio == null && loopSound != null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.clip = loopSound;
                _audio.loop = true;
                _audio.spatialBlend = 1f;
                _audio.rolloffMode = AudioRolloffMode.Linear;
                _audio.minDistance = 3f;
                _audio.maxDistance = 30f;
                _audio.volume = 0f;
                _audio.Play();
            }
        }

        // Silent behind the title screen and the cinematic, then fades in with the rest of the world.
        private void Update()
        {
            if (_audio == null) return;
            float target = Audio.AudioManager.CinematicPlaying || UI.TitleScreen.IsOpen ? 0f : HUM_VOLUME;
            _audio.volume = Mathf.MoveTowards(_audio.volume, target, 0.12f * Time.unscaledDeltaTime);
        }

        private void OnEnable()
        {
            EnvironmentManager.Register(this);
            Inspectables.All.Add(this);
        }

        private void OnDisable()
        {
            EnvironmentManager.Unregister(this);
            Inspectables.All.Remove(this);
        }

        /// <summary>A clean-energy site linked to this generator was built; shuts down once all of them are.</summary>
        public void NotifySiteCompleted()
        {
            _completedSites++;
            if (_completedSites >= sitesToShutdown) Shutdown();
        }

        public void Shutdown()
        {
            if (!_running) return;
            _running = false;
            if (_smoke != null) _smoke.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (_audio != null) _audio.Stop();
        }
    }
}
