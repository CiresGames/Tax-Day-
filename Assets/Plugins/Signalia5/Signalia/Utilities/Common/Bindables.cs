using AHAKuo.Signalia.Framework;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace AHAKuo.Signalia.Utilities
{
    /// <summary>
    /// Contains tools that allow you to bind values to targets.
    /// </summary>
    public static class Bindables
    {
        private static readonly List<ReflectingPair> reflectPairs = new();

        private class ReflectingPair
        {
            private object targetObject;
            private PropertyInfo targetProperty;
            private FieldInfo targetField;
            private Func<object> source;
            private object lastValue;

            public ReflectingPair(object targetObject, MemberInfo targetMember, Func<object> source)
            {
                this.targetObject = targetObject;
                this.source = source;

                // Determine if the member is a property or field
                if (targetMember is PropertyInfo propertyInfo)
                {
                    targetProperty = propertyInfo;
                }
                else if (targetMember is FieldInfo fieldInfo)
                {
                    targetField = fieldInfo;
                }
                else
                {
                    throw new ArgumentException($"Target member '{targetMember.Name}' is not a field or property.");
                }

                Update();
            }

            public void Update()
            {
                var sourceValue = source();

                // if any of these are null, we don't want to do anything and also remove the pair
                if (sourceValue == null
                    || targetProperty == null
                    || targetObject == null)
                {
                    reflectPairs.Remove(this);
                    return;
                }

                if (!Equals(lastValue, sourceValue))
                {
                    if (targetProperty != null)
                    {
                        targetProperty.SetValue(targetObject, sourceValue);
                    }
                    else if (targetField != null)
                    {
                        targetField.SetValue(targetObject, sourceValue);
                    }

                    lastValue = sourceValue;
                }
            }
        }

        /// <summary>
        /// This method should be used from within the owner of the target we want to bind. this.Bind(() => target, () => source). Keep in mind, sometimes updating with bindings won't update what you see in the UI. So if something fails to visually update, try using something else.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="targetObject"></param>
        /// <param name="targetExpression"></param>
        /// <param name="source"></param>
        public static void Bind<T>(this object targetObject, Expression<Func<T>> targetExpression, Func<object> source)
        {
            if (targetExpression.Body is MemberExpression memberExpression)
            {
                var targetMember = memberExpression.Member;

                // If the expression is a property or field of a different object, get that object
                if (memberExpression.Expression is MemberExpression parentExpression)
                {
                    var parentObject = Expression.Lambda(parentExpression).Compile().DynamicInvoke();
                    var pair = new ReflectingPair(parentObject, targetMember, source);
                    reflectPairs.Add(pair);
                }
                else
                {
                    var pair = new ReflectingPair(targetObject, targetMember, source);
                    reflectPairs.Add(pair);
                }
            }
            else
            {
                throw new ArgumentException("Target expression must be a member expression.");
            }

            // add a watchman to the scene if it doesn't exist
            Watchman.Watch();
        }

        public static void UpdateBindings()
        {
            foreach (var pair in reflectPairs)
            {
                pair.Update();
            }
        }

        public static void ResetAll()
        {
            reflectPairs.Clear();
        }
    }
}