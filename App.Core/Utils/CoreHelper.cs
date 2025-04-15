namespace App.Core.Utils
{
    public class CoreHelper
    {
        // Trả về thời gian hiện tại ở UTC (mặc định cho PostgreSQL)
        public static DateTimeOffset SystemTimeNow => new DateTimeOffset(TimeHelper.GetCurrentUtcTime(), TimeSpan.Zero);

        // Trả về thời gian hiện tại ở UTC+7 (tùy chọn cho SQL Server)
        public static DateTimeOffset SystemTimeNowWithOffset => TimeHelper.ConvertToUtcPlus7(DateTimeOffset.Now);
    }
}
