using UnityEngine;
using AHAKuo.Signalia.Framework;

namespace AHAKuo.Signalia.GameSystems.Localization.Internal
{
	/// <summary>
	/// A utility component that manually triggers language refresh events.
	/// Useful for forcing all localized components to update, or for triggering refreshes from UnityEvents.
	/// </summary>
	[AddComponentMenu("Signalia/Game Systems/Localization/Signalia | Localization Refresher")]
	public class LocalizationRefresher : MonoBehaviour
	{
		//[Header("Automatic Refresh")]
		[Tooltip("When to automatically trigger a localization refresh.")]
		[SerializeField] private RefreshTiming refreshTiming = RefreshTiming.None;
		
		private void Awake()
		{
			// watchman init
			Watchman.Watch();
			
			if (refreshTiming == RefreshTiming.OnAwake)
			{
				Refresh();
			}
		}
		
		private void Start()
		{
			if (refreshTiming == RefreshTiming.OnStart)
			{
				Refresh();
			}
		}
		
		/// <summary>
		/// Triggers a localization refresh by firing the language changed event.
		/// This will cause all LocalizedText and LocalizedImage components to update.
		/// Can be called from UnityEvents (e.g., button clicks).
		/// </summary>
		public void Refresh()
		{
			LocalizationRuntime.FireLanguageChangedEvent();
			
			if (RuntimeValues.Debugging.IsDebugging)
			{
				Debug.Log("[Signalia LocalizationRefresher] Triggered localization refresh.", this);
			}
		}
		
		/// <summary>
		/// Refreshes all localized content after a short delay.
		/// Useful if you need to wait for other systems to initialize first.
		/// </summary>
		/// <param name="delaySeconds">The delay in seconds before refreshing</param>
		public void RefreshDelayed(float delaySeconds)
		{
			if (delaySeconds <= 0)
			{
				Refresh();
			}
			else
			{
				StartCoroutine(RefreshAfterDelay(delaySeconds));
			}
		}
		
		private System.Collections.IEnumerator RefreshAfterDelay(float delaySeconds)
		{
			yield return new UnityEngine.WaitForSeconds(delaySeconds);
			Refresh();
		}
		
		/// <summary>
		/// Defines when the refresh should occur automatically.
		/// </summary>
		public enum RefreshTiming
		{
			/// <summary>No automatic refresh</summary>
			None,
			/// <summary>Refresh on Awake</summary>
			OnAwake,
			/// <summary>Refresh on Start</summary>
			OnStart
		}
	}
}
