using Godot;
using Godot.Collections;
using System.Collections.ObjectModel;
using System.Linq;
#nullable enable
namespace MonoCustomResourceRegistry
{
    public static class Settings
    {
        public enum ResourceSearchType
        {
            Recursive = 0,
            Namespace = 1,
        }

        public static string ClassPrefix => GetSettings(nameof(ClassPrefix)).AsString();
        public static ResourceSearchType SearchType => (ResourceSearchType)GetSettings(nameof(SearchType)).AsInt32();
        public static ReadOnlyCollection<string> ResourceScriptDirectories
        {
            get
            {
                Array array = (Array?)GetSettings(nameof(ResourceScriptDirectories)) ?? new Array();
                return new(array.Select(v => v.AsString()).ToList());
            }
        }

        public static void Init()
        {
            AddSetting(nameof(ClassPrefix), Variant.Type.String, Variant.CreateFrom(""));
            AddSetting(nameof(SearchType), Variant.Type.Int, Variant.CreateFrom((int)ResourceSearchType.Recursive), PropertyHint.Enum, "Recursive,Namespace");
            AddSetting(nameof(ResourceScriptDirectories), Variant.Type.Array, new Array<string>(new string[] { "res://" }));
        }

        private static Variant GetSettings(string title)
        {
            return ProjectSettings.GetSetting($"{nameof(MonoCustomResourceRegistry)}/{title}");
        }

        private static void AddSetting(string title, Variant.Type type, Variant value, PropertyHint hint = PropertyHint.None, string hintString = "")
        {
            title = SettingPath(title);
            if (!ProjectSettings.HasSetting(title))
                ProjectSettings.SetSetting(title, value);
            var info = new Dictionary
            {
                ["name"] = title,
                ["type"] = Variant.From(type),
                ["hint"] = Variant.From(hint),
                ["hint_string"] = hintString,
            };
            ProjectSettings.AddPropertyInfo(info);
            GD.Print("Successfully added property: " + title);
        }

        private static string SettingPath(string title) => $"{nameof(MonoCustomResourceRegistry)}/{title}";
    }
}