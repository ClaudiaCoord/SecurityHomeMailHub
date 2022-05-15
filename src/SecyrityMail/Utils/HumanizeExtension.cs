
using System;

namespace SecyrityMail.Utils
{
    public static class HumanizeExtension
    {
        public static string Humanize(this int size) => HumanizeExtension.Humanize((long)size);
        public static string Humanize(this uint size) => HumanizeExtension.Humanize((long)size);
        public static string Humanize(this ulong size) => HumanizeExtension.Humanize((long)size);
        public static string Humanize(this long size)
        {
            string[] tags = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
            if (size == 0)
                return "0" + tags[0];

            int idx = Convert.ToInt32(Math.Floor(Math.Log(size, 1024.0)));
            double num = Math.Round(size / Math.Pow(1024.0, idx), 1);
            return num + tags[idx];
        }
    }
}
