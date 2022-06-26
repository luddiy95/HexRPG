using UnityEngine;

namespace HexRPG
{
    using Battle.Stage;

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

        public static Vector3 GetRelativePosXZ(this Vector3 self, Vector3 other)
        {
            var relativePos = other - self;
            relativePos.y = 0;
            return relativePos;
        }

        public static float GetDistance2XZ(this Vector3 self, Vector3 other)
        {
            return self.GetRelativePosXZ(other).sqrMagnitude;
        }

        public static float GetDistanceXZ(this Vector3 self, Vector3 other)
        {
            return Mathf.Sqrt(self.GetDistance2XZ(other));
        }

        public static float GetDistance2XZ(this Hex self, Hex other)
        {
            return self.transform.position.GetDistance2XZ(other.transform.position);
        }

        public static float GetDistanceXZ(this Hex self, Hex other)
        {
            return self.transform.position.GetDistanceXZ(other.transform.position);
        }
    }
}
