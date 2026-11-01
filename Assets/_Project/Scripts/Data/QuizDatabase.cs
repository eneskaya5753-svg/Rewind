using System.Collections.Generic;
using UnityEngine;

namespace EcoGame.Data
{
    [CreateAssetMenu(menuName = "EcoGame/Quiz Database")]
    public class QuizDatabase : ScriptableObject
    {
        public List<QuizQuestionData> questions = new();
    }
}
