
namespace HexRPG.Battle
{
    using static Attribute;

    public enum Attribute
    {
        ATTRIBUTE_01, // 04Ç…ã≠Ç¢
        ATTRIBUTE_02, // 01Ç…ã≠Ç¢
        ATTRIBUTE_03, // 02Ç…ã≠Ç¢
        ATTRIBUTE_04, // 03Ç…ã≠Ç¢
        
        NONE
    }

    public static class AttributeExtensions
    {
        static Attribute[] AttributeArray = new Attribute[] { ATTRIBUTE_01, ATTRIBUTE_02, ATTRIBUTE_03, ATTRIBUTE_04 };

        /// <summary>
        /// é©ï™é©êg(self)Ç™otherÇÃëÆê´Ç…é„Ç¢Ç©Ç«Ç§Ç©
        /// </summary>
        /// <param name="self"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsWeakCompatibity(this Attribute self, Attribute other)
        {
            if (self == NONE || other == NONE) return false;
            return AttributeArray[((int)self + 1) % AttributeArray.Length] == other;
        }
    }
}
