# Concrete Jungle
This is a mod for the [HBS BattleTech](http://battletechgame.com/) game that makes Urban combat more dangerous and exciting. It introduces various types of ambushes that will occur in non-interleaved (i.e. non-combat) mode. When the player's non-interleaved turn ends and control flips to the enemy team, the mod will scan for buildings near to the player's current position. If there are buildings around, there is a chance an ambush is spawned. Ambushes can take multiple forms:

* **Explosion Ambush**: A series of explosions occur near the ambush position. 
* **Infantry Ambush**: Nearby buildings have 'infantry' spawn in them and optionally attack. These buildings will become hostile to the player, and attack each round.
* **Mech Ambush**: A unit of mechs will spawn from a nearby building and optionally have an ambush attack against the player.
* **Vehicle Ambush**: A unit of vehicles will spawn from a nearby building and optionally have an ambush attack against the player.

These ambushes only occur on the 'Urban Hi-Tech' map type introduced in the Urban Warfare DLC. If you do not have the DLC you may not see the effects of this mod at all.

In addition, the mod can pre-destroy a certain percentage of buildings to reflect a war-torn and destroyed urban area. This is controlled by the Devastation settings, below.

This mod relies heavily upon buildings, some of which can be contract targets or objectives. Every effort has been made to exclude contract objectives and important buildings from the list of possible ambush sites. If you notice an error, please open an issue so I can refine the selection filtering.

:warning: This mod requires [IRBTModUtils](https://github.com/iceraptor/IRBTModUtils/) - download the latest copy and include in your Mods/ folder.



## Configuration

The mod is designed to be heavily configurable, and express that configuration through mod.json settings. It's brown down into multiple sections, which are covered in sequence. General settings are:

* **Debug**: If true, the *Mods/ConcreteJungle/concrete_jungle.log* will be more verbose.
* **Trace**: If true, the *Mods/ConcreteJungle/concrete_jungle.log* will include every line.

:information_source: Many mod options have an `Ambushes: [ ]`. These represent different ranges of contract difficulty that you can configure. You MUST supply an appropriate ambush definition for all possible contract difficulties. If a ambush definition cannot be found for the contract's difficulty, it will be discarded and that contract will not experience any mod logic. Ranges cannot overlap and must be distinct - so you cannot have 1-3 and 2-4 ranges, for instance. 

### Ambush Configuration

These options are available under the `Settings.Ambush`:

* **MaxPerMap**: The total number of ambushes that will spawn per map (*integer*)
* **MinDistanceBetween**: A minimum distance between ambush origins. Once an ambush is triggered, another ambush won't spawn until the player's units have moved as least this far away. Defaults to 300m. (*float*)
* **BaseChance**: The base chance for an ambush to spring each turn. This base chance increases for each actor that the player activates (see below). Defaults to 0.3 or 30%. (*float*)
* **ChancePerActor**: The incremental chance for an ambush to spawn for each player actor that activates. Defaults to 0.05f or 5% (*float*).
* **SearchRadius**: When determining if an ambush should be triggered, the algorithm will search from the origin point up to this radius for suitable buildings. If insufficient buildings are found, the ambush will not occur.
* **AmbushWeights**: An array of ambush types, weighted for frequency. Values must be `Explosion`, `Infantry`, `Mech`, or `Vehicle`.  When an ambush is triggered, a random selection will be made from list for the type of ambush to use. More frequent values will therefore be more common.

### Devastation Configuration

Devastation will pre-destroy a certain percentage of the buildings, making the map look war-torn and ravaged. Note that this adds a few extra seconds to Urban map loads, typically on the order of 2-5 seconds.

* **Enabled**: If false, devastation will not occur.
* **DefaultRange**: A default value to use for devastation when planetary tags don't match
  * **MinDevastation**: The minimum percentage of buildings to remove. Defaults to 0.30 or 30% (*float*).
  * **MaxDevastation**: The maximum percentage of buildings to remove. Defaults to 0.90 or 90% (*float*).

### Explosion Ambush Configuration

An explosion ambush will cause a random number of explosive blasts to occur at the origin position. The explosion will use 

* Enabled

* SearchRadius

* Ambushes

  * MinDifficulty
  * MaxDifficulty
  * MinSpawns
  * MaxSpawns
  * SpawnPool

  

### Infantry Ambush Configuration

Loreum ipsum

* Enabled
* FreeAttackEnabled 
* SearchRadius
* Ambushes
  * MinDifficulty
  * MaxDifficulty
  * MinSpawns
  * MaxSpawns
  * SpawnPool

### Mech Ambush Configuration

Loreum ipsum

* Enabled
* FreeAttackEnabled 
* SearchRadius
* Ambushes
  * MinDifficulty
  * MaxDifficulty
  * MinSpawns
  * MaxSpawns
  * SpawnPool

### Vehicle Ambush Configuration

Loreum ipsum

* Enabled
* FreeAttackEnabled 
* SearchRadius
* Ambushes
  * MinDifficulty
  * MaxDifficulty
  * MinSpawns
  * MaxSpawns
  * SpawnPool



## DEV NOTES

### Todo

* Spawns should be rotated towards ambush origin

* Need to move turret spawns upwards

* Remove turrets when you move more than N meters away / lose LoS to the building

* Need to store association of turret -> building in a stat for save loading purposes

* Verify AI can't attack/destroy a trap turret directly

* Test turret invulnerability 

* Test AoE effect ordering

  * How to prevent the turrets from taking damage

* WTF won't building highlighting work? Probably not a huge deal

* Make sure turrets salvage isn't included

  * From Denadan - remove them from enemies after death

  * ```
        var turrets = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Turret>()
            .Where(t => t.IsDead)
    ```

    

* I have two suggestions: (a) Toggle or slider for having infantry be more likely if the target owns the planet. (b) falling buildings do damage, mostly STAB damage

* Collapse buildings from jumping if tonnage > structure points. Alternatively - jumping onto a building does X damage? Probably too much.

* Add ambushes that target the enemy or enemies in a 3 way 

* Add ambushes that are hostile to all

* Explosion should put up a floatie that gives the blast name

* Should explosions use CAC AoE weapons?

  * I think if they use a weapon with 1 or 2 range, this should work by default... need to test

* Add mech ambush (equivalent to vehicle)

* Can we make map devastation more efficient? Add it earlier in the processing?

### Ideas

* Destroy X% number of buildings to represent battle grounds
* Trigger a building destruction and spawn a handful of units
* Trigger a turret to spawn within a building as an 'infantry attack'
* Create vbied mines with an indicator that they are present
* Trigger an explosion centered on a building
* Could turret spawns represent Dropships? 

### Interesting methods

* ObstructionGameLogic.SpawnExplodeEffectOnCells, ObstructionGameLogic.ExplodeBuildingIfNeeded
* DropshipGameLogic.SpawnDropshipForFlyby
* Ability.ActivateSpawnTurret
* Ability.ActivateStrafe

* 

  

  



