using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace AzureCloudRocks.CodeSamples.Album.WebApi
{
    public static class Extensions
    {
        public static string AppendTicksAsDateTime(this string input)
        {
            if (input != null)
            {
                int startIndex = input.LastIndexOf('-');
                if (startIndex > 0)
                {
                    try
                    {
                        string ticks = input.Substring(startIndex + 1, input.Length - startIndex - 1);
                        DateTime time = new DateTime(long.Parse(ticks));
                        return input.Substring(0, startIndex + 1) + "(" + time.ToString("g") + ")";
                    }
                    catch
                    {
                        return input;
                    }
                }
            }
            return input;
        }
    }
}