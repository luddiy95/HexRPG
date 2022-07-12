
namespace HexRPG.Battle
{
    using static Attribute;

    public enum Attribute
    {
        ATTRIBUTE_01, // 04に強い
        ATTRIBUTE_02, // 01に強い
        ATTRIBUTE_03, // 02に強い
        ATTRIBUTE_04, // 03に強い
        
        NONE
    }

    public static class AttributeExtensions
    {
        static Attribute[] AttributeArray = new Attribute[] { ATTRIBUTE_01, ATTRIBUTE_02, ATTRIBUTE_03, ATTRIBUTE_04 };

        /// <summary>
        /// 自分自身(self)がotherの属性に弱いかどうか
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
