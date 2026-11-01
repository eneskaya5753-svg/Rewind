using System.Collections;
using System.Collections.Generic;
using EcoGame.Data;
using EcoGame.Environment;
using EcoGame.Interaction;
using EcoGame.Player;
using EcoGame.Quiz;
using EcoGame.Utils;
using UnityEngine;

namespace EcoGame.Machines
{
    /// <summary>
    /// Recycling station: trash is dumped onto the input belt, the red button runs the machine with one question
    /// for everything inside. Correct: colored bags come out on the output belt. Wrong: the trash comes out as it was.
    /// The ports (input, button, output) are RecyclerPort children.
    /// </summary>
    public class RecyclingMachine : MonoBehaviour, IInspectable
    {
        [SerializeField] private QuizManager quizManager;
        [SerializeField] private TrashTypeData[] trashTypes;
        [SerializeField] private GameObject bagPrefab;
        [SerializeField] private Transform beltStart;
        [SerializeField] private Transform machineIn;
        [SerializeField] private Transform machineOut;
        [SerializeField] private Transform beltEnd;
        [SerializeField] private string colorProperty = "_BaseColor";
        [Header("Sound")]
        [Tooltip("Plays while the machine packs; the quiet start of the clip is skipped and it stops when packing ends.")]
        [SerializeField] private AudioClip runSound;
        [SerializeField] private float runSoundStart = 3f;
        [SerializeField] private float runSoundExtra = 1.5f;

        [Tooltip("Red while there is nothing to run, green once trash is loaded.")]
        [SerializeField] private Renderer buttonRenderer;
        [SerializeField] private Color buttonIdleColor = new(0.85f, 0.15f, 0.12f);
        [SerializeField] private Color buttonReadyColor = new(0.2f, 0.85f, 0.25f);

        private MaterialPropertyBlock _buttonBlock;
        private bool? _buttonReady;
        private AudioSource _runAudio;

        /// <summary>One pile per trash type: a bag (correct answer) or the raw trash (wrong answer).</summary>
        private class OutputItem
        {
            public TrashType Type;
            public bool IsBag;
            public int Amount;
            public GameObject Visual;
        }

        private readonly int[] _loaded = new int[TrashTypeExtensions.Count];
        private readonly List<OutputItem> _output = new();

        public bool IsBusy { get; private set; }
        public int OutputCount => _output.Count;
        public bool OutputHasBags => _output.Count > 0 && _output[0].IsBag;

        public int LoadedTotal
        {
            get
            {
                int total = 0;
                foreach (var count in _loaded) total += count;
                return total;
            }
        }

        public Vector3 Position => transform.position;
        public string Title => "Geri Dönüşüm Makinesi";
        public float Progress => -1f;

        private EnvironmentSettings Settings => EnvironmentManager.Instance.Settings;

        public string GetStatus()
        {
            if (IsBusy) return "Çalışıyor...";
            string inside = LoadedTotal > 0
                ? $"İçeride {LoadedTotal} çöp var. Kırmızı butona bas, tek soruyu bil, hepsi torbalansın."
                : "Çöpleri giriş bandına boşalt, sonra kırmızı butona bas.";
            string output = "";
            foreach (var item in _output)
                output += $"\n<color={item.Type.TextColorHex()}>{item.Type.DisplayName()} {(item.IsBag ? "torbası" : "çöp")} x{item.Amount}</color> çıkış bandında";
            return inside + output;
        }

        private void OnEnable() => Inspectables.All.Add(this);
        private void OnDisable() => Inspectables.All.Remove(this);

        private void Update()
        {
            bool ready = !IsBusy && LoadedTotal > 0;
            if (buttonRenderer == null || _buttonReady == ready) return;
            _buttonReady = ready;
            _buttonBlock ??= new MaterialPropertyBlock();
            buttonRenderer.GetPropertyBlock(_buttonBlock);
            _buttonBlock.SetTexture("_BaseMap", Texture2D.whiteTexture);
            _buttonBlock.SetColor(colorProperty, ready ? buttonReadyColor : buttonIdleColor);
            buttonRenderer.SetPropertyBlock(_buttonBlock);
        }

        public void Load(PlayerInteractor interactor)
        {
            var items = new List<TrashType>();
            foreach (var type in TrashTypeExtensions.All)
            {
                int amount = interactor.Inventory.TakeFromBag(type, int.MaxValue);
                _loaded[(int)type] += amount;
                for (int i = 0; i < amount; i++) items.Add(type);
            }

            if (items.Count == 0) return;
            Notifier.Feed($"{items.Count} çöp banda boşaltıldı");
            StartCoroutine(SendToMachine(interactor.transform.position + Vector3.up, items));
        }

        public void Run()
        {
            if (IsBusy || LoadedTotal == 0) return;
            IsBusy = true;
            quizManager.Ask($"Makinede {LoadedTotal} çöp var. Tek soru, hepsi için!",
                correct => StartCoroutine(Produce(correct)),
                () => IsBusy = false); // closed without answering: trash stays loaded, run again any time
        }

        public void Collect(PlayerInteractor interactor)
        {
            var inventory = interactor.Inventory;
            for (int i = 0; i < _output.Count && inventory.FreeSpace > 0; i++)
            {
                var item = _output[i];
                int taken = Mathf.Min(item.Amount, inventory.FreeSpace);
                if (item.IsBag) inventory.AddRaw(item.Type, taken);
                else inventory.ReturnToBag(item.Type, taken);
                item.Amount -= taken;

                Notifier.Feed(item.IsBag
                    ? $"<color={item.Type.TextColorHex()}>+{taken} {item.Type.DisplayName()} torbası</color>"
                    : $"+{taken} {item.Type.DisplayName()} çöp");
                Audio.AudioManager.Play(Audio.Sfx.Loot);
                if (item.Amount > 0) continue;

                if (item.Visual != null) ItemMover.FlyTo(item.Visual, interactor.transform, Settings.itemFlySpeed);
                _output.RemoveAt(i--);
            }

            if (_output.Count > 0) Notifier.Show("Çanta dolu! Kalanlar bantta bekliyor.");
        }

        private IEnumerator SendToMachine(Vector3 from, List<TrashType> items)
        {
            foreach (var type in items)
            {
                var visual = SpawnTrashVisual(type, from);
                ItemMover.Move(visual, Settings.beltSpeed * 2f, true, null, beltStart.position, machineIn.position);
                yield return new WaitForSeconds(Settings.itemSpawnInterval);
            }
        }

        /// <summary>Machine hum for the packing time: starts past the clip's intro, fades out at the end.</summary>
        private void PlayRunSound()
        {
            if (runSound == null) return;
            if (_runAudio == null)
            {
                _runAudio = gameObject.AddComponent<AudioSource>();
                _runAudio.clip = runSound;
                _runAudio.spatialBlend = 1f;
                _runAudio.rolloffMode = AudioRolloffMode.Linear;
                _runAudio.minDistance = 3f;
                _runAudio.maxDistance = 25f;
                _runAudio.playOnAwake = false;
            }
            StartCoroutine(RunSound(Settings.machineRunTime + runSoundExtra));
        }

        private IEnumerator RunSound(float duration)
        {
            const float FADE = 0.5f;
            const float RUN_VOLUME = 0.35f;
            _runAudio.time = Mathf.Min(runSoundStart, runSound.length - duration - 0.1f);
            _runAudio.volume = RUN_VOLUME;
            _runAudio.Play();
            yield return new WaitForSeconds(Mathf.Max(0f, duration - FADE));
            for (float t = 0f; t < FADE; t += Time.deltaTime)
            {
                _runAudio.volume = RUN_VOLUME * (1f - t / FADE);
                yield return null;
            }
            _runAudio.Stop();
        }

        private IEnumerator Produce(bool correct)
        {
            Notifier.Show(correct
                ? "Doğru! Makine çöpleri renkli torbalara paketliyor."
                : "Yanlış! Makine çöpleri olduğu gibi geri veriyor.");
            PlayRunSound();
            yield return new WaitForSeconds(Settings.machineRunTime);

            foreach (var type in TrashTypeExtensions.All)
            {
                int amount = _loaded[(int)type];
                if (amount == 0) continue;
                _loaded[(int)type] = 0;

                var visual = correct ? SpawnBagVisual(type, machineOut.position) : SpawnTrashVisual(type, machineOut.position);
                ItemMover.Move(visual, Settings.beltSpeed, false, null, beltEnd.position, PilePosition(_output.Count));
                _output.Add(new OutputItem { Type = type, IsBag = correct, Amount = amount, Visual = visual });
                yield return new WaitForSeconds(Settings.itemSpawnInterval * 4f);
            }

            IsBusy = false;
        }

        private Vector3 PilePosition(int index)
        {
            const float SPACING = 0.7f;
            var local = new Vector3((index % 2 - 0.5f) * SPACING, 0f, -(index / 2) * SPACING);
            return beltEnd.position + beltEnd.rotation * local;
        }

        private GameObject SpawnTrashVisual(TrashType type, Vector3 position)
        {
            var source = trashTypes[(int)type].prefab.transform.Find("Visual");
            var visual = Instantiate(source.gameObject, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            VisualVariants.PickRandomChild(visual.transform);
            return visual;
        }

        private GameObject SpawnBagVisual(TrashType type, Vector3 position)
        {
            var visual = Instantiate(bagPrefab, position, Quaternion.identity);
            ColorUtility.TryParseHtmlString(type.BagColorHex(), out var color);
            var block = new MaterialPropertyBlock();
            block.SetColor(colorProperty, color);
            foreach (var bagRenderer in visual.GetComponentsInChildren<Renderer>()) bagRenderer.SetPropertyBlock(block);
            return visual;
        }
    }
}
