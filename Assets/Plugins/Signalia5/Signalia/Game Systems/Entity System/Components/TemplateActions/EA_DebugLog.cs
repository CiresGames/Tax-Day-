using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace AHAKuo.Signalia.GameSystems.Entities.Templates
{
    /// <summary>
    /// Advanced debug logging action for Entity System states.
    /// Provides rich context information, formatting options, rate limiting, and multiple trigger points.
    /// </summary>
    public class EA_DebugLog : EntityAction
    {
        #region Enums
        
        public enum DebugLogType 
        { 
            Info, 
            Warning, 
            Error, 
            Assert,
            Exception 
        }
        
        public enum LogTriggerPoint
        {
            OnEnter,
            OnTick,
            OnExit,
            OnEnterAndExit,
            OnAll
        }
        
        [Flags]
        public enum ContextInfo
        {
            None = 0,
            EntityName = 1 << 0,
            StateName = 1 << 1,
            LogicName = 1 << 2,
            Timestamp = 1 << 3,
            Position = 1 << 4,
            TimeInState = 1 << 5,
            TimeInLogic = 1 << 6,
            EntityType = 1 << 7,
            All = ~0
        }
        
        #endregion
        
        #region Serialized Fields
        
        [Header("Message Configuration")]
        [Tooltip("The message to log. Supports rich text formatting and placeholders.")]
        [TextArea(2, 5)]
        public string message = "State Entered";
        
        [Tooltip("Type of log message")]
        public DebugLogType logType = DebugLogType.Info;
        
        [Header("Trigger Configuration")]
        [Tooltip("When to trigger the log")]
        public LogTriggerPoint triggerPoint = LogTriggerPoint.OnEnter;
        
        [Header("Context Information")]
        [Tooltip("What context information to include in the log")]
        public ContextInfo includeContext = ContextInfo.EntityName | ContextInfo.StateName | ContextInfo.Timestamp;
        
        [Header("Formatting")]
        [Tooltip("Use rich text formatting (colors, bold, etc.)")]
        public bool useRichText = true;
        
        [Tooltip("Custom log format. Use placeholders: {message}, {entity}, {state}, {logic}, {time}, {pos}, {timeInState}, {timeInLogic}, {entityType}")]
        public string customFormat = "";
        
        [Header("Rate Limiting")]
        [Tooltip("Enable rate limiting to prevent log spam")]
        public bool enableRateLimit = false;
        
        [Tooltip("Minimum seconds between logs (rate limiting)")]
        [Min(0f)]
        public float rateLimitSeconds = 1f;
        
        [Tooltip("Maximum number of logs per second")]
        [Min(1)]
        public int maxLogsPerSecond = 10;
        
        [Header("Advanced Options")]
        [Tooltip("Include stack trace in error/exception logs")]
        public bool includeStackTrace = true;
        
        [Tooltip("Only log in editor (not in builds)")]
        public bool editorOnly = false;
        
        [Tooltip("Log object reference for debugging")]
        public bool logObjectReference = false;
        
        [Tooltip("Conditional logging - only log if this condition is true")]
        public bool conditionalLogging = false;
        
        [Tooltip("Condition value - log only when this is true")]
        public bool conditionValue = true;
        
        [Header("Performance")]
        [Tooltip("Track and log execution time")]
        public bool trackPerformance = false;
        
        [Header("Color Coding")]
        [Tooltip("Custom color for info logs (hex format, e.g., #00FF00)")]
        public string infoColor = "#00FFFF";
        
        [Tooltip("Custom color for warning logs")]
        public string warningColor = "#FFAA00";
        
        [Tooltip("Custom color for error logs")]
        public string errorColor = "#FF0000";
        
        #endregion
        
        #region Private Fields
        
        private Entity cachedEntity;
        private EntityLogic cachedLogic;
        private EntityFSMState cachedState;
        private float lastLogTime = -1f;
        private int logCount = 0;
        private float logCountResetTime = 0f;
        private Stopwatch performanceStopwatch;
        private bool hasLoggedOnce = false;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            CacheReferences();
            
            if (trackPerformance)
            {
                performanceStopwatch = new Stopwatch();
            }
        }
        
        #endregion
        
        #region EntityAction Overrides
        
        public override void OnStateEnter()
        {
            if (ShouldTrigger(LogTriggerPoint.OnEnter) || ShouldTrigger(LogTriggerPoint.OnEnterAndExit) || ShouldTrigger(LogTriggerPoint.OnAll))
            {
                LogMessage("OnStateEnter");
            }
        }
        
        public override void OnStateTick()
        {
            if (ShouldTrigger(LogTriggerPoint.OnTick) || ShouldTrigger(LogTriggerPoint.OnAll))
            {
                LogMessage("OnStateTick");
            }
        }
        
        public override void OnStateExit()
        {
            if (ShouldTrigger(LogTriggerPoint.OnExit) || ShouldTrigger(LogTriggerPoint.OnEnterAndExit) || ShouldTrigger(LogTriggerPoint.OnAll))
            {
                LogMessage("OnStateExit");
            }
        }
        
        #endregion
        
        #region Private Methods
        
        private void CacheReferences()
        {
            cachedEntity = GetComponentInParent<Entity>();
            cachedLogic = GetComponentInParent<EntityLogic>();
            
            if (cachedLogic != null)
            {
                cachedState = cachedLogic.CurrentState;
            }
        }
        
        private bool ShouldTrigger(LogTriggerPoint point)
        {
            return triggerPoint == point;
        }
        
        private void LogMessage(string triggerContext)
        {
            // Editor-only check
            if (editorOnly && !Application.isEditor)
            {
                return;
            }
            
            // Conditional logging check
            if (conditionalLogging && !conditionValue)
            {
                return;
            }
            
            // Rate limiting check
            if (enableRateLimit && !CanLog())
            {
                return;
            }
            
            // Update cached references
            CacheReferences();
            
            // Start performance tracking
            if (trackPerformance)
            {
                performanceStopwatch.Restart();
            }
            
            // Build the formatted message
            string formattedMessage = BuildFormattedMessage(triggerContext);
            
            // Log based on type
            LogByType(formattedMessage);
            
            // Stop performance tracking and log if enabled
            if (trackPerformance)
            {
                performanceStopwatch.Stop();
                UnityEngine.Debug.Log($"[Performance] {formattedMessage} - Execution Time: {performanceStopwatch.ElapsedMilliseconds}ms");
            }
            
            // Update rate limiting
            if (enableRateLimit)
            {
                UpdateRateLimit();
            }
            
            hasLoggedOnce = true;
        }
        
        private string BuildFormattedMessage(string triggerContext)
        {
            StringBuilder sb = new StringBuilder();
            
            // Use custom format if provided
            if (!string.IsNullOrEmpty(customFormat))
            {
                string formatted = customFormat;
                formatted = formatted.Replace("{message}", message);
                formatted = formatted.Replace("{entity}", GetEntityName());
                formatted = formatted.Replace("{state}", GetStateName());
                formatted = formatted.Replace("{logic}", GetLogicName());
                formatted = formatted.Replace("{time}", GetTimestamp());
                formatted = formatted.Replace("{pos}", GetPosition());
                formatted = formatted.Replace("{timeInState}", GetTimeInState());
                formatted = formatted.Replace("{timeInLogic}", GetTimeInLogic());
                formatted = formatted.Replace("{entityType}", GetEntityType());
                formatted = formatted.Replace("{trigger}", triggerContext);
                
                return ApplyRichText(formatted);
            }
            
            // Build message with context
            if (useRichText && logType == DebugLogType.Info)
            {
                sb.Append($"<color={infoColor}>");
            }
            else if (useRichText && logType == DebugLogType.Warning)
            {
                sb.Append($"<color={warningColor}>");
            }
            else if (useRichText && logType == DebugLogType.Error)
            {
                sb.Append($"<color={errorColor}>");
            }
            
            // Add context prefix
            if (includeContext != ContextInfo.None)
            {
                sb.Append("[");
                List<string> contextParts = new List<string>();
                
                if ((includeContext & ContextInfo.EntityName) != 0)
                {
                    contextParts.Add($"Entity: {GetEntityName()}");
                }
                
                if ((includeContext & ContextInfo.StateName) != 0)
                {
                    contextParts.Add($"State: {GetStateName()}");
                }
                
                if ((includeContext & ContextInfo.LogicName) != 0)
                {
                    contextParts.Add($"Logic: {GetLogicName()}");
                }
                
                if ((includeContext & ContextInfo.Timestamp) != 0)
                {
                    contextParts.Add($"Time: {GetTimestamp()}");
                }
                
                if ((includeContext & ContextInfo.Position) != 0)
                {
                    contextParts.Add($"Pos: {GetPosition()}");
                }
                
                if ((includeContext & ContextInfo.TimeInState) != 0)
                {
                    contextParts.Add($"TimeInState: {GetTimeInState()}");
                }
                
                if ((includeContext & ContextInfo.TimeInLogic) != 0)
                {
                    contextParts.Add($"TimeInLogic: {GetTimeInLogic()}");
                }
                
                if ((includeContext & ContextInfo.EntityType) != 0)
                {
                    contextParts.Add($"Type: {GetEntityType()}");
                }
                
                sb.Append(string.Join(" | ", contextParts));
                sb.Append("] ");
            }
            
            // Add main message
            sb.Append(message);
            
            // Add trigger context
            if (triggerPoint == LogTriggerPoint.OnAll)
            {
                sb.Append($" [{triggerContext}]");
            }
            
            // Close color tag if rich text is enabled
            if (useRichText && (logType == DebugLogType.Info || logType == DebugLogType.Warning || logType == DebugLogType.Error))
            {
                sb.Append("</color>");
            }
            
            // Add object reference if enabled
            if (logObjectReference && gameObject != null)
            {
                sb.Append($" [Object: {gameObject.name}]");
            }
            
            return sb.ToString();
        }
        
        private void LogByType(string formattedMessage)
        {
            UnityEngine.Object contextObject = logObjectReference ? gameObject : null;
            
            switch (logType)
            {
                case DebugLogType.Info:
                    UnityEngine.Debug.Log(formattedMessage, contextObject);
                    break;
                    
                case DebugLogType.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage, contextObject);
                    break;
                    
                case DebugLogType.Error:
                    if (includeStackTrace)
                    {
                        UnityEngine.Debug.LogError(formattedMessage + "\n" + Environment.StackTrace, contextObject);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(formattedMessage, contextObject);
                    }
                    break;
                    
                case DebugLogType.Assert:
                    UnityEngine.Debug.Assert(false, formattedMessage, contextObject);
                    break;
                    
                case DebugLogType.Exception:
                    Exception ex = new Exception(formattedMessage);
                    if (includeStackTrace)
                    {
                        UnityEngine.Debug.LogException(ex, contextObject);
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"Exception: {formattedMessage}", contextObject);
                    }
                    break;
                    
                default:
                    UnityEngine.Debug.Log(formattedMessage, contextObject);
                    break;
            }
        }
        
        private string ApplyRichText(string text)
        {
            if (!useRichText) return text;
            
            // Apply color based on log type
            string colorTag = "";
            string closeTag = "";
            
            switch (logType)
            {
                case DebugLogType.Info:
                    colorTag = $"<color={infoColor}>";
                    closeTag = "</color>";
                    break;
                case DebugLogType.Warning:
                    colorTag = $"<color={warningColor}>";
                    closeTag = "</color>";
                    break;
                case DebugLogType.Error:
                    colorTag = $"<color={errorColor}>";
                    closeTag = "</color>";
                    break;
            }
            
            return colorTag + text + closeTag;
        }
        
        #endregion
        
        #region Context Getters
        
        private string GetEntityName()
        {
            if (cachedEntity != null)
            {
                return cachedEntity.name;
            }
            return gameObject != null ? gameObject.name : "Unknown";
        }
        
        private string GetStateName()
        {
            if (cachedState != null)
            {
                return cachedState.stateName;
            }
            if (cachedLogic != null && cachedLogic.CurrentState != null)
            {
                return cachedLogic.CurrentState.stateName;
            }
            return "Unknown State";
        }
        
        private string GetLogicName()
        {
            if (cachedLogic != null)
            {
                return cachedLogic.name;
            }
            return "Unknown Logic";
        }
        
        private string GetTimestamp()
        {
            return Time.time.ToString("F2");
        }
        
        private string GetPosition()
        {
            if (transform != null)
            {
                Vector3 pos = transform.position;
                return $"({pos.x:F1}, {pos.y:F1}, {pos.z:F1})";
            }
            return "N/A";
        }
        
        private string GetTimeInState()
        {
            if (cachedLogic != null)
            {
                return cachedLogic.TimeInState.ToString("F2");
            }
            return "N/A";
        }
        
        private string GetTimeInLogic()
        {
            if (cachedLogic != null)
            {
                return cachedLogic.TimeInLogic.ToString("F2");
            }
            return "N/A";
        }
        
        private string GetEntityType()
        {
            if (cachedEntity != null)
            {
                return cachedEntity.EntityType.ToString();
            }
            return "N/A";
        }
        
        #endregion
        
        #region Rate Limiting
        
        private bool CanLog()
        {
            float currentTime = Time.time;
            
            // Check time-based rate limit
            if (lastLogTime >= 0f && (currentTime - lastLogTime) < rateLimitSeconds)
            {
                return false;
            }
            
            // Check per-second rate limit
            if (currentTime >= logCountResetTime + 1f)
            {
                logCount = 0;
                logCountResetTime = currentTime;
            }
            
            if (logCount >= maxLogsPerSecond)
            {
                return false;
            }
            
            return true;
        }
        
        private void UpdateRateLimit()
        {
            lastLogTime = Time.time;
            logCount++;
        }
        
        #endregion
        
        #region Public Utility Methods
        
        /// <summary>
        /// Manually trigger a log message
        /// </summary>
        public void TriggerLog()
        {
            LogMessage("Manual");
        }
        
        /// <summary>
        /// Check if this logger has logged at least once
        /// </summary>
        public bool HasLogged => hasLoggedOnce;
        
        /// <summary>
        /// Reset rate limiting counters
        /// </summary>
        public void ResetRateLimit()
        {
            lastLogTime = -1f;
            logCount = 0;
            logCountResetTime = 0f;
        }
        
        #endregion
    }
}