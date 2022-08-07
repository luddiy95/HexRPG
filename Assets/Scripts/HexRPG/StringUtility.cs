
namespace HexRPG
{
    public static class StringUtility
    {
        public static string GetMinute(this float time)
        {
            var integer = (int)time;
            var minute = integer / 60;
            return string.Format("{0:D2}", minute);
        }

        public static string GetSecond(this float time)
        {
            var integer = (int)time;
            var second = integer % 60;
            return string.Format("{0:D2}", second);
        }

        public static string GetMilliSecond(this float time)
        {
            var integer = (int)time;
            var fraction = (int)((time - integer) * 1000);
            return string.Format("{0:D3}", fraction);
        }

        public static string GetTime(this float time)
        {
            return time.GetMinute() + ":" + time.GetSecond() + ":" + time.GetMilliSecond();
        }
    }
}
