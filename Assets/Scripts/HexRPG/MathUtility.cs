using UnityEngine;

namespace HexRPG
{
    public static class MathUtility
    {
        /// <summary>
        /// 任意の角度(整数)を-179 〜 180に変換する
        /// </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static int GetIntegerEuler(float e)
        {
            int euler = (int)e;
            euler %= 360;
            if (euler <= -180) euler += 360;
            if (euler > 180) euler -= 360;
            return euler;
        }

        /// <summary>
        /// 任意の角度(整数)を-179 〜 180に変換する(60度単位)
        /// </summary>
        /// <param name="euler"></param>
        /// <returns></returns>
        public static int GetIntegerEuler60(float e)
        {
            int euler = (int)e;
            euler = (euler + 30) / 60 * 60;
            return GetIntegerEuler(euler);
        }
    }
}
