using System;
using System.Collections.Generic;
using EcoGame.Core;
using EcoGame.Data;
using EcoGame.UI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace EcoGame.Quiz
{
    /// <summary>Random, non-repeating questions; pauses the game while the panel is open.</summary>
    public class QuizManager : MonoBehaviour
    {
        [SerializeField] private QuizDatabase database;
        [SerializeField] private QuizPanel quizPanel;
        [SerializeField] private GameManager gameManager;

        private readonly List<QuizQuestionData> _pool = new();

        /// <summary>Shows a question; onCancelled runs instead of onAnswered if the player closes it (E or the X).</summary>
        public void Ask(string header, Action<bool> onAnswered, Action onCancelled = null)
        {
            var question = Next();
            if (question == null)
            {
                onAnswered(true);
                return;
            }

            gameManager.PushModal();
            quizPanel.Show(question, header, answer =>
            {
                gameManager.PopModal();
                if (answer.HasValue)
                {
                    onAnswered(answer.Value);
                    return;
                }
                _pool.Add(question); // not answered: it can come up again
                onCancelled?.Invoke();
            });
        }

        private QuizQuestionData Next()
        {
            if (_pool.Count == 0) _pool.AddRange(database.questions);
            if (_pool.Count == 0) return null;
            int index = Random.Range(0, _pool.Count);
            var question = _pool[index];
            _pool.RemoveAt(index);
            return question;
        }
    }
}
