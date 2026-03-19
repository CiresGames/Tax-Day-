using AHAKuo.Signalia.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AHAKuo.Signalia.Utilities
{
    public static class Extensions
    {
        // ==================================================
        // TRANSFORM EXTENSIONS
        // ==================================================

        /// <summary>
        /// Sets the Transform's position X value while leaving Y and Z unchanged.
        /// </summary>
        public static void SetPosX(this Transform t, float x)
        {
            Vector3 pos = t.position;
            pos.x = x;
            t.position = pos;
        }

        /// <summary>
        /// Sets the Transform's position Y value while leaving X and Z unchanged.
        /// </summary>
        public static void SetPosY(this Transform t, float y)
        {
            Vector3 pos = t.position;
            pos.y = y;
            t.position = pos;
        }

        /// <summary>
        /// Sets the Transform's position Z value while leaving X and Y unchanged.
        /// </summary>
        public static void SetPosZ(this Transform t, float z)
        {
            Vector3 pos = t.position;
            pos.z = z;
            t.position = pos;
        }

        /// <summary>
        /// Offsets the Transform's position along the world-space X axis by a specified amount.
        /// </summary>
        public static void TranslateX(this Transform t, float offset)
        {
            t.position += new Vector3(offset, 0f, 0f);
        }

        /// <summary>
        /// Offsets the Transform's localPosition along the local X axis by a specified amount.
        /// </summary>
        public static void TranslateLocalX(this Transform t, float offset)
        {
            t.localPosition += new Vector3(offset, 0f, 0f);
        }

        // ==================================================
        // VECTOR EXTENSIONS
        // ==================================================

        /// <summary>
        /// Returns a new Vector3, replacing only the non-null components.
        /// Usage: myVector3 = myVector3.With(x: 5f);
        /// </summary>
        public static Vector3 With(this Vector3 v, float? x = null, float? y = null, float? z = null)
        {
            return new Vector3(x ?? v.x, y ?? v.y, z ?? v.z);
        }

        /// <summary>
        /// Returns a new Vector2, replacing only the non-null components.
        /// Usage: myVector2 = myVector2.With(y: 10f);
        /// </summary>
        public static Vector2 With(this Vector2 v, float? x = null, float? y = null)
        {
            return new Vector2(x ?? v.x, y ?? v.y);
        }

        /// <summary>
        /// Returns the same vector with an added offset that is randomized within positive and negative ranges that fall within the given range vector.
        /// </summary>
        /// <param name="v"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static Vector3 RandomizeInRange(this Vector3 v, Vector3 range)
        {
            float x = UnityEngine.Random.Range(v.x - range.x, v.x + range.x);
            float y = UnityEngine.Random.Range(v.y - range.y, v.y + range.y);
            float z = UnityEngine.Random.Range(v.x - range.z, v.x + range.z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the same vector with an added offset that is randomized within positive and negative ranges that fall within the given range vector. Uses itself as the range.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector3 RandomizeInRange(this Vector3 v)
        {
            float x = UnityEngine.Random.Range(v.x * -1, v.x);
            float y = UnityEngine.Random.Range(v.y * -1, v.y);
            float z = UnityEngine.Random.Range(v.z * -1, v.z);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Returns the same vector with an added offset that is randomized within positive and negative ranges that fall within the given range vector. Uses itself as the range.
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2 RandomizeInRange(this Vector2 v)
        {
            float x = UnityEngine.Random.Range(v.x * -1, v.x);
            float y = UnityEngine.Random.Range(v.y * -1, v.y);
            return new Vector3(x, y);
        }

        // ==================================================
        // GAMEOBJECT EXTENSIONS
        // ==================================================

        /// <summary>
        /// Gets the component of type T if it exists, otherwise adds one.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            T comp = go.GetComponent<T>();
            if (comp == null)
                comp = go.AddComponent<T>();
            return comp;
        }

        /// <summary>
        /// Recursively sets the layer of the GameObject and all its children.
        /// </summary>
        public static void SetLayerRecursively(this GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }

        /// <summary>
        /// Sets the GameObject to be persistent across scene loads (DontDestroyOnLoad).
        /// </summary>
        /// <param name="go"></param>
        public static void DDOL(this GameObject go)
        {
            if (go == null)
            {
                Debug.LogError("GameObject is null! Cannot set to DontDestroyOnLoad.");
                return;
            }
            if (Application.isPlaying)
            {
                UnityEngine.Object.DontDestroyOnLoad(go);
            }
            else
            {
                Debug.LogWarning("DontDestroyOnLoad can only be called in play mode. Ignoring.");
            }
        }

        /// <summary>
        /// Check if this GameObject is on a specific layer.
        /// </summary>
        /// <param name="go"></param>
        /// <param name="layerMask"></param>
        public static bool IsOnLayer(this GameObject go, LayerMask layerMask)
        {
            if (go == null)
            {
                Debug.LogError("GameObject is null! Cannot check layer.");
                return false;
            }

            return (layerMask & (1 << go.layer)) != 0;
        }

        // ==================================================
        // COMPONENT EXTENSIONS
        // ==================================================

        /// <summary>
        /// Attempts to get a component of type T from this Component's GameObject or its parent GameObjects.
        /// Returns true if the component was found, false otherwise.
        /// Supports both Component types and interfaces.
        /// </summary>
        /// <typeparam name="T">The type of component or interface to search for.</typeparam>
        /// <param name="component">The component to start searching from.</param>
        /// <param name="result">The found component, or null if not found.</param>
        /// <returns>True if the component was found, false otherwise.</returns>
        public static bool TryGetComponentInParent<T>(this Component component, out T result) where T : class
        {
            result = component.GetComponentInParent<T>();
            return result != null;
        }

        // ==================================================
        // COLLECTION EXTENSIONS
        // ==================================================

        private static System.Random rng = new System.Random();

        /// <summary>
        /// Shuffles an IList<T> in place using the Fisher-Yates algorithm.
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        /// <summary>
        /// Returns a random element from the IList<T>, or default if the list is null/empty.
        /// </summary>
        public static T RandomElement<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            int index = rng.Next(list.Count);
            return list[index];
        }

        public static T Random<T>(this T[] array)
        {
            if (array == null || array.Length == 0)
            {
                Debug.LogError("Array is either null or empty!");
                return default(T);
            }

            int randomIndex = rng.Next(0, array.Length);
            return array[randomIndex];
        }

        /// <summary>
        /// Returns the value associated with the specified key, or a default value if not found.
        /// </summary>
        public static TValue GetValueOrDefault<TKey, TValue>(
            this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            if (dictionary.TryGetValue(key, out TValue val))
                return val;
            return defaultValue;
        }

        /// <summary>
        /// Returns true if the list is null or empty.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool Empty(this IList list)
        {
            return list == null || list.Count == 0;
        }

        /// <summary>
        /// Returns true if the list is not null and has at least one element.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static bool HasValue(this IList list)
        {
            return list != null && list.Count > 0;
        }

        // ==================================================
        // STRING EXTENSIONS
        // ==================================================

        /// <summary>
        /// Returns true if the string is null or empty.
        /// </summary>
        public static bool IsNullOrEmpty(this string s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static bool HasValue(this string s)
        {
            return !string.IsNullOrEmpty(s);
        }

        public static bool IsSameAs(this string s, string other, bool ignoreCase)
        {
            return string.Equals(s, other, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the string is null, empty, or entirely whitespace.
        /// </summary>
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        /// <summary>
        /// Returns true if the string contains the given value, ignoring case differences.
        /// </summary>
        public static bool ContainsIgnoreCase(this string s, string value)
        {
            if (s == null || value == null) return false;
            return s.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Converts "HelloWorld" -> "helloWorld" (lowercase first letter).
        /// </summary>
        public static string ToCamelCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Converts "hello world" -> "HelloWorld" by capitalizing each word and removing separators.
        /// </summary>
        public static string ToPascalCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var words = s.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                words[i] = char.ToUpperInvariant(words[i][0])
                    + words[i].Substring(1).ToLowerInvariant();
            }
            return string.Join("", words);
        }

        /// <summary>
        /// Converts "HelloWorld" -> "hello-world" by inserting a hyphen before uppercase letters.
        /// </summary>
        public static string ToKebabCase(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsUpper(c) && i > 0)
                    sb.Append('-');
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
        
        // ==================================================
        // BOOL EXTENSIONS
        // ==================================================
        
        /// <summary>
        /// Returns true if the value is true.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Not(this bool b) => !b; // just a simple testable thing
        
        /// <summary>
        /// Returns true if either value is true.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Or(this bool a, bool b) => a || b;
        
        /// <summary>
        /// Returns true if both values are true.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool And(this bool a, bool b) => a && b;
        
        /// <summary>
        /// Return a random boolean value. Must be called from either True or False.
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Random(this bool b) => UnityEngine.Random.Range(0, 2) == 0;

        // ==================================================
        // MATH EXTENSIONS
        // ==================================================

        /// <summary>
        /// Determines whether two floats are approximately equal, given a tolerance.
        /// </summary>
        public static bool Approximately(this Single a, Single b, Single tolerance = 0.0001f)
        {
            return Mathf.Abs(a - b) <= tolerance;
        }

        /// <summary>
        /// Clamps a float between a specified minimum and maximum value.
        /// </summary>
        public static Single Clamp(this Single val, Single min, Single max)
        {
            return Mathf.Clamp(val, min, max);
        }

        public static bool MoreThanZero(this Single val)
        {
            return val > 0;
        }

        public static Single Total(this IEnumerable<Single> values)
        {
            float total = 0;
            foreach (var value in values)
            {
                total += value;
            }
            return total;
        }

        public static Single Total(this List<Single> values)
        {
            float total = 0;
            foreach (var value in values)
            {
                total += value;
            }
            return total;
        }

        /// <summary>
        /// Value is zero.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool Zero(this Single val)
        {
            return Mathf.Approximately(val, 0f);
        }

        /// <summary>
        /// Value is zero.
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static bool Zero(this int val)
        {
            return val == 0;
        }

        // ==================================================
        // ANIMATION EXTENSIONS
        // ==================================================

        /// <summary>
        /// Checks if the animator is currently in the specified state (by name) on a given layer.
        /// Note: Returns false if currently transitioning.
        /// </summary>
        /// <param name="animator">The Animator to check.</param>
        /// <param name="stateName">The full path or short name of the animation state.</param>
        /// <param name="layer">Animator layer to check.</param>
        public static bool IsPlaying(this Animator animator, string stateName, int layer = 0)
        {
            if (animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layer);
            return currentState.IsName(stateName);
        }

        /// <summary>
        /// Checks if the animator is transitioning to the specified state (by name) on a given layer.
        /// </summary>
        /// <param name="animator">The Animator to check.</param>
        /// <param name="stateName">The full path or short name of the animation state.</param>
        /// <param name="layer">Animator layer to check.</param>
        public static bool IsTransitioningTo(this Animator animator, string stateName, int layer = 0)
        {
            if (!animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(layer);
            return nextState.IsName(stateName);
        }

        /// <summary>
        /// Checks if the animator is playing ANY of the specified state names on a given layer.
        /// Useful if you have multiple states you want to treat equivalently.
        /// </summary>
        /// <param name="animator">The Animator to check.</param>
        /// <param name="layer">Animator layer to check.</param>
        /// <param name="stateNames">An array of state names to match against.</param>
        public static bool IsPlayingAnyOf(this Animator animator, int layer = 0, params string[] stateNames)
        {
            if (animator.IsInTransition(layer))
                return false;

            AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(layer);
            foreach (var name in stateNames)
            {
                if (currentState.IsName(name))
                    return true;
            }
            return false;
        }

        // ==================================================
        // ACTION EXTENSIONS
        // ==================================================

        /// <summary>
        /// Do this action after a specified number of seconds.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="seconds"></param>
        /// <param name="ignoreTimescale"></param>
        public static void DoIn(this Action action, float seconds, bool ignoreTimescale = true)
        {
            SIGS.DoIn(seconds, action, ignoreTimescale);
        }
    }
}
