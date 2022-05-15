
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace HomeMailHub.CmdLine
{
    internal class CmdOption
    {
        #region public
        public static void Help<T>(Func<string, string> fun = default) where T : class, new()
        {
            T clz = new();
            Type t = typeof(CmdOptionAttribute);
            foreach (var attr in from PropertyInfo prop in clz.GetType().GetProperties()
                                 from CmdOptionAttribute attr in prop.GetCustomAttributes(t, false)
                                 select attr)
            {
                try
                {
                    string id = attr.ResourceId,
                           key = attr.Key,
                           desc = attr.Desc,
                           txt = string.Empty;

                    if (string.IsNullOrWhiteSpace(key))
                        continue;

                    do
                    {
                        if (!string.IsNullOrWhiteSpace(id) && (fun != default))
                            try { txt = fun.Invoke(id); } catch { txt = string.Empty; }
                        if (!string.IsNullOrWhiteSpace(txt))
                            break;
                        txt = desc;
                        if (!string.IsNullOrWhiteSpace(txt))
                            break;
                        txt = "..";

                    } while (false);

                    Console.WriteLine($"\t{key}".PadRight(20) + $"{txt}");
                }
                catch (Exception e) { throw CmdOptionException.Create($"{nameof(Help)}: {e.Message}", e); }
            }
        }

        public static T Parse<T>(string[] args) where T : class, new()
        {

            T clz = new();
            Type t = typeof(CmdOptionAttribute);
            foreach (var (prop, attr) in from PropertyInfo prop in clz.GetType().GetProperties()
                                         from CmdOptionAttribute attr in prop.GetCustomAttributes(t, false)
                                         select (prop, attr)) {
                try
                {
                    string opt, key = attr.Key;

                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    if (attr.IsSwitch)
                    {
                        if (prop.PropertyType != typeof(bool))
                            throw CmdOptionException.Create(
                                new TypeInitializationException(nameof(Boolean), default));

                        if (IsFoundValue(key, args))
                            prop.SetValue(clz, true);
                    }
                    else if (attr.IsEnum)
                    {
                        if (!prop.PropertyType.IsEnum)
                            throw CmdOptionException.Create(
                                new TypeInitializationException(nameof(Enum), default));

                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;

                        //object val;
                        //opt = opt.ToLowerInvariant();
                        //if (Enum.TryParse(opt, out val)) // prop.PropertyType.GetType()
                        //prop.SetValue(clz, val);
                    }
                    else if (!string.IsNullOrWhiteSpace(attr.FileStringFormat))
                    {
                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;
                        string path = string.Format(attr.FileStringFormat, opt);
                        if (string.IsNullOrWhiteSpace(path))
                            throw CmdOptionException.Create(
                                new ArgumentException(prop.Name));

                        if (attr.IsFile)
                        {
                            if (prop.PropertyType != typeof(FileInfo))
                                throw CmdOptionException.Create(
                                    new TypeInitializationException(nameof(FileInfo), default));

                            prop.SetValue(clz, CreateFileInstance(path, attr.IsFileExists));
                        }
                        else if (attr.IsDirectory)
                        {
                            if (prop.PropertyType != typeof(DirectoryInfo))
                                throw CmdOptionException.Create(
                                    new TypeInitializationException(nameof(DirectoryInfo), default));

                            prop.SetValue(clz, CreateDirectoryInstance(path, attr.IsDirectoryExists));
                        }
                    }
                    else if (attr.IsFile)
                    {
                        if (prop.PropertyType != typeof(FileInfo))
                            throw CmdOptionException.Create(
                                new TypeInitializationException(nameof(FileInfo), default));

                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;

                        prop.SetValue(clz, CreateFileInstance(opt, attr.IsFileExists));
                    }
                    else if (attr.IsDirectory)
                    {
                        if (prop.PropertyType != typeof(DirectoryInfo))
                            throw CmdOptionException.Create(
                                new TypeInitializationException(nameof(DirectoryInfo), default));

                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;

                        prop.SetValue(clz, CreateDirectoryInstance(opt, attr.IsDirectoryExists));
                    }
                    else if (prop.PropertyType == typeof(string))
                    {
                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;
                        prop.SetValue(clz, opt);
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;
                        if (int.TryParse(opt, out int x))
                            prop.SetValue(clz, x);
                    }
                    else if (prop.PropertyType == typeof(long))
                    {
                        opt = GetNextValue(key, args);
                        if (string.IsNullOrWhiteSpace(opt))
                            continue;
                        if (long.TryParse(opt, out long x))
                            prop.SetValue(clz, x);
                    }
                    else
                    {
                        CmdOptionException.Create(
                            new Exception($"{nameof(Parse)}: {Properties.Resources.E1}: {key}/{prop.PropertyType}"));
                    }
                }
                catch (Exception e) when (e is CmdOptionException) { throw; }
                catch (Exception ex) { throw CmdOptionException.Create($"{nameof(Parse)}: {ex.Message}", ex); }
            }
            return clz;
        }
        #endregion

        #region private
        private static PropertyInfo GetPropertyInfo(PropertyInfo p, string s)
        {
            return p.PropertyType.GetProperty(
                s, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        private static string GetNextValue(string key, string[] args)
        {
            int idx = args.ToList().IndexOf(key) + 1;
            return ((args.Length <= idx) || (idx == 0)) ? default : args.Skip(idx).Take(1).FirstOrDefault();
        }
        private static bool IsFoundValue(string key, string[] args)
        {
            return (from i in args
                    where key.Equals(i)
                    select i).FirstOrDefault() != default;
        }
        private static FileInfo CreateFileInstance(string s, bool isExists)
        {
            FileInfo fi = new(CheckValidPath(s));
            if (fi == default)
                throw CmdOptionException.Create(
                    new NullReferenceException(nameof(FileInfo)));
            if (isExists && !fi.Exists)
                throw CmdOptionException.Create(
                    new FileNotFoundException(fi.FullName), true);
            return fi;
        }
        private static DirectoryInfo CreateDirectoryInstance(string s, bool isExists)
        {
            DirectoryInfo dir = new(CheckValidPath(s, false));
            if (dir == default)
                throw CmdOptionException.Create(
                    new NullReferenceException(nameof(DirectoryInfo)));
            if (isExists && !dir.Exists)
                throw CmdOptionException.Create(
                    new FileNotFoundException(dir.FullName), true);
            return dir;
        }
        private static string CheckValidPath(string s, bool isfile = true)
        {
            if (string.IsNullOrWhiteSpace(s))
                throw new NullReferenceException(Properties.Resources.E2);

            string name,
                   src = s.Trim(new char[] { '"', ' ', Path.DirectorySeparatorChar })
                          .Normalize(NormalizationForm.FormC);
            if (isfile)
                name = Path.GetDirectoryName(src);
            else
                name = src;

            if (!string.IsNullOrWhiteSpace(name))
            {
                char[] test = Path.GetInvalidPathChars();
                foreach (char c in test)
                    if (name.Contains(c))
                        throw new InvalidCastException(string.Format(Properties.Resources.E3, c, name));
            }

            if (!isfile)
                return src;

            name = Path.GetFileName(src);
            if (!string.IsNullOrWhiteSpace(name))
            {
                char[] test = Path.GetInvalidFileNameChars();
                foreach (char c in test)
                    if (name.Contains(c))
                        throw new InvalidCastException(string.Format(Properties.Resources.E4, c, name));
            }
            return src;
        }
        #endregion
    }
}
