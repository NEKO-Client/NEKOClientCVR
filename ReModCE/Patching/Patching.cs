﻿using System.Collections.Generic;
using System.Linq;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using ABI_RC.Core.InteractionSystem;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using ABI_RC.Core.Savior;
using HarmonyLib;
using MelonLoader;
using NEKOClient.Reflection;

namespace NEKOClient.Patching
{

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class Patching
    {
        private static readonly HarmonyLib.Harmony _instance = new HarmonyLib.Harmony("VRCPlates");
        private static readonly MethodInfo? _targetMethod = typeof(List<CVRPlayerEntity>).GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
        private static readonly MethodInfo? _onPlayerJoin = typeof(Patching).GetMethod(nameof(OnPlayerJoin), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly FieldInfo _playerEntity = typeof(CVRPlayerManager).GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Instance).Single(t => t.GetField("p") != null).GetField("p");

        // Thank you Bono for the the UserJoin Transpiler.
        public static void Init()
        {
            var _ReloadFriends = typeof(ViewManager).GetMethod("RequestFriendsListTask", BindingFlags.Public | BindingFlags.Instance);
            var _onReloadFriends =
                typeof(Patching).GetMethod(nameof(OnReloadFriends), BindingFlags.NonPublic | BindingFlags.Static);

            var _HandleRelations =
                typeof(ViewManager).GetMethod("HandleRelations", BindingFlags.NonPublic | BindingFlags.Instance);
            var _onRelations =
                typeof(Patching).GetMethod(nameof(OnRelations), BindingFlags.NonPublic | BindingFlags.Static);

            var _SettingsChanged = typeof(MetaPort).GetMethod("SettingsChangedHandler", BindingFlags.NonPublic | BindingFlags.Instance);
            var _onSettingsChanged =
                typeof(Patching).GetMethod(nameof(OnSettingsChanged), BindingFlags.NonPublic | BindingFlags.Static);

            var _TryCreatePlayer = typeof(CVRPlayerManager).GetMethod(nameof(CVRPlayerManager.TryCreatePlayer),
                BindingFlags.Instance | BindingFlags.Public);
            var _onTryCreatePlayer =
                typeof(Patching).GetMethod(nameof(Transpiler), BindingFlags.NonPublic | BindingFlags.Static);

            var _PlayerLeave = typeof(CVRPlayerEntity).GetMethod("Recycle", BindingFlags.Public | BindingFlags.Instance);
            var _onPlayerLeave =
                typeof(Patching).GetMethod(nameof(OnPlayerLeave), BindingFlags.NonPublic | BindingFlags.Static);

            var _AvatarInstantiated =
                typeof(PuppetMaster).GetMethod("AvatarInstantiated", BindingFlags.Public | BindingFlags.Instance);
            var _onAvatarInstantiated =
                typeof(Patching).GetMethod(nameof(OnAvatarInstantiated), BindingFlags.NonPublic | BindingFlags.Static);

            var _ReloadAllNameplates =
                typeof(CVRPlayerManager).GetMethod("ReloadAllNameplates", BindingFlags.Public | BindingFlags.Instance);
            var _onReloadAllNameplates =
                typeof(Patching).GetMethod(nameof(OnReloadAllNameplates), BindingFlags.NonPublic | BindingFlags.Static);


            if (_HandleRelations != null && _onRelations != null)
            {
                _instance.Patch(_HandleRelations, null, new HarmonyMethod(_onRelations));
            }
            else
            {
                NEKOClient.Error("Failed to patch HandleRelations\n" + new StackTrace());
            }

            if (_SettingsChanged != null && _onSettingsChanged != null)
            {
                _instance.Patch(_SettingsChanged, null, new HarmonyMethod(_onSettingsChanged));
            }
            else
            {
                NEKOClient.Error("Failed to patch SettingsChanged\n" + new StackTrace());
            }

            if (_PlayerLeave != null && _onPlayerLeave != null)
            {
                _instance.Patch(_PlayerLeave, new HarmonyMethod(_onPlayerLeave));
            }
            else
            {
                NEKOClient.Error("Failed to patch PlayerLeave\n" + new StackTrace());
            }

            if (_AvatarInstantiated != null && _onAvatarInstantiated != null)
            {
                _instance.Patch(_AvatarInstantiated, null, new HarmonyMethod(_onAvatarInstantiated));
            }
            else
            {
                NEKOClient.Error("Failed to patch AvatarInstantiated\n" + new StackTrace());
            }

            if (_ReloadAllNameplates != null && _onReloadAllNameplates != null)
            {
                _instance.Patch(_ReloadAllNameplates, null, new HarmonyMethod(_onReloadAllNameplates));
            }
            else
            {
                NEKOClient.Error("Failed to patch ReloadAllNameplates\n" + new StackTrace());
            }

            if (_ReloadFriends != null && _onReloadFriends != null)
            {
                _instance.Patch(_ReloadFriends, null, new HarmonyMethod(_onReloadFriends));
            }
            else
            {
                NEKOClient.Error("Failed to patch ReloadFriends\n" + new StackTrace());
            }

            if (_targetMethod != null)
            {
                if (_TryCreatePlayer != null)
                {
                    if (_onTryCreatePlayer != null)
                    {
                        if (_playerEntity != null)
                        {
                            if (_onPlayerJoin != null)
                            {
                                _instance.Patch(_TryCreatePlayer, transpiler: new HarmonyMethod(_onTryCreatePlayer));
                            }
                            else
                            {
                                NEKOClient.Error("[5] Failed to patch TryCreatePlayer\n" + new StackTrace());
                            }
                        }
                        else
                        {
                            NEKOClient.Error("[4] Failed to patch TryCreatePlayer\n" + new StackTrace());
                        }
                    }
                    else
                    {
                        NEKOClient.Error("[3] Failed to patch TryCreatePlayer\n" + new StackTrace());
                    }
                }
                else
                    NEKOClient.Error("[2] Failed to patch TryCreatePlayer\n" + new StackTrace());
            }
            else
            {
                NEKOClient.Error("[1] Failed to patch TryCreatePlayer\n" + new StackTrace());
            }
        }

        private static void OnRelations(ViewManager __instance, string __0, string __1)
        {
            switch (__1)
            {
                case "Add":
                    {
                        return;
                    }
                case "Deny":
                    {
                        return;
                    }
                case "Unfriend":
                    {
                        var nameplate = NEKOClient.NameplateManager?.GetNameplate(__0);
                        if (nameplate != null)
                        {
                            nameplate.IsFriend = false;
                        }

                        return;
                    }
                case "Block":
                    {
                        var nameplate = NEKOClient.NameplateManager?.GetNameplate(__0);
                        if (nameplate != null)
                        {
                            nameplate.IsBlocked = true;
                        }

                        return;
                    }
                case "Unblock":
                    {
                        var nameplate = NEKOClient.NameplateManager?.GetNameplate(__0);
                        if (nameplate != null)
                        {
                            nameplate.IsBlocked = false;
                        }

                        return;
                    }
                case "RequestInvite":
                    {
                        return;
                    }
            }
        }

        private static void OnSettingsChanged(MetaPort __instance)
        {
            if (!__instance.settings.settingsHaveChanged) return;
            if (NEKOClient.NameplateManager == null) return;
            foreach (var nameplate in NEKOClient.NameplateManager.Nameplates.Where(nameplate => nameplate.Value != null))
            {
                nameplate.Value!.ApplySettings();
            }
        }

        private static void OnAvatarInstantiated(PuppetMaster __instance)
        {
            var descriptor = __instance.GetPlayerDescriptor();
            if (descriptor != null)
            {
                var entity = PlayerUtils.GetPlayerEntity(descriptor.ownerId);
                if (entity != null)
                {
                    if (NEKOClient.NameplateManager != null) MelonCoroutines.Start(NEKOClient.NameplateManager.CreateNameplate(entity));
                }
                else
                {
                    NEKOClient.Error("Failed to get player entity for " + descriptor.ownerId);
                }
            }
            else
            {
                NEKOClient.Error("Failed to get player descriptor for avatar " + __instance.name);
            }
        }

        private static void OnPlayerJoin(CVRPlayerEntity __instance)
        {
            if (NEKOClient.NameplateManager != null) MelonCoroutines.Start(NEKOClient.NameplateManager.CreateNameplate(__instance));
        }

        private static void OnPlayerLeave(CVRPlayerEntity __instance)
        {
            if (NEKOClient.NameplateManager != null) NEKOClient.NameplateManager.RemoveNameplate(__instance.Uuid);
        }

        private static void OnReloadAllNameplates()
        {
            if (NEKOClient.NameplateManager == null) return;
            foreach (var pair in NEKOClient.NameplateManager.Nameplates)
            {
                NEKOClient.Debug("Reloading Nameplate: " + pair.Key);
                if (pair.Value != null) pair.Value.ApplySettings();
            }
        }

        private static void OnReloadFriends(ViewManager __instance)
        {
            if (NEKOClient.NameplateManager == null) return;
            foreach (var nameplate in NEKOClient.NameplateManager.Nameplates.Select(pair => pair.Value).Where(nameplate => nameplate != null))
            {
                nameplate!.IsFriend = Friends.FriendsWith(nameplate.Player.Uuid);
            }
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new CodeMatcher(instructions)
                .MatchForward(true, new CodeMatch(OpCodes.Callvirt, _targetMethod))
                .Insert(
                    new CodeInstruction(OpCodes.Ldloc_0),
                    new CodeInstruction(OpCodes.Ldfld, _playerEntity),
                    new CodeInstruction(OpCodes.Call, _onPlayerJoin)
                )
                .InstructionEnumeration();

            return code;
        }
    }
}
