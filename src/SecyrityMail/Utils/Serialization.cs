
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace SecyrityMail.Utils
{
    internal static class Serialization
    {
        #region Xml
        /// <summary>
        /// Сохранение данных в файл
        /// </summary>
        /// <typeparam name="T1">тип данных</typeparam>
        /// <param name="path">путь к файлу <see cref="string">string</see></param>
        /// <param name="src">данные</param>
        /// <param name="isappend">перезаписывать файл</param>
        /// <param name="enc"><see cref="Encoding">Encoding</see></param>
        public static void SerializeToFile<T1>(this string path, T1 src, bool isappend = false, Encoding enc = default)
        {
            if (src == null) return;
            enc = enc == default ? new UTF8Encoding(false) : enc;
            using StreamWriter sw = new(path, isappend, enc);
            XmlSerializer xml = new(typeof(T1));
            xml.Serialize(sw, src);
        }
        /// <summary>
        /// Чтение данных из файла
        /// </summary>
        /// <typeparam name="T1">тип данных</typeparam>
        /// <param name="path">путь к файлу <see cref="string">string</see></param>
        /// <param name="enc"><see cref="Encoding">Encoding</see></param>
        /// <returns>сщгласно типу данных</returns>
        public static T1 DeserializeFromFile<T1>(this string path, Encoding enc = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return default;
            enc = enc == default ? new UTF8Encoding(false) : enc;
            using StreamReader sr = new(path, enc, true);
            XmlSerializer xml = new(typeof(T1));
            if (xml.Deserialize(sr) is T1 val)
                return val;
            return default;
        }
        #endregion
    }
}
