namespace App.Core.Utils
{
    public static class TimeHelper
    {
        public static DateTimeOffset ConvertToUtcPlus7(DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.ToOffset(new TimeSpan(7, 0, 0));
        }

        public static DateTimeOffset ConvertToUtcPlus7NotChanges(DateTimeOffset dateTimeOffset)
        {
            TimeSpan utcPlus7Offset = new(7, 0, 0);
            return dateTimeOffset.ToOffset(utcPlus7Offset).AddHours(-7);
        }

        public static DateTime GetCurrentUtcTime()
        {
            return DateTime.UtcNow;
        }
    }
}
