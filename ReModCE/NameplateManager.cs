﻿using System.Collections;
using System.Diagnostics;
using ABI_RC.Core.Networking.IO.Social;
using ABI_RC.Core.Player;
using Dissonance;
using MelonLoader;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using NEKOClient.MonoScripts;
using NEKOClient.Reflection;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NEKOClient
{
    public class NameplateManager
    {

        public readonly Dictionary<string, OldNameplate?> Nameplates;
        private static Dictionary<string, Texture>? _imageCache;
        private static Dictionary<string, RawImage[]>? _imageQueue;

        public NameplateManager()
        {
            Nameplates = new Dictionary<string, OldNameplate?>();
            _imageCache = new Dictionary<string, Texture>();
            _imageQueue = new Dictionary<string, RawImage[]>();

            MelonCoroutines.Start(ImageRequestLoop());
        }

        public static void AddImageToQueue(string id, RawImage[] image)
        {
            if (_imageQueue != null && _imageCache != null)
            {
                if (id is "" or "https://files.abidata.io/user_images/00default.png") return;
                if (_imageCache.TryGetValue(id, out var cachedImage))
                {
                    foreach (var im in image)
                    {
                        im.texture = cachedImage;
                    }
                }
                else
                {
                    if (!_imageQueue.ContainsKey(id))
                    {
                        _imageQueue.Add(id, image);
                    }
                }
            }
            else
            {
                NEKOClient.Error("Image Queue is Null");
            }
        }

        private static IEnumerator ImageRequestLoop()
        {
            while (true)
            {
                var rateLimit = Settings.RateLimit == null ? 1f : Settings.RateLimit.Value;
                _imageQueue = (_imageQueue ?? new Dictionary<string, RawImage[]>()).Where(w => w.Key != null).ToDictionary(w => w.Key, w => w.Value);
                if (_imageQueue is { Count: > 0 })
                {
                    var pair = _imageQueue.First(w => w.Key != null);
                    if (pair.Key != null)
                    {
                        using var uwr = UnityWebRequest.Get(pair.Key);
                        uwr.downloadHandler = new DownloadHandlerTexture();

                        var request = uwr.SendWebRequest();
                        while (!request.isDone)
                        {
                            yield return null;
                        }

                        if (uwr.isNetworkError || uwr.isHttpError)
                        {
                            NEKOClient.Warning("Unable to set profile picture: " + uwr.error + "\n" + new StackTrace());
                            _imageQueue.Remove(pair.Key);
                        }
                        else
                        {
                            var tex = ((DownloadHandlerTexture)uwr.downloadHandler).texture;
                            _imageCache?.Add(pair.Key, tex);
                            foreach (var im in pair.Value)
                            {
                                im.texture = tex;
                            }
                        }
                        _imageQueue.Remove(pair.Key);
                        uwr.Dispose();
                    }
                }

                yield return new WaitForSeconds(rateLimit);
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private void AddNameplate(OldNameplate nameplate, CVRPlayerEntity player)
        {
            string id;
            try
            {
                id = player.Uuid;
            }
            catch
            {
                return;
            }

            if (id != null && nameplate != null)
                Nameplates.Add(id, nameplate);
        }

        public void RemoveNameplate(string player)
        {
            if (Nameplates.ContainsKey(player))
            {
                if (Nameplates.TryGetValue(player, out var nameplate))
                {
                    Object.Destroy(nameplate!.gameObject);
                }
                Nameplates.Remove(player);
            }
            else
            {
                NEKOClient.Error("NameplateManager: RemoveNameplate: Player not found: " + player + "\n" + new StackTrace());
            }
        }

        public OldNameplate? GetNameplate(CVRPlayerEntity player)
        {
            if (Nameplates.TryGetValue(player.Uuid, out var nameplate))
            {
                return nameplate;
            }
            else
            {
                MelonCoroutines.Start(CreateNameplate(player));
            }

            NEKOClient.DebugError($"Nameplate does not exist in Dictionary for player: {player.Username}");
            return null;
        }

        public OldNameplate? GetNameplate(string id)
        {
            if (Nameplates.TryGetValue(id, out var nameplate))
            {
                return nameplate;
            }
            else
            {
                var player = PlayerUtils.GetPlayerEntity(id);
                if (player != null)
                {
                    MelonCoroutines.Start(CreateNameplate(player));
                }
                else
                {
                    NEKOClient.DebugError($"Player does not exist in Dictionary for id: {id}");
                }
            }
            return null;
        }

        public void ClearNameplates()
        {
            Nameplates.Clear();
        }

        //Nameplates can support 5 compatibility badges total
        public GameObject? AddBadge(OldNameplate plate, string id, Texture2D? icon)
        {
            if (plate.badgeCompat == null) return null;
            var gameObject = plate.badgeCompat.gameObject;
            var badge = Object.Instantiate(gameObject, gameObject.transform.parent);
            badge.name = id;
            badge.transform.localPosition = gameObject.transform.localPosition;
            badge.transform.localRotation = gameObject.transform.localRotation;
            badge.transform.localScale = gameObject.transform.localScale;
            badge.SetActive(true);
            var image = badge.GetComponent<Image>();
            if (icon != null)
            {
                image.sprite = Sprite.Create(icon, new Rect(0, 0, icon.width, icon.height),
                    new Vector2(0.5f, 0.5f));
            }
            else
            {
                image.enabled = false;
            }
            return badge;
        }

        public static void InitializePlate(OldNameplate oldNameplate, PlayerDescriptor playerDescriptor)
        {
            try
            {
                if (playerDescriptor != null)
                {
                    var playerEntity = PlayerUtils.GetPlayerEntity(playerDescriptor.ownerId);
                    if (playerEntity != null)
                    {
                        oldNameplate.Player = playerEntity;

                        if (oldNameplate.Player != null)
                        {
                            var player = oldNameplate.Player;

                            oldNameplate.Name = player.Username;

                            // oldNameplate.Status = player.field_Private_APIUser_0.statusDescriptionDisplayString;

                            oldNameplate.Rank = player.ApiUserRank;

                            oldNameplate.VipRank = Utils.GetAbbreviation(player.ApiUserRank);

                            // Literally broken no matter what I try.
                            //oldNameplate.ShowSocialRank = player.field_Private_APIUser_0.showSocialRank;

                            oldNameplate.IsFriend = Friends.FriendsWith(player.Uuid);

                            // oldNameplate.IsMaster = player.field_Private_VRCPlayerApi_0.isMaster;

                            // NEKOClient.NameplateManager!._masterClient = player.field_Private_APIUser_0.id;

                            //Getting if this value has changed.
                            //uSpeaker.NativeMethodInfoPtr_Method_Public_Single_1
                            //Have fun future me, it's your favorite thing, native patching :D
                            // oldNameplate.UserVolume = player.prop_USpeaker_0.field_Private_Single_1;

                            oldNameplate.ProfilePicture = player.ApiProfileImageUrl;

                            // oldNameplate.IsQuest = player.field_Private_APIUser_0._last_platform.ToLower() == "android";

                            oldNameplate.IsMuted = player.PlayerDescriptor.voiceMuted;

                            oldNameplate.IsLocal = player.DarkRift2Player.Type == NetworkPlayerType.Local;
                        }
                        else
                        {
                            NEKOClient.NameplateManager?.RemoveNameplate(playerDescriptor.ownerId);
                        }
                    }
                    else
                    {
                        NEKOClient.NameplateManager?.RemoveNameplate(playerDescriptor.ownerId);
                    }
                }
                else
                {
                    oldNameplate.Name = "||Error||";
                    NEKOClient.Error("Unable to Initialize Nameplate: Player Descriptor is null\n" + new StackTrace());
                }
            }
            catch (Exception e)
            {
                oldNameplate.Name = "||Error||";
                NEKOClient.Error("Unable to Initialize Nameplate: " + e + "\n" + new StackTrace());
            }
        }

        public static void OnEnableToggle(Component playerNameplate, OldNameplate? oldNameplate)
        {
            if (Settings.Enabled == null) return;
            if (Settings.Enabled.Value)
            {
                playerNameplate.gameObject.SetActive(false);
                if (oldNameplate != null && !oldNameplate.IsLocal &&
                    oldNameplate.Nameplate != null)
                    oldNameplate.Nameplate.SetActive(Settings.Enabled.Value);
            }
            else
            {
                playerNameplate.gameObject.SetActive(true);
                if (oldNameplate != null && oldNameplate.Nameplate != null)
                    oldNameplate.Nameplate.SetActive(false);
            }
        }

        public IEnumerator CreateNameplate(CVRPlayerEntity playerEntity)
        {
            yield return new WaitForSeconds(0.25f);

            var oldNameplate = playerEntity.PlayerNameplate;
            if (oldNameplate == null) yield break;
            if (oldNameplate.gameObject != null)
            {
                if (oldNameplate.gameObject.transform != null)
                {
                    var position = oldNameplate.gameObject.transform.position;

                    if (Settings.Offset != null && Settings.Scale != null && Settings.Enabled != null)
                    {
                        var scaleValue = Settings.Scale.Value * .001f;
                        var offsetValue = Settings.Offset.Value;
                        var id = playerEntity.Uuid;
                        if (id is { Length: > 0 })
                        {
                            if (Nameplates.TryGetValue(id, out var nameplate))
                            {
                                if (nameplate != null)
                                {
                                    nameplate.ApplySettings();
                                }
                                else
                                {
                                    NEKOClient.Debug("Nameplate is null, removing from dictionary\n" + new StackTrace());
                                    RemoveNameplate(id);
                                }
                            }
                            else
                            {
                                var plate = Object.Instantiate(AssetManager.Nameplate,
                                    new(position.x, position.y + offsetValue, position.z),
                                    new(0, 0, 0, 0), oldNameplate.transform.parent);

                                if (plate != null)
                                {
                                    plate.transform.localScale = new(scaleValue, scaleValue, scaleValue);
                                    plate.name = "OldNameplate";
                                    nameplate = plate.AddComponent<OldNameplate>();
                                    AddNameplate(nameplate, playerEntity);
                                }
                                else
                                {
                                    NEKOClient.Error("Unable to Instantiate Nameplate: Nameplate is Null\n" +
                                                    new StackTrace());
                                }
                            }

                            OnEnableToggle(oldNameplate, nameplate);
                        }
                        else
                        {
                            NEKOClient.Error("Unable to Instantiate Nameplate: Player is Null\n" + new StackTrace());
                        }
                    }
                    else
                    {
                        NEKOClient.Error("Unable to Initialize Nameplate: Settings are null\n" + new StackTrace());
                    }
                }
                else
                {
                    NEKOClient.Error("Unable to Initialize Nameplate: Nameplate Transform is null\n" + new StackTrace());
                }
            }
            else
            {
                NEKOClient.Error("Unable to Initialize Nameplate: Nameplate Gameobject is null\n" + new StackTrace());
            }
        }
    }
}
