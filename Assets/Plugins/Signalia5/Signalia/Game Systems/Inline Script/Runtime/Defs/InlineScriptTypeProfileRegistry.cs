using System;
using System.Collections.Generic;
using System.Reflection;

namespace AHAKuo.Signalia.GameSystems.InlineScript
{
    public static class InlineScriptTypeProfileRegistry
    {
        private static readonly Dictionary<Type, InlineScriptTypeProfile> Cache = new Dictionary<Type, InlineScriptTypeProfile>();

        public static InlineScriptTypeProfile GetProfile(Type scriptType)
        {
            if (scriptType == null)
            {
                return InlineVoid.TypeProfile;
            }

            if (Cache.TryGetValue(scriptType, out var profile))
            {
                return profile;
            }

            profile = ResolveProfile(scriptType);
            Cache[scriptType] = profile;
            return profile;
        }

        private static InlineScriptTypeProfile ResolveProfile(Type scriptType)
        {
            if (!typeof(ISB_TopLayer).IsAssignableFrom(scriptType))
            {
                return InlineVoid.TypeProfile;
            }

            var property = scriptType.GetProperty("TypeProfile", BindingFlags.Public | BindingFlags.Static);
            if (property != null && property.PropertyType == typeof(InlineScriptTypeProfile))
            {
                return (InlineScriptTypeProfile)property.GetValue(null);
            }

            return InlineVoid.TypeProfile;
        }
    }
}
