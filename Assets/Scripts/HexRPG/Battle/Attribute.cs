using System;

namespace HexRPG.Battle
{
    public enum Attribute
    {
        ATTRIBUTE_01, // 04�ɋ���
        ATTRIBUTE_02, // 01�ɋ���
        ATTRIBUTE_03, // 02�ɋ���
        ATTRIBUTE_04 // 03�ɋ���
    }

    public static class AttributeExtensions
    {
        static Attribute[] AttributeArray => (Attribute[])Enum.GetValues(typeof(Attribute));

        /// <summary>
        /// �������g(self)��other�̑����Ɏア���ǂ���
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
