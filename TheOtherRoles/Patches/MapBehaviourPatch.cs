using HarmonyLib;
using System;
using Hazel;
using UnityEngine;
using UnityEngine.Rendering;


namespace TheOtherRoles.Patches {
	
	[HarmonyPatch(typeof(MapBehaviour))]
	class MapBehaviourPatch {

		[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate))]
		static bool Prefix(MapBehaviour __instance) {
			if (!MeetingHud.Instance) return true;  // Only run in meetings, and then set the Position of the HerePoint to the Position before the Meeting!

			if (!ShipStatus.Instance) {
				return false;
			}
			Vector3 vector = AntiTeleport.position != null? AntiTeleport.position : PlayerControl.LocalPlayer.transform.position;
			vector /= ShipStatus.Instance.MapScale;
			vector.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
			vector.z = -1f;
			__instance.HerePoint.transform.localPosition = vector;

			return false;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close))]		
		static bool Prefix2(MapBehaviour __instance) {
			if (!MeetingHud.Instance) return true;  // Only run in meetings, and then dont enable the hud like it would happen ingame!
			__instance.gameObject.SetActive(false);
			__instance.countOverlay.gameObject.SetActive(false);
			__instance.infectedOverlay.gameObject.SetActive(false);
			__instance.taskOverlay.Hide();
			__instance.HerePoint.enabled = true;
			return false;
		}

	}
}