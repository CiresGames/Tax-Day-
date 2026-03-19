using System;

namespace AHAKuo.Signalia.Framework.Editors
{
    /// <summary>
    /// Shared version metadata used by editor windows.
    /// </summary>
    [Serializable]
    public struct VersionInfo
    {
        public string package;
        public string version;
        public string date;
        public string company;
    }
}

