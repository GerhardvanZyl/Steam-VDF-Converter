﻿using System;
using System.Collections.Generic;
using System.Text;

namespace VdfParser.Test
{
    public class App
    {
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
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

    public class VdfTestExcerptUnsupported
    {
        public UnsupportedRoot Steam { get; set; }
    }

    public class UnsupportedRoot
    {
        public List<string> Apps { get; set; }
    }
}