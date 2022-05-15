
using System;
using System.Collections.Generic;

namespace SecyrityMail.Utils
{
    internal class ConverterComparer : IEqualityComparer<Tuple<string, string>>
    {
        public bool Equals(Tuple<string, string> x1, Tuple<string, string> x2) => x1 != null && x1.Item1.Equals(x2?.Item1);
        public int GetHashCode(Tuple<string, string> x) => (x == null) ? 0 : x.Item1.GetHashCode();
    }
}
