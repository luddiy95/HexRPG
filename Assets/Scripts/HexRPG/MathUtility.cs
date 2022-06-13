using UnityEngine;

namespace HexRPG
{
    public static class MathUtility
    {
        /// <summary>
        /// ”CˆÓ‚ÌŠp“x(®”)‚ğ-179 ` 180‚É•ÏŠ·‚·‚é
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
        /// ”CˆÓ‚ÌŠp“x(®”)‚ğ-179 ` 180‚É•ÏŠ·‚·‚é(60“x’PˆÊ)
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
