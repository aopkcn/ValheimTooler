using System;
using System.Collections.Generic;
using System.Linq;
using RapidGUI;
using UnityEngine;
using ValheimTooler.Core.Extensions;
using ValheimTooler.Models;
using ValheimTooler.Utils;

namespace ValheimTooler.Core
{
    public static class PlayerHacks
    {
        public static bool s_isInfiniteStaminaMe = false;
        public static bool s_inventoryNoWeightLimit = false;
        public static bool s_instantCraft = false;
        public static bool s_bypassRestrictedTeleportable = false;
        private static bool s_isInfiniteStaminaOthers = false;
        private static bool s_isNoStaminaOthers = false;
        private static int s_teleportSourceIdx = -1;
        private static int s_teleportTargetIdx = -1;
        private static string s_teleportTargetSearchTerms = "";
        private static string s_teleportTargetPreviousSearchTerms = "";
        private static string s_teleportCoordinates = "0,0,0";
        private static int s_healTargetIdx = -1;
        private static string s_guardianPowerIdx = "";
        private static int s_guardianPowerTargetIdx = -1;
        private static IDictionary<string, string> s_guardianPowers;
        private static int s_skillNameIdx = 0;
        private static int s_skillLevelIdx = 0;

        private static float s_actionTimer = 0f;
        private static readonly float s_actionTimerInterval = 0.5f;

        private static float s_updateTimer = 0f;
        private static readonly float s_updateTimerInterval = 1.5f;

        private static List<TPTarget> s_tpTargets = null;
        private static List<TPTarget> s_tpTargetsFiltered = null;
        private static List<Player> s_players = null;

        private static readonly List<Skills.SkillType> s_skills = new List<Skills.SkillType>();
        private static readonly List<string> s_levels = new List<string>();

        public static void Start()
        {
            foreach (object obj in Enum.GetValues(typeof(Skills.SkillType)))
            {
                Skills.SkillType skillType = (Skills.SkillType)obj;

                if (skillType == Skills.SkillType.None)
                    continue;

                s_skills.Add(skillType);
            }
            s_skills.Reverse();
            for (var i = 1; i <= 100; i++)
            {
                s_levels.Add(i.ToString());
            }

            s_guardianPowers = new Dictionary<string, string>() {
                { Localization.instance.Localize("$se_eikthyr_name"), "GP_Eikthyr" },
                { Localization.instance.Localize("$se_theelder_name"), "GP_TheElder" },
                { Localization.instance.Localize("$se_bonemass_name"), "GP_Bonemass" },
                { Localization.instance.Localize("$se_moder_name"), "GP_Moder" },
                { Localization.instance.Localize("$se_yagluth_name"), "GP_Yagluth" },
                { Localization.instance.Localize("$se_queen_name"), "GP_Queen" },
                { Localization.instance.Localize("$se_fader_name"), "GP_Fader" },
            };
        }

        public static void Update()
        {
            if (Time.time >= s_actionTimer)
            {
                if (s_isInfiniteStaminaOthers)
                {
                    AllOtherPlayersMaxStamina();
                }
                if (s_isNoStaminaOthers)
                {
                    AllOtherPlayerNoStamina();
                }
                s_actionTimer = Time.time + s_actionTimerInterval;
            }

            if (Time.time >= s_updateTimer)
            {
                if (ZNet.instance == null || Minimap.instance == null)
                {
                    s_tpTargets = null;
                    s_tpTargetsFiltered = null;
                    s_teleportSourceIdx = -1;
                    s_teleportTargetIdx = -1;
                }
                else
                {
                    List<TPTarget> targets = new List<TPTarget>();

                    foreach (Player player in Player.GetAllPlayers())
                    {
                        targets.Add(new TPTarget(TPTarget.TargetType.Player, player));
                    }

                    foreach(ZNet.PlayerInfo player in ZNet.instance.GetPlayerList())
                    {
                        if (player.m_characterID == null || !player.m_publicPosition)
                            continue;

                        var result = targets.FirstOrDefault(t => t.targetType == TPTarget.TargetType.Player &&  t.player.GetPlayerName() == player.m_name);

                        if (result == null)
                        {
                            targets.Add(new TPTarget(TPTarget.TargetType.PlayerNet, player));
                        }
                    }

                    foreach (var pin in Minimap.instance.GetFieldValue<List<Minimap.PinData>>("m_pins"))
                    {
                        targets.Add(new TPTarget(TPTarget.TargetType.MapPin, pin));
                    }

                    s_tpTargets = targets;
                    s_tpTargetsFiltered = targets;
                    s_teleportTargetPreviousSearchTerms = "";
                    SearchTeleportTarget();
                }

                s_players = Player.GetAllPlayers();

                s_updateTimer = Time.time + s_updateTimerInterval;
            }

            if (ConfigManager.s_godModeShortCut.Value.IsDown())
            {
                ActionCurrentPlayerToggleGodMode(true);
            }
            if (ConfigManager.s_unlimitedStaminaShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleUnlimitedStamina(true);
            }
            if (ConfigManager.s_flyModeShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleFlyMode(true);
            }
            if (ConfigManager.s_ghostModeShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleGhostMode(true);
            }
            if (ConfigManager.s_noPlacementCostShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleNoPlacementCost(true);
            }
            if (ConfigManager.s_inventoryInfiniteWeightShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleInventoryInfiniteWeight(true);
            }
            if (ConfigManager.s_instantCraftShortcut.Value.IsDown())
            {
                ActionCurrentPlayerToggleInstantCraft(true);
            }
            if (ConfigManager.s_guardianPowerAllShortcut.Value.IsDown())
            {
                if (s_guardianPowers.ContainsKey(s_guardianPowerIdx))
                {
                    AllPlayersActiveGuardianPower(s_guardianPowers[s_guardianPowerIdx]);
                }
            }
            if (ConfigManager.s_healAllShortcut.Value.IsDown())
            {
                foreach (Player player in s_players)
                {
                    player.VTHeal();
                }
            }
        }

        public static void DisplayGUI()
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.BeginVertical();
                {
                    GUILayout.BeginVertical(VTLocalization.instance.Localize("$vt_player_general_title"), GUI.skin.box, GUILayout.ExpandWidth(false));
                    {
                        GUILayout.Space(EntryPoint.s_boxSpacing);

                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_god_mode", Player.m_localPlayer.VTInGodMode(), ConfigManager.s_godModeShortCut.Value)))
                        {
                            ActionCurrentPlayerToggleGodMode();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_inf_stamina_me", s_isInfiniteStaminaMe, ConfigManager.s_unlimitedStaminaShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleUnlimitedStamina();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_inf_stamina_others", s_isInfiniteStaminaOthers)))
                        {
                            s_isInfiniteStaminaOthers = !s_isInfiniteStaminaOthers;
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_no_stamina", s_isNoStaminaOthers)))
                        {
                            s_isNoStaminaOthers = !s_isNoStaminaOthers;
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_fly_mode", Player.m_localPlayer.VTInFlyMode(), ConfigManager.s_flyModeShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleFlyMode();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_ghost_mode", Player.m_localPlayer.VTInGhostMode(), ConfigManager.s_ghostModeShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleGhostMode();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_no_placement_cost", Player.m_localPlayer.VTIsNoPlacementCost(), ConfigManager.s_noPlacementCostShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleNoPlacementCost();
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_explore_minimap")))
                        {
                            Minimap.instance.VTExploreAll();
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_reset_minimap")))
                        {
                            Minimap.instance.VTReset();
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_tame_creatures")))
                        {
                            Player.m_localPlayer.VTTameNearbyCreatures();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_infinite_weight", s_inventoryNoWeightLimit, ConfigManager.s_inventoryInfiniteWeightShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleInventoryInfiniteWeight();
                        }
                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_instant_craft", s_instantCraft, ConfigManager.s_instantCraftShortcut.Value)))
                        {
                            ActionCurrentPlayerToggleInstantCraft();
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_remove_tombstone")))
                        {
                            if (Player.m_localPlayer != null)
                            {
                                var tombstones = UnityEngine.Object.FindObjectsOfType<TombStone>();
                                foreach (var tombstone in tombstones)
                                {
                                    if ((long)tombstone.CallMethod("GetOwner") == Player.m_localPlayer.GetPlayerID())
                                    {
                                        var nview = tombstone.GetFieldValue<ZNetView>("m_nview");
                                        if (nview != null && nview.IsValid())
                                        {
                                            nview.Destroy();
                                        }
                                    }
                                }
                            }
                        }
                        // Deactivated because of mod conflict. See https://github.com/Astropilot/ValheimTooler/issues/45
                        //GUILayout.BeginHorizontal();
                        //{
                        //    var maxInteract = Player.m_localPlayer != null ? Player.m_localPlayer.m_maxInteractDistance : 5f;
                        //    GUILayout.Label(VTLocalization.instance.Localize("$vt_player_farinteract_label (") + maxInteract.ToString("F1") + ")", GUILayout.ExpandWidth(false));
                        //    maxInteract = GUILayout.HorizontalSlider(maxInteract, 1f, 50f, GUILayout.ExpandWidth(true));
                        //    if (Player.m_localPlayer != null)
                        //    {
                        //        Player.m_localPlayer.m_maxInteractDistance = maxInteract;
                        //        Player.m_localPlayer.m_maxPlaceDistance = maxInteract;
                        //    }
                        //}
                        //GUILayout.EndHorizontal();
                        //if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_farinteract_reset")))
                        //{
                        //    if (Player.m_localPlayer != null)
                        //    {
                        //        Player.m_localPlayer.m_maxInteractDistance = 5f;
                        //    }
                        //}
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(VTLocalization.instance.Localize("$vt_player_power_title"), GUI.skin.box, GUILayout.ExpandWidth(false));
                    {
                        GUILayout.Space(EntryPoint.s_boxSpacing);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_power_name :"), GUILayout.ExpandWidth(false));
                            s_guardianPowerIdx = RGUI.SelectionPopup(s_guardianPowerIdx, s_guardianPowers.Keys.Select(p => VTLocalization.instance.Localize(p)).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_target :"), GUILayout.ExpandWidth(false));
                            s_guardianPowerTargetIdx = RGUI.SelectionPopup(s_guardianPowerTargetIdx, s_players?.Select(p => p.GetPlayerName()).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_power_active_target")))
                        {
                            if (s_guardianPowerTargetIdx < s_players.Count && s_guardianPowerTargetIdx >= 0)
                            {
                                s_players[s_guardianPowerTargetIdx].VTActiveGuardianPower(s_guardianPowers[s_guardianPowerIdx]);
                            }
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_power_active_all")))
                        {
                            if (s_guardianPowers.ContainsKey(s_guardianPowerIdx))
                            {
                                AllPlayersActiveGuardianPower(s_guardianPowers[s_guardianPowerIdx]);
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();

                GUILayout.BeginVertical();
                {
                    GUILayout.BeginVertical(VTLocalization.instance.Localize("$vt_player_teleport_title"), GUI.skin.box, GUILayout.ExpandWidth(false));
                    {
                        GUILayout.Space(EntryPoint.s_boxSpacing);

                        if (GUILayout.Button(UI.Utils.ToggleButtonLabel("$vt_player_teleport_restricted", s_bypassRestrictedTeleportable)))
                        {
                            s_bypassRestrictedTeleportable = !s_bypassRestrictedTeleportable;
                        }

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_teleport_player_source :"), GUILayout.ExpandWidth(false));

                            s_teleportSourceIdx = RGUI.SelectionPopup(s_teleportSourceIdx, s_players?.Select(p => p.GetPlayerName()).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_teleport_target :"), GUILayout.ExpandWidth(false));

                            s_teleportTargetIdx = RGUI.SearchableSelectionPopup(s_teleportTargetIdx, s_tpTargetsFiltered?.Select(t => t.ToString()).ToArray(), ref s_teleportTargetSearchTerms);
                            SearchTeleportTarget();
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_teleport_button")))
                        {
                            if (s_players != null && s_teleportSourceIdx < s_players.Count && s_teleportSourceIdx >= 0)
                            {
                                if (s_tpTargetsFiltered != null && s_teleportTargetIdx < s_tpTargetsFiltered.Count && s_teleportTargetIdx >= 0)
                                {
                                    var source = s_players[s_teleportSourceIdx];
                                    var targetPosition = s_tpTargetsFiltered[s_teleportTargetIdx].Position;

                                    if (targetPosition != null && targetPosition is Vector3 targetPositionValue)
                                    {
                                        source.TeleportTo(targetPositionValue, source.transform.rotation, true);
                                    }
                                }
                            }
                        }

                        GUILayout.Space(EntryPoint.s_boxSpacing);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_coordinates (X,Y,Z):") + GetPlayerCoordinates(), GUILayout.ExpandWidth(false));
                        }
                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_teleport_coordinates :"), GUILayout.ExpandWidth(false));
                            s_teleportCoordinates = GUILayout.TextField(s_teleportCoordinates);
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_teleport_button")))
                        {
                            var coordinates = s_teleportCoordinates.Split(',');
                            if (Player.m_localPlayer != null && coordinates.Length == 3)
                            {
                                if (int.TryParse(coordinates[0], out int coord_x) && int.TryParse(coordinates[1], out int coord_y) && int.TryParse(coordinates[2], out int coord_z))
                                {
                                    Player.m_localPlayer.TeleportTo(new Vector3(coord_x, coord_y, coord_z), Player.m_localPlayer.transform.rotation, true);
                                }
                            }
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(VTLocalization.instance.Localize("$vt_player_heal_manager_title"), GUI.skin.box, GUILayout.ExpandWidth(false));
                    {
                        GUILayout.Space(EntryPoint.s_boxSpacing);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_heal_player :"), GUILayout.ExpandWidth(false));

                            s_healTargetIdx = RGUI.SelectionPopup(s_healTargetIdx, s_players?.Select(p => p.GetPlayerName()).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_heal_selected_player")))
                        {
                            if (s_healTargetIdx < s_players.Count && s_healTargetIdx >= 0)
                            {
                                s_players[s_healTargetIdx].VTHeal();
                            }
                        }
                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_heal_all_players")))
                        {
                            foreach (Player player in s_players)
                            {
                                player.VTHeal();
                            }
                        }
                    }
                    GUILayout.EndVertical();

                    GUILayout.BeginVertical(VTLocalization.instance.Localize("$vt_player_skill_title"), GUI.skin.box, GUILayout.ExpandWidth(false));
                    {
                        GUILayout.Space(EntryPoint.s_boxSpacing);
                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_skill_name :"), GUILayout.ExpandWidth(false));
                            s_skillNameIdx = RGUI.SelectionPopup(s_skillNameIdx, s_skills.Select(skill => skill.ToString()).ToArray());
                        }
                        GUILayout.EndHorizontal();

                        GUILayout.BeginHorizontal();
                        {
                            GUILayout.Label(VTLocalization.instance.Localize("$vt_player_skill_level :"), GUILayout.ExpandWidth(false));
                            s_skillLevelIdx = RGUI.SelectionPopup(s_skillLevelIdx, s_levels.ToArray());
                        }
                        GUILayout.EndHorizontal();

                        if (GUILayout.Button(VTLocalization.instance.Localize("$vt_player_skill_button")))
                        {
                            if (s_skillNameIdx < s_skills.Count && s_skillNameIdx >= 0)
                            {
                                if (int.TryParse(s_levels[s_skillLevelIdx], out int levelInt))
                                {
                                    Skills.SkillType skillType = s_skills[s_skillNameIdx];

                                    if (skillType == Skills.SkillType.All)
                                    {
                                        foreach (object obj in Enum.GetValues(typeof(Skills.SkillType)))
                                        {
                                            Skills.SkillType skillType2 = (Skills.SkillType)obj;

                                            if (skillType2 == Skills.SkillType.None || skillType2 == Skills.SkillType.All)
                                                continue;

                                            Player.m_localPlayer.VTUpdateSkillLevel(skillType2, levelInt);
                                        }
                                    }

                                    Player.m_localPlayer.VTUpdateSkillLevel(skillType, levelInt);
                                }
                            }
                        }
                    }
                    GUILayout.EndVertical();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private static void ActionCurrentPlayerToggleGodMode(bool sendNotification = false)
        {
            Player.m_localPlayer.VTSetGodMode(!Player.m_localPlayer.VTInGodMode());

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_god_mode", Player.m_localPlayer.VTInGodMode()));
            }
        }

        private static void ActionCurrentPlayerToggleUnlimitedStamina(bool sendNotification = false)
        {
            s_isInfiniteStaminaMe = !s_isInfiniteStaminaMe;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_inf_stamina_me", s_isInfiniteStaminaMe));
            }
        }

        private static void ActionCurrentPlayerToggleFlyMode(bool sendNotification = false)
        {
            Player.m_localPlayer.VTSetFlyMode(!Player.m_localPlayer.VTInFlyMode());

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_fly_mode", Player.m_localPlayer.VTInFlyMode()));
            }
        }

        private static void ActionCurrentPlayerToggleGhostMode(bool sendNotification = false)
        {
            Player.m_localPlayer.VTSetGhostMode(!Player.m_localPlayer.VTInGhostMode());

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_ghost_mode", Player.m_localPlayer.VTInGhostMode()));
            }
        }

        private static void ActionCurrentPlayerToggleNoPlacementCost(bool sendNotification = false)
        {
            Player.m_localPlayer.VTSetNoPlacementCost(!Player.m_localPlayer.VTIsNoPlacementCost());

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_no_placement_cost", Player.m_localPlayer.VTIsNoPlacementCost()));
            }
        }

        private static void ActionCurrentPlayerToggleInventoryInfiniteWeight(bool sendNotification = false)
        {
            s_inventoryNoWeightLimit = !s_inventoryNoWeightLimit;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_infinite_weight", s_inventoryNoWeightLimit));
            }
        }

        private static void ActionCurrentPlayerToggleInstantCraft(bool sendNotification = false)
        {
            s_instantCraft = !s_instantCraft;

            if (sendNotification)
            {
                Player.m_localPlayer.VTSendMessage(UI.Utils.ToggleButtonLabel("$vt_player_instant_craft", s_instantCraft));
            }
        }

        private static void SearchTeleportTarget()
        {
            if (s_teleportTargetPreviousSearchTerms.Equals(s_teleportTargetSearchTerms))
            {
                return;
            }
            if (s_teleportTargetSearchTerms.Length == 0)
            {
                s_tpTargetsFiltered = s_tpTargets;
            }
            else
            {
                string searchLower = s_teleportTargetSearchTerms.ToLower();
                s_tpTargetsFiltered = s_tpTargets.Where(i => i.ToString().ToLower().Contains(searchLower)).ToList();
            }
            s_teleportTargetPreviousSearchTerms = s_teleportTargetSearchTerms;
        }

        private static void AllOtherPlayersMaxStamina()
        {
            List<Player> players = Player.GetAllPlayers();

            if (players != null && Player.m_localPlayer != null)
            {
                foreach (Player player in players)
                {
                    if (player.GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                    {
                        player.VTSetMaxStamina();
                    }
                }
            }
        }

        private static void AllOtherPlayerNoStamina()
        {
            List<Player> players = Player.GetAllPlayers();

            if (players != null && Player.m_localPlayer != null)
            {
                foreach (Player player in players)
                {
                    if (player.GetPlayerID() != Player.m_localPlayer.GetPlayerID())
                    {
                        player.VTSetNoStamina();
                    }
                }
            }
        }

        private static void AllPlayersActiveGuardianPower(string guardianPower)
        {
            List<Player> players = Player.GetAllPlayers();

            if (players != null)
            {
                foreach (Player player in players)
                {
                    player.VTActiveGuardianPower(guardianPower);
                }
            }
        }

        private static string GetPlayerCoordinates()
        {
            Player localPlayer = Player.m_localPlayer;

            if (localPlayer == null)
            {
                return "[None]";
            }

            return localPlayer.transform.position.ToString("F0");
        }
    }
}
