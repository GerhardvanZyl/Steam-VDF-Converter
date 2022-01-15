using System;
using System.Collections.Generic;

namespace VdfParser.Test
{
    public class App
    {
        public Dictionary<string, string> Tags { get; set; }
    }

    public class AppWithList
    {
        public List<string> Tags { get; set; }
    }

    public class Root
    {
        public bool StartMenuShortcutCheck { get; set; }
        public bool DesktopShortcutCheck { get; set; }
        public DateTime SurveyDate { get; set; }
        public string SurveyDateVersion {get;set;}
        public string SteamDefaultDialog { get; set; }
        public Dictionary<string, App> Apps { get; set; }
    }

    public class SteamBasic
    {
        public bool StartMenuShortcutCheck { get; set; }
        public bool DesktopShortcutCheck { get; set; }
        public DateTime SurveyDate { get; set; }
        public string SurveyDateVersion { get; set; }
        public string SteamDefaultDialog { get; set; }
    }

    public class VdfFileTestExceprt
    {
        public Root Steam { get; set; }
    }

    public class VdfWithList
    {
        public RootWithList Steam { get; set; }
    }

    public class RootWithList
    {
        public bool StartMenuShortcutCheck { get; set; }
        public Dictionary<string, AppWithList> Apps { get; set; }
    }

    public class MyDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {

        public new void Add(TKey key, TValue val)
        {
            base.Add(key, val);
        }
    }

    public class CustomVdfExcerpt
    {
        public CustomRoot Steam { get; set; }
    }

    public class CustomRoot
    {
        public MyDictionary<string, AppWithList> Apps { get; set; }
    }

    public class LibraryFolder
    {
        public string path { get; set; }

        public string label { get; set; }
    }

    public class LibraryFolders<TKey, TValue> : Dictionary<TKey, TValue>
    {
        public string contentstatsid { get; set; }
    }

    public class Library
    {
        public LibraryFolders<int, LibraryFolder> libraryfolders { get; set; }
    }

}
