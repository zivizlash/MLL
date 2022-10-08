namespace MLL.Tools
{
    public class ConcurrentFlag
    {
        private int _value;

        public ConcurrentFlag(bool defaultValue = false)
        {
            _value = ToInt(defaultValue);
        }

        public bool TrySetValue(bool value)
        {
            var expected = ToInt(!value);

            return Interlocked.CompareExchange(
                ref _value, ToInt(value), expected) == expected;
        }
        
        private static int ToInt(bool value) => value ? 1 : 0;
    }
}
