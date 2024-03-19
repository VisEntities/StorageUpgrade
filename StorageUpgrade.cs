using System;
using System.Collections.Generic;
using Network;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Originally created by Orange, up to version 1.5.1
 */

namespace Oxide.Plugins
{
    [Info("Storage Upgrade", "VisEntities", "2.1.0")]
    [Description("Hit storage with a hammer to boost its capacity.")]

    public class StorageUpgrade : RustPlugin
    {
        #region 3rd Party Dependencies

        [PluginReference]
        private readonly Plugin Economics, ServerRewards;

        #endregion 3rd Party Dependencies

        #region Fields

        private static StorageUpgrade _plugin;
        private static Configuration _config;

        private const string FX_UPGRADE = "assets/prefabs/misc/easter/painted eggs/effects/gold_open.prefab";
        private const string FX_UPGRADE_2 = "assets/prefabs/missions/effects/mission_objective_complete.prefab";

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Non Upgradeable Containers")]
            public string[] NonUpgradeableContainers { get; set; }

            [JsonProperty("Profiles")]
            public Dictionary<string, ProfileConfig> Profiles { get; set; }

            [JsonIgnore]
            public HashSet<string> permissions { get; set; } = new HashSet<string>();

            public void InitializeProfiles()
            {
                foreach (var keyValuePair in Profiles)
                {
                    string permissionSuffix = keyValuePair.Key;
                    ProfileConfig profile = keyValuePair.Value;

                    string permission = profile.ConstructPermission(permissionSuffix);
                    if (!permissions.Add(permission))
                        continue;

                    profile.RegisterPermission(permission);
                    profile.InitializeCurrencies();
                }
            }

            public ProfileConfig GetProfileForPlayer(BasePlayer player)
            {
                foreach (ProfileConfig profile in Profiles.Values)
                    if (PermissionUtil.VerifyHasPermission(player, profile.Permission))
                        return profile;

                return null;
            }
        }

        private class ProfileConfig
        {
            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("Expand Storage By")]
            public int ExpandStorageBy { get; set; }

            [JsonProperty("Container Shortnames")]
            public string[] ContainerShortnames { get; set; }

            [JsonProperty("Upgrade Cost")]
            public List<CurrencyConfig> UpgradeCost { get; set; }

            [JsonIgnore]
            public string Permission { get; set; }

            public string ConstructPermission(string permissionSuffix)
            {
                return string.Join(".", nameof(StorageUpgrade), permissionSuffix).ToLower();
            }

            public void RegisterPermission(string permission)
            {
                Permission = permission;
                _plugin.permission.RegisterPermission(Permission, _plugin);
            }

            public void InitializeCurrencies()
            {
                foreach (CurrencyConfig currency in UpgradeCost)
                {
                    currency.CreatePaymentGateway();
                }
            }

            public List<CurrencyConfig> GetUpgradeCost()
            {
                List<CurrencyConfig> cost = new List<CurrencyConfig>();

                foreach (var currency in UpgradeCost)
                {
                    if (currency.Enabled && currency.Valid)
                        cost.Add(currency);
                }

                return cost;
            }
        }

        private class CurrencyConfig
        {
            [JsonProperty("Name")]
            public string Name { get; set; }

            [JsonProperty("Enabled")]
            public bool Enabled { get; set; }

            [JsonProperty("Amount")]
            public int Amount { get; set; }

            [JsonIgnore]
            private bool _itemHasBeenValidated;

            [JsonIgnore]
            private ItemDefinition _itemDefinition;

            [JsonIgnore]
            public IPaymentGateway PaymentGateway;

            [JsonIgnore]
            public PaymentGatewayType PaymentGatewayType;

            [JsonIgnore]
            public bool Valid
            {
                get
                {
                    if (PaymentGateway != null && PaymentGateway.Valid && PaymentGatewayType != PaymentGatewayType.Unknown)
                        return true;

                    return false;
                }
            }

            [JsonIgnore]
            public ItemDefinition ItemDefinition
            {
                get
                {
                    if (!_itemHasBeenValidated)
                    {
                        ItemDefinition matchedItemDefinition = ItemManager.FindItemDefinition(Name);
                        if (matchedItemDefinition != null)
                            _itemDefinition = matchedItemDefinition;
                        else
                            return null;

                        _itemHasBeenValidated = true;
                    }

                    return _itemDefinition;
                }
            }

            public void CreatePaymentGateway()
            {
                if (string.IsNullOrEmpty(Name))
                    return;

                if (Name.Contains("coin"))
                {
                    PaymentGateway = CoinPaymentGateway.Instance;
                    PaymentGatewayType = PaymentGatewayType.Coin;
                }
                else if (Name.Contains("point"))
                {
                    PaymentGateway = PointPaymentGateway.Instance;
                    PaymentGatewayType = PaymentGatewayType.Point;
                }
                else if (ItemDefinition != null)
                {
                    PaymentGateway = new ItemPaymentGateway(ItemDefinition.itemid);
                    PaymentGatewayType = PaymentGatewayType.Item;
                }
                else
                {
                    PaymentGateway = null;
                    PaymentGatewayType = PaymentGatewayType.Unknown;
                }
            }

            public bool CanPlayerPay(BasePlayer player)
            {
                return PaymentGateway.Get(player) >= Amount;
            }

            public void DeductFromPlayer(BasePlayer player)
            {
                PaymentGateway.Deduct(player, Amount);
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                NonUpgradeableContainers = new string[]
                {
                    "vending.machine",
                    "furnace",
                    "furnace.large",
                    "legacyfurnace",
                    "electric.igniter",
                    "workbench1",
                    "workbench2",
                    "workbench3",
                    "box.repair.bench",
                    "research.table",
                    "campfire",
                    "water.barrel",
                    "fishtrophy",
                    "huntingtrophylarge",
                    "skull.trophy",
                    "huntingtrophysmall",
                    "small.oil.refinery",
                    "recycler_static",
                    "dropbox",
                },
                Profiles = new Dictionary<string, ProfileConfig>
                {
                    ["basic"] = new ProfileConfig
                    {
                        Enabled = true,
                        ExpandStorageBy = 2,
                        ContainerShortnames = new string[]
                        {
                            "planter.large.deployed",
                            "railroadplanter.deployed",
                            "bathtub.planter.deployed",
                            "planter.small.deployed",
                            "minecart.planter.deployed",
                            "fridge.deployed",
                            "composter",
                            "box.wooden.large",
                            "woodbox_deployed",
                            "small_stash_deployed"
                        },
                        UpgradeCost = new List<CurrencyConfig>
                        {
                            new CurrencyConfig
                            {
                                Enabled = true,
                                Name = "wood",
                                Amount = 100
                            },
                            new CurrencyConfig
                            {
                                Enabled = true,
                                Name = "metal.fragments",
                                Amount = 100
                            }
                        }
                    },
                    ["vip"] = new ProfileConfig
                    {
                        Enabled = true,
                        ExpandStorageBy = 6,
                        ContainerShortnames = new string[]
                        {
                            "planter.large.deployed",
                            "railroadplanter.deployed",
                            "bathtub.planter.deployed",
                            "planter.small.deployed",
                            "minecart.planter.deployed",
                            "fridge.deployed",
                            "composter",
                            "box.wooden.large",
                            "woodbox_deployed",
                            "small_stash_deployed"
                        },
                        UpgradeCost = new List<CurrencyConfig>
                        {
                            new CurrencyConfig
                            {
                                Enabled = false,
                                Name = "coin",
                                Amount = 1000
                            },
                            new CurrencyConfig
                            {
                                Enabled = false,
                                Name = "point",
                                Amount = 1000
                            }
                        }
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            _config.InitializeProfiles();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnHammerHit(BasePlayer player, HitInfo hitInfo)
        {
            if (player == null || hitInfo == null || !player.serverInput.IsDown(BUTTON.FIRE_SECONDARY))
                return;

            StorageContainer container = hitInfo.HitEntity as StorageContainer;
            if (container == null || _config.NonUpgradeableContainers.Contains(container.ShortPrefabName) || hitInfo.HitEntity is BaseOven)
            {
                SendReplyToPlayer(player, Lang.CannotUpgrade);
                return;
            }

            ProfileConfig profile = _config.GetProfileForPlayer(player);
            if (profile == null || !profile.Enabled || !profile.ContainerShortnames.Contains(container.ShortPrefabName))
            {
                SendReplyToPlayer(player, Lang.NoPermissionOrCannotUpgrade);
                return;
            }

            int currentCapacity = container.inventory.capacity;
            if (currentCapacity >= 48)
            {
                SendReplyToPlayer(player, Lang.AlreadyAtMaxCapacity);
                return;
            }

            List<CurrencyConfig> upgradeCost = profile.GetUpgradeCost();
            foreach (CurrencyConfig currency in upgradeCost)
            {
                int playerBalance = currency.PaymentGateway.Get(player);
                if (playerBalance < currency.Amount)
                {
                    int shortfall = currency.Amount - playerBalance;
                    SendReplyToPlayer(player, Lang.NeedMoreToUpgrade, shortfall, currency.Name);
                    return;
                }
            }

            foreach (CurrencyConfig currency in upgradeCost)
            {
                currency.DeductFromPlayer(player);
            }

            int newCapacity = Math.Min(currentCapacity + profile.ExpandStorageBy, 48);
            container.inventory.capacity = newCapacity;

            SendReplyToPlayer(player, Lang.UpgradedFromTo, currentCapacity, newCapacity);

            RunEffect(FX_UPGRADE, container.transform.position + new Vector3(0.0f, 1.0f, 0.5f), Vector3.up);
            RunEffect(FX_UPGRADE, container.transform.position + new Vector3(0.0f, 1.0f, -0.5f), Vector3.up);
            RunEffect(FX_UPGRADE, container.transform.position + new Vector3(-0.5f, 1.0f, 0.0f), Vector3.up);
            RunEffect(FX_UPGRADE, container.transform.position + new Vector3(0.5f, 1.0f, 0.0f), Vector3.up);

            RunEffect(FX_UPGRADE_2, player, boneId: 698017942);
        }

        #endregion Oxide Hooks

        #region Payment Gateways

        // The following PaymentGateway implementation was inspired by WhiteThunder.
        private enum PaymentGatewayType
        {
            Item,
            Coin,
            Point,
            Unknown
        }

        private interface IPaymentGateway
        {
            bool Valid { get; }

            int Get(BasePlayer player);

            void Give(BasePlayer player, int amount);

            void Deduct(BasePlayer player, int amount);
        }

        private class ItemPaymentGateway : IPaymentGateway
        {
            private int _itemId;

            public ItemPaymentGateway(int itemId)
            {
                _itemId = itemId;
            }

            public bool Valid
            {
                get { return true; }
            }

            public int Get(BasePlayer player)
            {
                return player.inventory.GetAmount(_itemId);
            }

            public void Give(BasePlayer player, int amount)
            {
                player.GiveItem(ItemManager.CreateByItemID(_itemId, amount));
                player.Command("note.inv", _itemId, +amount);
            }

            public void Deduct(BasePlayer player, int amount)
            {
                player.inventory.Take(null, _itemId, amount);
                player.Command("note.inv", _itemId, -amount);
            }
        }

        private class CoinPaymentGateway : IPaymentGateway
        {
            private static readonly CoinPaymentGateway _instance = new CoinPaymentGateway();

            private Plugin _economicsPlugin
            {
                get { return _plugin.Economics; }
            }

            public bool Valid
            {
                get { return VerifyPluginBeingLoaded(_economicsPlugin); }
            }

            public static CoinPaymentGateway Instance
            {
                get { return _instance; }
            }

            private CoinPaymentGateway() { }

            public int Get(BasePlayer player)
            {
                return Convert.ToInt32(_economicsPlugin.Call("Balance", player.userID));
            }

            public void Give(BasePlayer player, int amount)
            {
                _economicsPlugin.Call("Deposit", player.userID, Convert.ToDouble(amount));
            }

            public void Deduct(BasePlayer player, int amount)
            {
                _economicsPlugin.Call("Withdraw", player.userID, Convert.ToDouble(amount));
            }
        }

        private class PointPaymentGateway : IPaymentGateway
        {
            private static readonly PointPaymentGateway _instance = new PointPaymentGateway();

            private Plugin _serverRewardsPlugin
            {
                get { return _plugin.ServerRewards; }
            }

            public bool Valid
            {
                get { return VerifyPluginBeingLoaded(_serverRewardsPlugin); }
            }

            public static PointPaymentGateway Instance
            {
                get { return _instance; }
            }

            private PointPaymentGateway() { }

            public int Get(BasePlayer player)
            {
                return Convert.ToInt32(_serverRewardsPlugin.Call("CheckPoints", player.userID));
            }

            public void Give(BasePlayer player, int amount)
            {
                _serverRewardsPlugin.Call("AddPoints", player.userID, amount);
            }

            public void Deduct(BasePlayer player, int amount)
            {
                _serverRewardsPlugin.Call("TakePoints", player.userID, amount);
            }
        }

        #endregion Payment Gateways

        #region Utility Classes

        private static class PermissionUtil
        {
            public static bool VerifyHasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Utility Classes

        #region Helper Functions

        private static void RunEffect(string prefab, Vector3 worldPosition = default(Vector3), Vector3 worldDirection = default(Vector3), Connection effectRecipient = null, bool sendToAll = false)
        {
            Effect.server.Run(prefab, worldPosition, worldDirection, effectRecipient, sendToAll);
        }

        private static void RunEffect(string prefab, BaseEntity entity, uint boneId = 0, Vector3 localPosition = default(Vector3), Vector3 localDirection = default(Vector3), Connection effectRecipient = null, bool sendToAll = false)
        {
            Effect.server.Run(prefab, entity, boneId, localPosition, localDirection, effectRecipient, sendToAll);
        }

        private static bool VerifyPluginBeingLoaded(Plugin plugin)
        {
            return plugin != null && plugin.IsLoaded ? true : false;
        }

        #endregion Helper Functions

        #region Localization

        private class Lang
        {
            public const string CannotUpgrade = "CannotUpgrade";
            public const string NoPermissionOrCannotUpgrade = "NoPermissionOrCannotUpgrade";
            public const string AlreadyAtMaxCapacity = "AlreadyAtMaxCapacity";
            public const string NeedMoreToUpgrade = "NeedMoreToUpgrade";
            public const string UpgradedFromTo = "UpgradedFromTo";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.CannotUpgrade] = "This container cannot be upgraded.",
                [Lang.NoPermissionOrCannotUpgrade] = "You don't have permission or this container cannot be upgraded.",
                [Lang.AlreadyAtMaxCapacity] = "This container is already at maximum capacity.",
                [Lang.NeedMoreToUpgrade] = "You need <color=#FFA500>{0}</color> more <color=#FFA500>{1}</color> to upgrade.",
                [Lang.UpgradedFromTo] = "Container upgraded from <color=#FFA500>{0}</color> to <color=#FFA500>{1}</color> slots."
            }, this, "en");
        }

        private void SendReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}