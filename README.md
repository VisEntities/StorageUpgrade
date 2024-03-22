This plugin enables players to upgrade the capacity of their storage containers by hitting them with a hammer. You can charge for upgrades and decide how many slots each hit adds to the container.

[Demonstration](https://youtu.be/XCTpC5NmGPs)

-----------------

## Upgrading
Hit the container with a hammer while holding down the right mouse button.

-----------------

## Upgrade Profiles
When creating a new profile, assign it a unique name (suffix), as this will be used to construct the profile permission. For example, a profile named `vip` results in the permission `storageupgrade.vip`, which the plugin automatically constructs for you.

You can charge for upgrades using inventory items, points ([Server Rewards](https://umod.org/plugins/server-rewards)), or coins ([Economics](https://umod.org/plugins/economics)). Use the item shortname for items, `points` for Server Rewards, and `coins` for Economics.

-------------------

## Notes
- Certain containers, like ovens, recyclers, and repair benches, cannot be upgraded due to game restrictions. These are listed under `NonUpgradeableContainers` in the config. Just make sure to update this list with any new non-upgradeable containers introduced to the game.
- Picking up and placing the container again will reset its inventory capacity to the original size.

---------

## Configuration

```json
{
  "Version": "2.0.0",
  "Non Upgradeable Containers": [
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
    "dropbox"
  ],
  "Profiles": {
    "basic": {
      "Enabled": true,
      "Expand Storage By": 6,
      "Container Shortnames": [
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
      ],
      "Upgrade Cost": [
        {
          "Enabled": true,
          "Name": "wood",
          "Amount": 100
        },
        {
          "Enabled": true,
          "Name": "metal.fragments",
          "Amount": 100
        }
      ]
    },
    "vip": {
      "Enabled": true,
      "Expand Storage By": 6,
      "Container Shortnames": [
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
      ],
      "Upgrade Cost": [
        {
          "Enabled": false,
          "Name": "coin",
          "Amount": 1000
        },
        {
          "Enabled": false,
          "Name": "point",
          "Amount": 1000
        }
      ]
    }
  }
}
```

---------------

## Localization

```json
{
  "CannotUpgrade": "This container cannot be upgraded.",
  "NoPermissionOrCannotUpgrade": "You don't have permission or this container cannot be upgraded.",
  "AlreadyAtMaxCapacity": "This container is already at maximum capacity.",
  "NeedMoreToUpgrade": "You need <color=#FFA500>{0}</color> more <color=#FFA500>{1}</color> to upgrade.",
  "UpgradedFromTo": "Container upgraded from <color=#FFA500>{0}</color> to <color=#FFA500>{1}</color> slots."
}
```

------------------

## Credits

 * Rewritten from scratch and maintained to present by **VisEntities**
 * Originally created by **Orange**, up to version 1.5.1