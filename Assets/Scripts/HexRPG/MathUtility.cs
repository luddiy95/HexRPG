using UnityEngine;

namespace HexRPG
{
    public static class MathUtility
    {
        /// <summary>
        /// �C�ӂ̊p�x(����)��-179 �` 180�ɕϊ�����
        /// </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static int GetIntegerEuler(int euler)
        {
            euler %= 360;
            if (euler <= -180) euler += 360;
            if (euler > 180) euler -= 360;
            return euler;
        }

        /// <summary>
        /// �C�ӂ̊p�x(����)��-179 �` 180�ɕϊ�����(60�x�P��)
        /// </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static int GetIntegerEuler60(int euler)
        {
            euler = (euler + 30) / 60 * 60;
            return GetIntegerEuler(euler);
        }
    }
}
