using UnityEngine;

namespace EcoGame.Data
{
    [CreateAssetMenu(menuName = "EcoGame/Quiz Question")]
    public class QuizQuestionData : ScriptableObject
    {
        [TextArea] public string question;
        public string[] answers = new string[4];
        public int correctIndex;
        [TextArea] public string infoNote;
    }
}
