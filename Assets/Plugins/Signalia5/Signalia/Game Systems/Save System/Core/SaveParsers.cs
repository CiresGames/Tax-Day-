using System;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.SaveSystem
{
    /// <summary>
    /// The SaveParsers system handles serialization and deserialization of various types
    /// to and from string representation for the Save System.
    /// Supports extensible custom parsers and a fallback JSON parser.
    /// </summary>
    public static class SaveParsers
    {
        private static readonly List<ISaveParser> _parsers = new List<ISaveParser>();
        private static readonly List<ISaveParser> _customParsers = new List<ISaveParser>();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the default parsers
        /// </summary>
        private static void Initialize()
        {
            if (_initialized) return;
            
            _parsers.Add(new NumericParser());
            _parsers.Add(new BoolParser());
            _parsers.Add(new StringParser());
            _parsers.Add(new UnityCommonTypesParser());
            _parsers.Add(new TransformDataParser());
            _parsers.Add(new JSONParser()); // Fallback
            
            _initialized = true;
        }

        /// <summary>
        /// Registers a custom parser. Custom parsers have highest priority.
        /// </summary>
        public static void RegisterCustomParser(ISaveParser parser)
        {
            if (!_customParsers.Contains(parser))
            {
                _customParsers.Add(parser);
            }
        }

        /// <summary>
        /// Serializes a value to a string using the appropriate parser
        /// </summary>
        public static string Serialize(object value)
        {
            if (value == null) return "";
            
            Initialize();
            
            Type type = value.GetType();
            
            // Check custom parsers first
            foreach (var parser in _customParsers)
            {
                if (parser.CanParse(type))
                {
                    return parser.Serialize(value);
                }
            }
            
            // Check built-in parsers
            foreach (var parser in _parsers)
            {
                if (parser.CanParse(type))
                {
                    return parser.Serialize(value);
                }
            }
            
            Debug.LogWarning($"No parser found for type {type}. Value will not be saved.");
            return "";
        }

        /// <summary>
        /// Deserializes a string to the specified type using the appropriate parser
        /// </summary>
        public static T Deserialize<T>(string value)
        {
            if (string.IsNullOrEmpty(value)) return default;
            
            Initialize();
            
            Type type = typeof(T);
            
            // Check custom parsers first
            foreach (var parser in _customParsers)
            {
                if (parser.CanParse(type))
                {
                    return (T)parser.Deserialize(value, type);
                }
            }
            
            // Check built-in parsers
            foreach (var parser in _parsers)
            {
                if (parser.CanParse(type))
                {
                    return (T)parser.Deserialize(value, type);
                }
            }
            
            Debug.LogWarning($"No parser found for type {type}. Returning default value.");
            return default;
        }

        #region Built-in Parsers

        /// <summary>
        /// Parser for numeric types (int, float, double, long, short, byte, decimal, etc.)
        /// </summary>
        private class NumericParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                return type == typeof(int) || type == typeof(float) || type == typeof(double) ||
                       type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                       type == typeof(sbyte) || type == typeof(uint) || type == typeof(ulong) ||
                       type == typeof(ushort) || type == typeof(decimal);
            }

            public string Serialize(object value)
            {
                return value.ToString();
            }

            public object Deserialize(string value, Type type)
            {
                try
                {
                    if (type == typeof(int)) return int.Parse(value);
                    if (type == typeof(float)) return float.Parse(value);
                    if (type == typeof(double)) return double.Parse(value);
                    if (type == typeof(long)) return long.Parse(value);
                    if (type == typeof(short)) return short.Parse(value);
                    if (type == typeof(byte)) return byte.Parse(value);
                    if (type == typeof(sbyte)) return sbyte.Parse(value);
                    if (type == typeof(uint)) return uint.Parse(value);
                    if (type == typeof(ulong)) return ulong.Parse(value);
                    if (type == typeof(ushort)) return ushort.Parse(value);
                    if (type == typeof(decimal)) return decimal.Parse(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse numeric value '{value}' to type {type}: {e.Message}");
                }
                return Activator.CreateInstance(type);
            }
        }

        /// <summary>
        /// Parser for boolean values
        /// </summary>
        private class BoolParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                return type == typeof(bool);
            }

            public string Serialize(object value)
            {
                return value.ToString();
            }

            public object Deserialize(string value, Type type)
            {
                if (bool.TryParse(value, out bool result))
                {
                    return result;
                }
                return false;
            }
        }

        /// <summary>
        /// Parser for string values
        /// </summary>
        private class StringParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                return type == typeof(string);
            }

            public string Serialize(object value)
            {
                return (string)value;
            }

            public object Deserialize(string value, Type type)
            {
                return value;
            }
        }

        /// <summary>
        /// Parser for common Unity types (Vector2, Vector3, Vector4, Vector2Int, Vector3Int, Rect, RectInt, Bounds, BoundsInt)
        /// </summary>
        private class UnityCommonTypesParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) ||
                       type == typeof(Vector2Int) || type == typeof(Vector3Int) ||
                       type == typeof(Rect) || type == typeof(RectInt) ||
                       type == typeof(Bounds) || type == typeof(BoundsInt);
            }

            public string Serialize(object value)
            {
                if (value is Vector2 v2) return $"{v2.x},{v2.y}";
                if (value is Vector3 v3) return $"{v3.x},{v3.y},{v3.z}";
                if (value is Vector4 v4) return $"{v4.x},{v4.y},{v4.z},{v4.w}";
                if (value is Vector2Int v2i) return $"{v2i.x},{v2i.y}";
                if (value is Vector3Int v3i) return $"{v3i.x},{v3i.y},{v3i.z}";
                if (value is Rect r) return $"{r.x},{r.y},{r.width},{r.height}";
                if (value is RectInt ri) return $"{ri.x},{ri.y},{ri.width},{ri.height}";
                if (value is Bounds b) return $"{b.center.x},{b.center.y},{b.center.z},{b.size.x},{b.size.y},{b.size.z}";
                if (value is BoundsInt bi) return $"{bi.position.x},{bi.position.y},{bi.position.z},{bi.size.x},{bi.size.y},{bi.size.z}";
                return "";
            }

            public object Deserialize(string value, Type type)
            {
                try
                {
                    string[] parts = value.Split(',');
                    
                    if (type == typeof(Vector2))
                        return new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
                    
                    if (type == typeof(Vector3))
                        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                    
                    if (type == typeof(Vector4))
                        return new Vector4(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                    
                    if (type == typeof(Vector2Int))
                        return new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1]));
                    
                    if (type == typeof(Vector3Int))
                        return new Vector3Int(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                    
                    if (type == typeof(Rect))
                        return new Rect(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                    
                    if (type == typeof(RectInt))
                        return new RectInt(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), int.Parse(parts[3]));
                    
                    if (type == typeof(Bounds))
                    {
                        Vector3 center = new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
                        Vector3 size = new Vector3(float.Parse(parts[3]), float.Parse(parts[4]), float.Parse(parts[5]));
                        return new Bounds(center, size);
                    }
                    
                    if (type == typeof(BoundsInt))
                    {
                        Vector3Int position = new Vector3Int(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
                        Vector3Int size = new Vector3Int(int.Parse(parts[3]), int.Parse(parts[4]), int.Parse(parts[5]));
                        return new BoundsInt(position, size);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse Unity type '{value}' to type {type}: {e.Message}");
                }
                return Activator.CreateInstance(type);
            }
        }

        /// <summary>
        /// Parser for Transform-related data types (Color, Color32, Quaternion, LayerMask)
        /// </summary>
        private class TransformDataParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                return type == typeof(Color) || type == typeof(Color32) ||
                       type == typeof(Quaternion) || type == typeof(LayerMask);
            }

            public string Serialize(object value)
            {
                if (value is Color c) return $"{c.r},{c.g},{c.b},{c.a}";
                if (value is Color32 c32) return $"{c32.r},{c32.g},{c32.b},{c32.a}";
                if (value is Quaternion q) return $"{q.x},{q.y},{q.z},{q.w}";
                if (value is LayerMask lm) return lm.value.ToString();
                return "";
            }

            public object Deserialize(string value, Type type)
            {
                try
                {
                    if (type == typeof(Color))
                    {
                        string[] parts = value.Split(',');
                        return new Color(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                    }
                    
                    if (type == typeof(Color32))
                    {
                        string[] parts = value.Split(',');
                        return new Color32(byte.Parse(parts[0]), byte.Parse(parts[1]), byte.Parse(parts[2]), byte.Parse(parts[3]));
                    }
                    
                    if (type == typeof(Quaternion))
                    {
                        string[] parts = value.Split(',');
                        return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
                    }
                    
                    if (type == typeof(LayerMask))
                    {
                        return (LayerMask)int.Parse(value);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to parse transform data '{value}' to type {type}: {e.Message}");
                }
                return Activator.CreateInstance(type);
            }
        }

        /// <summary>
        /// JSON parser as fallback for serializable classes
        /// </summary>
        private class JSONParser : ISaveParser
        {
            public bool CanParse(Type type)
            {
                // Fallback parser - accepts everything
                return true;
            }

            public string Serialize(object value)
            {
                try
                {
                    return JsonUtility.ToJson(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to serialize object to JSON: {e.Message}");
                    return "";
                }
            }

            public object Deserialize(string value, Type type)
            {
                try
                {
                    return JsonUtility.FromJson(value, type);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to deserialize JSON to type {type}: {e.Message}");
                    return Activator.CreateInstance(type);
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Interface for custom parsers
    /// </summary>
    public interface ISaveParser
    {
        /// <summary>
        /// Returns true if this parser can handle the given type
        /// </summary>
        bool CanParse(Type type);
        
        /// <summary>
        /// Serializes the value to a string
        /// </summary>
        string Serialize(object value);
        
        /// <summary>
        /// Deserializes the string to the specified type
        /// </summary>
        object Deserialize(string value, Type type);
    }
}
