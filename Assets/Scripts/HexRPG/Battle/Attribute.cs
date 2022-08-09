
namespace HexRPG.Battle
{
    using static Attribute;

    public enum Attribute
    {
        ATTRIBUTE_01, // 04�ɋ���
        ATTRIBUTE_02, // 01�ɋ���
        ATTRIBUTE_03, // 02�ɋ���
        ATTRIBUTE_04, // 03�ɋ���
        
        NONE
    }

    public static class AttributeExtensions
    {
        static Attribute[] AttributeArray = new Attribute[] { ATTRIBUTE_01, ATTRIBUTE_02, ATTRIBUTE_03, ATTRIBUTE_04 };

        /// <summary>
        /// �������g(self)��other�̑����Ɏア���ǂ���
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
