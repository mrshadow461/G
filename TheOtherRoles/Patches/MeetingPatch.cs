using HarmonyLib;
using Hazel;
using System.Collections.Generic;
using System.Linq;
using UnhollowerBaseLib;
using static TheOtherRoles.TheOtherRoles;
using static TheOtherRoles.MapOptions;
using System.Collections;
using System;
using System.Text;
using UnityEngine;
using System.Reflection;

namespace TheOtherRoles.Patches {
    [HarmonyPatch]
    class MeetingHudPatch {
        static bool[] selections;
        static SpriteRenderer[] renderers;
        private static GameData.PlayerInfo target = null;
        private const float scale = 0.65f;

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
        class MeetingCalculateVotesPatch {
            private static Dictionary<byte, int> CalculateVotes(MeetingHud __instance) {
                Dictionary<byte, int> dictionary = new Dictionary<byte, int>();
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.VotedFor != 252 && playerVoteArea.VotedFor != 255 && playerVoteArea.VotedFor != 254) {
                        PlayerControl player = Helpers.playerById((byte)playerVoteArea.TargetPlayerId);
                        if (player == null || player.Data == null || player.Data.IsDead || player.Data.Disconnected) continue;

                        int currentVotes;
                        int additionalVotes = (Mayor.mayor != null && Mayor.mayor.PlayerId == playerVoteArea.TargetPlayerId
                                               || Doppelganger.doppelganger != null && Doppelganger.copiedRole == RoleInfo.mayor
                                               && Doppelganger.doppelganger.PlayerId == playerVoteArea.TargetPlayerId) ? 2 : 1; // Mayor vote
                        if (dictionary.TryGetValue(playerVoteArea.VotedFor, out currentVotes))
                            dictionary[playerVoteArea.VotedFor] = currentVotes + additionalVotes;
                        else
                            dictionary[playerVoteArea.VotedFor] = additionalVotes;
                    }
                }
                // Swapper swap votes
                PlayerVoteArea swapped1 = null;
                PlayerVoteArea swapped2 = null;
                if (Swapper.swapper != null && !Swapper.swapper.Data.IsDead) {
                    
                    foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                        if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                        if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                    }

                    if (swapped1 != null && swapped2 != null) {
                        if (!dictionary.ContainsKey(swapped1.TargetPlayerId)) dictionary[swapped1.TargetPlayerId] = 0;
                        if (!dictionary.ContainsKey(swapped2.TargetPlayerId)) dictionary[swapped2.TargetPlayerId] = 0;
                        int tmp = dictionary[swapped1.TargetPlayerId];
                        dictionary[swapped1.TargetPlayerId] = dictionary[swapped2.TargetPlayerId];
                        dictionary[swapped2.TargetPlayerId] = tmp;
                    }
                }
                // Doppelganger Swapper swap votes (again)
                if (Doppelganger.doppelganger != null && !Doppelganger.doppelganger.Data.IsDead && Doppelganger.copiedRole == RoleInfo.swapper)
                {
                    PlayerVoteArea swapped3 = null;
                    PlayerVoteArea swapped4 = null;
                    foreach (PlayerVoteArea playerVoteArea in __instance.playerStates)
                    {
                        if (playerVoteArea.TargetPlayerId == Doppelganger.swapperPlayerId1) swapped3 = playerVoteArea;
                        if (playerVoteArea.TargetPlayerId == Doppelganger.swapperPlayerId2) swapped4 = playerVoteArea;
                    }

                    bool doubleSwap = swapped1 == swapped3 && swapped2 == swapped4 || swapped1 == swapped4 && swapped2 == swapped3;  // swap back and forth
                    bool chainedSwap = !doubleSwap && (swapped1 == swapped3 || swapped2 == swapped3 || swapped1 == swapped4 || swapped2 == swapped4);

                    if (swapped3 != null && swapped4 != null && !chainedSwap)
                    {
                        if (!dictionary.ContainsKey(swapped3.TargetPlayerId)) dictionary[swapped3.TargetPlayerId] = 0;
                        if (!dictionary.ContainsKey(swapped4.TargetPlayerId)) dictionary[swapped4.TargetPlayerId] = 0;
                        int tmp = dictionary[swapped3.TargetPlayerId];
                        dictionary[swapped3.TargetPlayerId] = dictionary[swapped4.TargetPlayerId];
                        dictionary[swapped4.TargetPlayerId] = tmp;
                    }

                    // only relevant, if the doppelganger swaps exactly one of the swapper's targets resulting in a chained swap.
                    if (chainedSwap && swapped3 != null && swapped4 != null)
                    {
                        PlayerVoteArea middle;
                        PlayerVoteArea end1;
                        PlayerVoteArea end2;

                        // decide who is who in the swap: Swapper swaps first, and doppelganger second
                        // this means, we only have to swap the "ends" of the chain, if it is a chain"
                        if (swapped1 == swapped3 || swapped1 == swapped4)
                        {
                            middle = swapped1;
                            end1 = swapped2;
                            if (swapped1 == swapped3)
                            {
                                end2 = swapped4;
                            }
                            else
                            {
                                end2 = swapped3;
                            }
                        }
                        else  // swapped 2 is middle
                        {
                            middle = swapped2;
                            end1 = swapped1;
                            if (swapped2 == swapped3)
                            {
                                end2 = swapped4;
                            }
                            else
                            {
                                end2 = swapped3;
                            }
                        }
                        // Do three way swap of votes!
                        if (!dictionary.ContainsKey(middle.TargetPlayerId)) dictionary[middle.TargetPlayerId] = 0;
                        if (!dictionary.ContainsKey(end1.TargetPlayerId)) dictionary[end1.TargetPlayerId] = 0;
                        if (!dictionary.ContainsKey(end2.TargetPlayerId)) dictionary[end2.TargetPlayerId] = 0;
                        int tmp = dictionary[end1.TargetPlayerId];
                        // dictionary[middle.TargetPlayerId] = dictionary[end1.TargetPlayerId];
                        dictionary[end1.TargetPlayerId] = dictionary[end2.TargetPlayerId];
                        dictionary[end2.TargetPlayerId] = tmp;
                    }
                }
                return dictionary;
            }


            static bool Prefix(MeetingHud __instance) {
                if (__instance.playerStates.All((PlayerVoteArea ps) => ps.AmDead || ps.DidVote)) {
                    // If skipping is disabled, replace skipps/no-votes with self vote
                    if (target == null && blockSkippingInEmergencyMeetings && noVoteIsSelfVote) {
                        foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                            if (playerVoteArea.VotedFor < 0) playerVoteArea.VotedFor = playerVoteArea.TargetPlayerId; // TargetPlayerId
                        }
                    }

			        Dictionary<byte, int> self = CalculateVotes(__instance);
                    bool tie;
			        KeyValuePair<byte, int> max = self.MaxPair(out tie);
                    GameData.PlayerInfo exiled = GameData.Instance.AllPlayers.ToArray().FirstOrDefault(v => !tie && v.PlayerId == max.Key && !v.IsDead);

                    MeetingHud.VoterState[] array = new MeetingHud.VoterState[__instance.playerStates.Length];
                    for (int i = 0; i < __instance.playerStates.Length; i++)
                    {
                        PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                        array[i] = new MeetingHud.VoterState {
                            VoterId = playerVoteArea.TargetPlayerId,
                            VotedForId = playerVoteArea.VotedFor
                        };
                    }

                    // RPCVotingComplete
                    __instance.RpcVotingComplete(array, exiled, tie);
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.BloopAVoteIcon))]
        class MeetingHudBloopAVoteIconPatch {
            public static bool Prefix(MeetingHud __instance, [HarmonyArgument(0)]GameData.PlayerInfo voterPlayer, [HarmonyArgument(1)]int index, [HarmonyArgument(2)]Transform parent) {
                SpriteRenderer spriteRenderer = UnityEngine.Object.Instantiate<SpriteRenderer>(__instance.PlayerVotePrefab);
                if (!PlayerControl.GameOptions.AnonymousVotes || (PlayerControl.LocalPlayer.Data.IsDead && MapOptions.ghostsSeeVotes))
                    PlayerControl.SetPlayerMaterialColors(voterPlayer.DefaultOutfit.ColorId, spriteRenderer);
                else
                    PlayerControl.SetPlayerMaterialColors(Palette.DisabledGrey, spriteRenderer);
                spriteRenderer.transform.SetParent(parent);
                spriteRenderer.transform.localScale = Vector3.zero;
                __instance.StartCoroutine(Effects.Bloop((float)index * 0.3f, spriteRenderer.transform, 1f, 0.5f));
                parent.GetComponent<VoteSpreader>().AddVote(spriteRenderer);
                return false;
            }
        } 

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.PopulateResults))]
        class MeetingHudPopulateVotesPatch {
            
            static bool Prefix(MeetingHud __instance, Il2CppStructArray<MeetingHud.VoterState> states) {
                // Swapper swap

                PlayerVoteArea swapped1 = null;
                PlayerVoteArea swapped2 = null;
                PlayerVoteArea swapped3 = null;
                PlayerVoteArea swapped4 = null;
                PlayerVoteArea swapThreeway1 = null;
                PlayerVoteArea swapThreeway2 = null;
                PlayerVoteArea swapThreeway3 = null;

                foreach (PlayerVoteArea playerVoteArea in __instance.playerStates) {
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId1) swapped1 = playerVoteArea;
                    if (playerVoteArea.TargetPlayerId == Swapper.playerId2) swapped2 = playerVoteArea;
                }
                bool doSwap = swapped1 != null && swapped2 != null && Swapper.swapper != null && !Swapper.swapper.Data.IsDead;
                foreach (PlayerVoteArea playerVoteArea in __instance.playerStates)
                {
                    if (playerVoteArea.TargetPlayerId == Doppelganger.swapperPlayerId1) swapped3 = playerVoteArea;
                    if (playerVoteArea.TargetPlayerId == Doppelganger.swapperPlayerId2) swapped4 = playerVoteArea;
                }
                bool doSwapAgain = swapped3 != null && swapped4 != null && Doppelganger.doppelganger != null && Doppelganger.copiedRole == RoleInfo.swapper && !Doppelganger.doppelganger.Data.IsDead;
                bool doSwapThreeway = false;  // Use swapped 1, swapped 2 and swapped 3!
                if (doSwap && doSwapAgain)  // There might be a conflict here!
                {
                    if (swapped1 == swapped3 && swapped2 == swapped4 || swapped1 == swapped4 && swapped2 == swapped3)  // Swap back and forth -> no swap!!
                    {
                        doSwap = doSwapAgain = false;
                    }
                    else if (swapped1 == swapped3 || swapped2 == swapped3 || swapped1 == swapped4 || swapped2 == swapped4)    
                    {
                        doSwap = doSwapAgain = false;
                        doSwapThreeway = true;
                        // we swap 1 -> 3,  2 -> 1, 3 -> 2
                        if (swapped1 == swapped3 || swapped2 == swapped3)
                        {
                            if (swapped1 == swapped3) {
                                swapThreeway1 = swapped2;
                                swapThreeway2 = swapped1;
                            } else
                            {
                                swapThreeway1 = swapped1;
                                swapThreeway2 = swapped2;
                            }
                            swapThreeway3 = swapped4;
                        } else if (swapped1 == swapped4 || swapped2 == swapped4)
                        {
                            swapThreeway1 = swapped1;
                            swapThreeway2 = swapped4;
                            if (swapped1 == swapped4)
                            {
                                swapThreeway1 = swapped2;
                            }
                            swapThreeway3 = swapped3;
                        }
                    }
                }
                if (doSwap)
                {
                    __instance.StartCoroutine(Effects.Slide3D(swapped1.transform, swapped1.transform.localPosition, swapped2.transform.localPosition, 1.5f));
                    __instance.StartCoroutine(Effects.Slide3D(swapped2.transform, swapped2.transform.localPosition, swapped1.transform.localPosition, 1.5f));
                }
                if (doSwapAgain)
                {
                    __instance.StartCoroutine(Effects.Slide3D(swapped3.transform, swapped3.transform.localPosition, swapped4.transform.localPosition, 1.5f));
                    __instance.StartCoroutine(Effects.Slide3D(swapped4.transform, swapped4.transform.localPosition, swapped3.transform.localPosition, 1.5f));
                }
                if (doSwapThreeway)
                {
                    __instance.StartCoroutine(Effects.Slide3D(swapThreeway1.transform, swapThreeway1.transform.localPosition, swapThreeway3.transform.localPosition, 1.5f));
                    __instance.StartCoroutine(Effects.Slide3D(swapThreeway2.transform, swapThreeway2.transform.localPosition, swapThreeway1.transform.localPosition, 1.5f));
                    __instance.StartCoroutine(Effects.Slide3D(swapThreeway3.transform, swapThreeway3.transform.localPosition, swapThreeway2.transform.localPosition, 1.5f));
                }



                __instance.TitleText.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.MeetingVotingResults, new Il2CppReferenceArray<Il2CppSystem.Object>(0));
                int num = 0;
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    byte targetPlayerId = playerVoteArea.TargetPlayerId;
                    // Swapper change playerVoteArea that gets the votes
                    if (doSwap && playerVoteArea.TargetPlayerId == swapped1.TargetPlayerId) playerVoteArea = swapped2;
                    else if (doSwap && playerVoteArea.TargetPlayerId == swapped2.TargetPlayerId) playerVoteArea = swapped1;
                    if (doSwapAgain && playerVoteArea.TargetPlayerId == swapped3.TargetPlayerId) playerVoteArea = swapped4;
                    else if (doSwapAgain && playerVoteArea.TargetPlayerId == swapped4.TargetPlayerId) playerVoteArea = swapped3;

                    if (doSwapThreeway && playerVoteArea.TargetPlayerId == swapThreeway1.TargetPlayerId) playerVoteArea = swapThreeway2;
                    else if (doSwapThreeway && playerVoteArea.TargetPlayerId == swapThreeway2.TargetPlayerId) playerVoteArea = swapThreeway3;
                    else if (doSwapThreeway && playerVoteArea.TargetPlayerId == swapThreeway3.TargetPlayerId) playerVoteArea = swapThreeway1;

                    playerVoteArea.ClearForResults();
                    int num2 = 0;
                    bool mayorFirstVoteDisplayed = false;
                    bool doppelgangerMayorFirstVoteDisplayed = false;
                    for (int j = 0; j < states.Length; j++) {
                        MeetingHud.VoterState voterState = states[j];
                        GameData.PlayerInfo playerById = GameData.Instance.GetPlayerById(voterState.VoterId);
                        if (playerById == null) {
                            Debug.LogError(string.Format("Couldn't find player info for voter: {0}", voterState.VoterId));
                        } else if (i == 0 && voterState.SkippedVote && !playerById.IsDead) {
                            __instance.BloopAVoteIcon(playerById, num, __instance.SkippedVoting.transform);
                            num++;
                        }
                        else if (voterState.VotedForId == targetPlayerId && !playerById.IsDead) {
                            __instance.BloopAVoteIcon(playerById, num2, playerVoteArea.transform);
                            num2++;
                        }

                        // Major vote, redo this iteration to place a second vote
                        if (Mayor.mayor != null && voterState.VoterId == (sbyte)Mayor.mayor.PlayerId && !mayorFirstVoteDisplayed) {
                            mayorFirstVoteDisplayed = true;
                            j--;    
                        }
                        if (Doppelganger.doppelganger != null && voterState.VoterId == (sbyte)Doppelganger.doppelganger.PlayerId
                            && !doppelgangerMayorFirstVoteDisplayed && Doppelganger.copiedRole == RoleInfo.mayor)
                        {
                            doppelgangerMayorFirstVoteDisplayed = true;
                            j--;
                        }
                    }
                }
                return false;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.VotingComplete))]
        class MeetingHudVotingCompletedPatch {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)]byte[] states, [HarmonyArgument(1)]GameData.PlayerInfo exiled, [HarmonyArgument(2)]bool tie)
            {
                // Reset swapper values
                Swapper.playerId1 = Byte.MaxValue;
                Swapper.playerId2 = Byte.MaxValue;
                Doppelganger.swapperPlayerId1 = Byte.MaxValue;
                Doppelganger.swapperPlayerId2 = Byte.MaxValue;

                // Lovers & Pursuer save next to be exiled, because RPC of ending game comes before RPC of exiled
                Lovers.notAckedExiledIsLover = false;
                Pursuer.notAckedExiled = false;
                if (exiled != null) {
                    Lovers.notAckedExiledIsLover = ((Lovers.lover1 != null && Lovers.lover1.PlayerId == exiled.PlayerId) || (Lovers.lover2 != null && Lovers.lover2.PlayerId == exiled.PlayerId));
                    Pursuer.notAckedExiled = (Pursuer.pursuer != null && Pursuer.pursuer.PlayerId == exiled.PlayerId);
                }
                    
                               
            }
        }


        static void swapperOnClick(int i, MeetingHud __instance) {
            if (__instance.state == MeetingHud.VoteStates.Results) return;
            if (__instance.playerStates[i].AmDead) return;

            int selectedCount = selections.Where(b => b).Count();
            SpriteRenderer renderer = renderers[i];

            if (selectedCount == 0) {
                renderer.color = Color.green;
                selections[i] = true;
            } else if (selectedCount == 1) {
                if (selections[i]) {
                    renderer.color = Color.red;
                    selections[i] = false;
                } else {
                    selections[i] = true;
                    renderer.color = Color.green;   
                    
                    PlayerVoteArea firstPlayer = null;
                    PlayerVoteArea secondPlayer = null;
                    for (int A = 0; A < selections.Length; A++) {
                        if (selections[A]) {
                            if (firstPlayer != null) {
                                secondPlayer = __instance.playerStates[A];
                                break;
                            } else {
                                firstPlayer = __instance.playerStates[A];
                            }
                        }
                    }

                    if (firstPlayer != null && secondPlayer != null) {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwapperSwap, Hazel.SendOption.Reliable, -1);
                        writer.Write((byte)firstPlayer.TargetPlayerId);
                        writer.Write((byte)secondPlayer.TargetPlayerId);
                        writer.Write((byte)PlayerControl.LocalPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);

                        RPCProcedure.swapperSwap((byte)firstPlayer.TargetPlayerId, (byte)secondPlayer.TargetPlayerId, PlayerControl.LocalPlayer.PlayerId);
                    }
                }
            }
        }

        private static GameObject guesserUI;
        static void guesserOnClick(int buttonTarget, MeetingHud __instance) {
            if (guesserUI != null || !(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted)) return;
            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(false));

            Transform container = UnityEngine.Object.Instantiate(__instance.transform.FindChild("PhoneUI"), __instance.transform);
            container.transform.localPosition = new Vector3(0, 0, -5f);
            guesserUI = container.gameObject;

            int i = 0;
            var buttonTemplate = __instance.playerStates[0].transform.FindChild("votePlayerBase");
            var maskTemplate = __instance.playerStates[0].transform.FindChild("MaskArea");
            var smallButtonTemplate = __instance.playerStates[0].Buttons.transform.Find("CancelButton");
            var textTemplate = __instance.playerStates[0].NameText;

            Transform exitButtonParent = (new GameObject()).transform;
            exitButtonParent.SetParent(container);
            Transform exitButton = UnityEngine.Object.Instantiate(buttonTemplate.transform, exitButtonParent);
            Transform exitButtonMask = UnityEngine.Object.Instantiate(maskTemplate, exitButtonParent);
            exitButton.gameObject.GetComponent<SpriteRenderer>().sprite = smallButtonTemplate.GetComponent<SpriteRenderer>().sprite;
            exitButtonParent.transform.localPosition = new Vector3(2.725f, 2.1f, -5);
            exitButtonParent.transform.localScale = new Vector3(0.25f, 0.9f, 1);
            exitButton.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
            exitButton.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() => {
                __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                UnityEngine.Object.Destroy(container.gameObject);
            }));

            List<Transform> buttons = new List<Transform>();
            Transform selectedButton = null;

            foreach (RoleInfo roleInfo in RoleInfo.allRoleInfos) {
                // Have to swap this around, as the Doppelganger can only copy the nice guesser!
                RoleId guesserRole = (Guesser.evilGuesser != null && PlayerControl.LocalPlayer.PlayerId == Guesser.evilGuesser.PlayerId) ? RoleId.EvilGuesser :  RoleId.NiceGuesser;
                if (roleInfo.roleId == RoleId.Lover || roleInfo.roleId == guesserRole || roleInfo == RoleInfo.niceMini || (!Guesser.evilGuesserCanGuessSpy && guesserRole == RoleId.EvilGuesser && roleInfo.roleId == RoleId.Spy)) continue; // Not guessable roles
                Transform buttonParent = (new GameObject()).transform;
                buttonParent.SetParent(container);
                Transform button = UnityEngine.Object.Instantiate(buttonTemplate, buttonParent);
                Transform buttonMask = UnityEngine.Object.Instantiate(maskTemplate, buttonParent);
                TMPro.TextMeshPro label = UnityEngine.Object.Instantiate(textTemplate, button);
                button.GetComponent<SpriteRenderer>().sprite = DestroyableSingleton<HatManager>.Instance.AllNamePlates[0].Image;
                buttons.Add(button);
                int row = i/5, col = i%5;
                buttonParent.localPosition = new Vector3(-3.47f + 1.75f * col, 1.5f - 0.45f * row, -5);
                buttonParent.localScale = new Vector3(0.55f, 0.55f, 1f);
                label.text = Helpers.cs(roleInfo.color, roleInfo.name);
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.transform.localPosition = new Vector3(0, 0, label.transform.localPosition.z);
                label.transform.localScale *= 1.7f;
                int copiedIndex = i;

                button.GetComponent<PassiveButton>().OnClick.RemoveAllListeners();
                button.GetComponent<PassiveButton>().OnClick.AddListener((UnityEngine.Events.UnityAction)(() =>
                {
                    if (selectedButton != button)
                    {
                        selectedButton = button;
                        buttons.ForEach(x => x.GetComponent<SpriteRenderer>().color = x == selectedButton ? Color.red : Color.white);
                    } else {
                        PlayerControl focusedTarget = Helpers.playerById((byte)__instance.playerStates[buttonTarget].TargetPlayerId);
                        if (!(__instance.state == MeetingHud.VoteStates.Voted || __instance.state == MeetingHud.VoteStates.NotVoted) || focusedTarget == null || Guesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) <= 0) return;

                        if (!Guesser.killsThroughShield && (focusedTarget == Medic.shielded || focusedTarget == Doppelganger.medicShielded)) { // Depending on the options, shooting the shielded player will not allow the guess, notifiy everyone about the kill attempt and close the window
                            __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true)); 
                            UnityEngine.Object.Destroy(container.gameObject);

                            MessageWriter murderAttemptWriter = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.ShieldedMurderAttempt, Hazel.SendOption.Reliable, -1);
                            murderAttemptWriter.Write(target.PlayerId);
                            AmongUsClient.Instance.FinishRpcImmediately(murderAttemptWriter);
                            RPCProcedure.shieldedMurderAttempt(target.PlayerId);
                            return;
                        }

                        var mainRoleInfo = RoleInfo.getRoleInfoForPlayer(focusedTarget).FirstOrDefault();
                        if (mainRoleInfo == null) return;
                        PlayerControl dyingTarget = focusedTarget;

                        // Add doppelganger as guessable role, even after copy!
                        if (!(mainRoleInfo == roleInfo || roleInfo == RoleInfo.doppelganger && Doppelganger.doppelganger != null && focusedTarget == Doppelganger.doppelganger))
                        {
                            dyingTarget = PlayerControl.LocalPlayer;  // Guess is incorrect!
                        }

                        // Reset the GUI
                        __instance.playerStates.ToList().ForEach(x => x.gameObject.SetActive(true));
                        UnityEngine.Object.Destroy(container.gameObject);
                        if (Guesser.hasMultipleShotsPerMeeting && Guesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 1 && dyingTarget != PlayerControl.LocalPlayer)
                            __instance.playerStates.ToList().ForEach(x => { if (x.TargetPlayerId == dyingTarget.PlayerId && x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });
                        else
                            __instance.playerStates.ToList().ForEach(x => { if (x.transform.FindChild("ShootButton") != null) UnityEngine.Object.Destroy(x.transform.FindChild("ShootButton").gameObject); });

                        // Shoot player and send chat info if activated
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.GuesserShoot, Hazel.SendOption.Reliable, -1);
                        writer.Write(PlayerControl.LocalPlayer.PlayerId);
                        writer.Write(dyingTarget.PlayerId);
                        writer.Write(focusedTarget.PlayerId);
                        writer.Write((byte)roleInfo.roleId);
                        writer.Write((byte)PlayerControl.LocalPlayer.PlayerId);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                        RPCProcedure.guesserShoot(PlayerControl.LocalPlayer.PlayerId, dyingTarget.PlayerId, focusedTarget.PlayerId, (byte)roleInfo.roleId);
                    }
                }));

                i++;
            }
            container.transform.localScale *= 0.75f;
        }

        [HarmonyPatch(typeof(PlayerVoteArea), nameof(PlayerVoteArea.Select))]
        class PlayerVoteAreaSelectPatch {
            static bool Prefix(MeetingHud __instance) {
                return !(PlayerControl.LocalPlayer != null && (Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) || Doppelganger.isRoleAndLocalPlayer(RoleInfo.goodGuesser)) && guesserUI != null);
            }
        }


        static void populateButtonsPostfix(MeetingHud __instance) {
            // Add Swapper Buttons
            if (Swapper.swapper != null && PlayerControl.LocalPlayer == Swapper.swapper && !Swapper.swapper.Data.IsDead ||
                Doppelganger.isRoleAndLocalPlayer(RoleInfo.swapper) && !Doppelganger.doppelganger.Data.IsDead) {
                selections = new bool[__instance.playerStates.Length];
                renderers = new SpriteRenderer[__instance.playerStates.Length];

                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || (PlayerControl.LocalPlayer == Swapper.swapper && playerVoteArea.TargetPlayerId == Swapper.swapper.PlayerId && Swapper.canOnlySwapOthers)) continue;
                    if (playerVoteArea.AmDead || (PlayerControl.LocalPlayer == Doppelganger.doppelganger && playerVoteArea.TargetPlayerId == Doppelganger.doppelganger.PlayerId && Swapper.canOnlySwapOthers)) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject checkbox = UnityEngine.Object.Instantiate(template);
                    checkbox.transform.SetParent(playerVoteArea.transform);
                    checkbox.transform.position = template.transform.position;
                    checkbox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                    SpriteRenderer renderer = checkbox.GetComponent<SpriteRenderer>();
                    renderer.sprite = Swapper.getCheckSprite();
                    renderer.color = Color.red;

                    PassiveButton button = checkbox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => swapperOnClick(copiedIndex, __instance)));
                    
                    selections[i] = false;
                    renderers[i] = renderer;
                }
            }

            //Fix visor in Meetings 
            foreach (PlayerVoteArea pva in __instance.playerStates) {
                if(pva.PlayerIcon != null && pva.PlayerIcon.VisorSlot != null){
                    pva.PlayerIcon.VisorSlot.transform.position += new Vector3(0, 0, -1f);
                }
            }

            // Add overlay for spelled players
            if (Witch.witch != null && Witch.futureSpelled != null) {
                foreach (PlayerVoteArea pva in __instance.playerStates) {
                    if (Witch.futureSpelled.Any(x => x.PlayerId == pva.TargetPlayerId)) {
                        SpriteRenderer rend = (new GameObject()).AddComponent<SpriteRenderer>();
                        rend.transform.SetParent(pva.transform);
                        rend.gameObject.layer = pva.Megaphone.gameObject.layer;
                        rend.transform.localPosition = new Vector3(-1.21f, -0.12f, -1f);
                        rend.sprite = Witch.getSpelledOverlaySprite();
                    }
                }
            }

            // Add Guesser Buttons
            if ((Guesser.isGuesser(PlayerControl.LocalPlayer.PlayerId) || Doppelganger.isRoleAndLocalPlayer(RoleInfo.goodGuesser)) && !PlayerControl.LocalPlayer.Data.IsDead && Guesser.remainingShots(PlayerControl.LocalPlayer.PlayerId) > 0) {
                for (int i = 0; i < __instance.playerStates.Length; i++) {
                    PlayerVoteArea playerVoteArea = __instance.playerStates[i];
                    if (playerVoteArea.AmDead || playerVoteArea.TargetPlayerId == PlayerControl.LocalPlayer.PlayerId) continue;

                    GameObject template = playerVoteArea.Buttons.transform.Find("CancelButton").gameObject;
                    GameObject targetBox = UnityEngine.Object.Instantiate(template, playerVoteArea.transform);
                    targetBox.name = "ShootButton";
                    targetBox.transform.localPosition = new Vector3(-0.95f, 0.03f, -1f);
                    SpriteRenderer renderer = targetBox.GetComponent<SpriteRenderer>();
                    renderer.sprite = Guesser.getTargetSprite();
                    PassiveButton button = targetBox.GetComponent<PassiveButton>();
                    button.OnClick.RemoveAllListeners();
                    int copiedIndex = i;
                    button.OnClick.AddListener((UnityEngine.Events.UnityAction)(() => guesserOnClick(copiedIndex, __instance)));
                }
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.ServerStart))]
        class MeetingServerStartPatch {
            static void Postfix(MeetingHud __instance)
            {
                populateButtonsPostfix(__instance);
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Deserialize))]
        class MeetingDeserializePatch {
            static void Postfix(MeetingHud __instance, [HarmonyArgument(0)]MessageReader reader, [HarmonyArgument(1)]bool initialState)
            {
                // Add swapper buttons
                if (initialState) {
                    populateButtonsPostfix(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoStartMeeting))]
        class StartMeetingPatch {
            public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)]GameData.PlayerInfo meetingTarget) {
                // Medium meeting start time
                Medium.meetingStartTime = DateTime.UtcNow;
                // Reset vampire bitten
                Vampire.bitten = null;
                // Count meetings
                if (meetingTarget == null) meetingsCount++;
                // Save the meeting target
                target = meetingTarget;
            }
        }

        [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
        class MeetingHudUpdatePatch {
            static void Postfix(MeetingHud __instance) {
                // Deactivate skip Button if skipping on emergency meetings is disabled
                if (target == null && blockSkippingInEmergencyMeetings)
                    __instance.SkipVoteButton.gameObject.SetActive(false);
            }
        }
    }
}
