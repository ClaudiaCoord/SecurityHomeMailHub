
using System;
using System.Collections.Generic;
using System.Threading;

namespace HomeMailHub.Gui
{
    internal class GuiLinearData
    {
        public int X = 0, Y = 0, Width = 0, Height = 1;
        public bool AutoSize = false;

        public GuiLinearData() { }
        public GuiLinearData(int x, int y, bool b = false) {
            X = x;
            Y = y;
            AutoSize = b;
            Height = 0;
        }
        public GuiLinearData(int x, int y, int w, int h) {
            X = x; 
            Y = y;
            Width = w;
            Height = h;
        }
    }
    internal class GuiLinearLayot
    {
        private static readonly string _DefaultLang = "en";
        private static readonly string _CurrentLang;
        static GuiLinearLayot() =>
            _CurrentLang = Thread.CurrentThread.CurrentCulture.TwoLetterISOLanguageName.ToLowerInvariant();

        private Dictionary<string, List<GuiLinearData>> DictValues { get; } = new();
        public List<GuiLinearData> Get() => Get(_CurrentLang);
        public List<GuiLinearData> Get(string key) { if (DictValues.TryGetValue(key, out List<GuiLinearData> data)) return data; return null; }
        public List<GuiLinearData> GetDefault() => GetDefault(_CurrentLang);
        public List<GuiLinearData> GetDefault(string key) {
            List<GuiLinearData> data;
            if (DictValues.TryGetValue(key, out data))
                return data;
            if (DictValues.TryGetValue(_DefaultLang, out data))
                return data;
            throw new Exception($"{nameof(GuiLinearLayot)} - NOT found default english layout!");
        }
        public void Add(string key, int x, int y, int w, int h, bool b = false) {
            List<GuiLinearData> list = Get(key);
            if (list != null) {
                DictValues.Remove(key);
                list.Add(new GuiLinearData { X = x, Y = y, Width = w, Height = h, AutoSize = b });
                DictValues[key] = list;
            } else {
                DictValues[key] = new List<GuiLinearData>() {
                    new GuiLinearData {X = x, Y = y, Width = w, Height = h, AutoSize = b}
                };
            }
        }
        public void Add(string key, List<GuiLinearData> list) {
            if (Get(key) != null)
                return;
            DictValues[key] = list;
        }
    }
}
