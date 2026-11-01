using System;
using System.Collections;
using EcoGame.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EcoGame.UI
{
    /// <summary>
    /// One question; the chosen answer lights green (right) or red (wrong) and the panel closes by itself.
    /// The right answer is not revealed on a wrong pick.
    /// E or the X (top-right) closes it without answering.
    /// </summary>
    public class QuizPanel : MonoBehaviour
    {
        [SerializeField] private Text txtHeader;
        [SerializeField] private Text txtQuestion;
        [SerializeField] private Button[] answerButtons;
        [SerializeField] private Text txtFeedback;
        [SerializeField] private Button btnContinue;
        [SerializeField] private Button btnClose;
        [SerializeField] private Color correctColor = new(0.3f, 0.8f, 0.35f);
        [SerializeField] private Color wrongColor = new(0.9f, 0.3f, 0.3f);
        [SerializeField] private float closeDelay = 0.8f;

        private QuizQuestionData _question;
        private Action<bool?> _callback; // null = closed without answering
        private Color _normalColor;
        private bool _correct;
        private bool _answered;
        private int _shownFrame;

        /// <summary>Frame the quiz was closed with E, so the same press does not also hit what is behind it.</summary>
        public static int ClosedFrame { get; private set; } = -1;

        private void Awake()
        {
            _normalColor = answerButtons[0].image.color;
            if (btnClose != null) btnClose.onClick.AddListener(Close);
            for (int i = 0; i < answerButtons.Length; i++)
            {
                int index = i;
                answerButtons[i].onClick.AddListener(() => Answer(index));
            }
        }

        public void Show(QuizQuestionData question, string header, Action<bool?> callback)
        {
            gameObject.SetActive(true);
            _question = question;
            _callback = callback;
            _answered = false;
            _shownFrame = Time.frameCount; // the E press that opened the quiz must not close it
            txtHeader.text = header;
            txtQuestion.text = question.question;

            for (int i = 0; i < answerButtons.Length; i++)
            {
                var button = answerButtons[i];
                button.interactable = true;
                button.image.color = _normalColor;
                button.GetComponentInChildren<Text>().text = $"{(char)('A' + i)})  {question.answers[i]}";
            }

            if (txtFeedback != null) txtFeedback.text = "";
            if (btnContinue != null) btnContinue.gameObject.SetActive(false);
        }

        private void Answer(int index)
        {
            _answered = true;
            _correct = index == _question.correctIndex;
            if (_correct) Audio.AudioManager.Play(Audio.Sfx.QuizCorrect);
            foreach (var button in answerButtons) button.interactable = false;
            answerButtons[index].image.color = _correct ? correctColor : wrongColor;
            StartCoroutine(CloseAfterDelay());
        }

        private IEnumerator CloseAfterDelay()
        {
            // The game is paused while the quiz is open, so wait in real time.
            yield return new WaitForSecondsRealtime(closeDelay);
            gameObject.SetActive(false);
            _callback?.Invoke(_correct);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!_answered && Time.frameCount != _shownFrame && keyboard != null && keyboard.eKey.wasPressedThisFrame) Close();
        }

        private void Close()
        {
            if (_answered) return;
            _answered = true;
            gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;
            _callback?.Invoke(null);
        }
    }
}
