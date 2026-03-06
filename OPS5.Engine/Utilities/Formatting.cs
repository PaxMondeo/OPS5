using System;

namespace OPS5.Engine.Utilities
{
    public static class Formatting
    {
        public static string CheckForDateTime(string? val)
        {
            if (val is string value)
            {
                if (!Decimal.TryParse(value, out _))
                {
                    if (value.Contains('/') || value.Contains('-') || value.Contains(' ')) //Allow Time to go unadulterated without adding a date to it.
                    {
                        if (DateTime.TryParse(value, out DateTime dtTime))
                        {
                            value = dtTime.ToString("yyyy-MM-dd HH:mm:ss.ffff");
                        }
                    }
                }

                return value;
            }
            else
                return string.Empty;
        }
    }
}
