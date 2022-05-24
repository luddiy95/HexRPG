using System;

namespace HexRPG.Battle
{
    public enum Attribute
    {
        ATTRIBUTE_01, // 04‚É‹­‚¢
        ATTRIBUTE_02, // 01‚É‹­‚¢
        ATTRIBUTE_03, // 02‚É‹­‚¢
        ATTRIBUTE_04 // 03‚É‹­‚¢
    }

    public static class AttributeExtensions
    {
        static Attribute[] AttributeArray => (Attribute[])Enum.GetValues(typeof(Attribute));

        /// <summary>
        /// ©•ª©g(self)‚ªother‚Ì‘®«‚Éã‚¢‚©‚Ç‚¤‚©
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsWeakCompatibity(this Attribute self, Attribute other)
        {
            return AttributeArray[((int)self + 1) % AttributeArray.Length] == other;
        }
    }
}
