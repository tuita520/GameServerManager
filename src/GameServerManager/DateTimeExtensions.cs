using System;

namespace GameServerManager
{
    public static class DateTimeExtensions
    {
        public static string ToTimeStamp(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy.MM.dd - HH.mm.ss");
        }
    }
}
