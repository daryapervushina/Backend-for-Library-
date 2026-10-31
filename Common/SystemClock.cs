namespace Library.Common;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
