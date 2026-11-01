using UnityEngine;

namespace EcoGame.Utils
{
    public static class MathUtil
    {
        public static float FlatDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }
    }
}
