namespace WebUI.Extensions
{
    public static class DateOnlyExtension
    {
        public static DateTime ToDateTime(this DateOnly dateOnly)
        {
            return dateOnly.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        }
    }
}