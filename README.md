# Concrete Jungle
This is a mod for the [HBS BattleTech](http://battletechgame.com/) game that makes Urban combat more dangerous and exciting. It introduces various types of ambushes that will occur in non-interleaved (i.e. non-combat) mode. When the player's non-interleaved turn ends and control flips to the enemy team, the mod will scan for buildings near to the player's current position. If there are buildings around, there is a chance an ambush is spawned. Ambushes can take multiple forms:

* **Explosion Ambush**: A series of explosions occur near the ambush position. 
* **Infantry Ambush**: Nearby buildings have 'infantry' spawn in them and optionally attack. These buildings will become hostile to the player, and attack each round.
* **Mech Ambush**: A unit of mechs will spawn from a nearby building and optionally have an ambush attack against the player.
* **Vehicle Ambush**: A unit of vehicles will spawn from a nearby building and optionally have an ambush attack against the player.

These ambushes only occur on the 'Urban Hi-Tech' map type introduced in the Urban Warfare DLC. If you do not have the DLC you may not see the effects of this mod at all.

In addition, the mod can pre-destroy a certain percentage of buildings to reflect a war-torn and destroyed urban area. This is controlled by the Devastation settings, below.

This mod relies heavily upon buildings, some of which can be contract targets or objectives. Every effort has been made to exclude contract objectives and important buildings from the list of possible ambush sites. If you notice an error, please open an issue so I can refine the selection filtering.

:warning: This mod requires several other mods to function properly. For each of them, you should download the latest copy and include them in your Mods/ folder.

* [IRBTModUtils](https://github.com/iceraptor/IRBTModUtils/)
* [CustomAmmoCategories](https://github.com/BattletechModders/CustomBundle/tree/master/CustomAmmoCategories)

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

An explosion ambush spawns multiple explosive blasts around an origin point. These blasts will do damage using the [CustomAmmoCategory](https://github.com/BattletechModders/CustomBundle/tree/master/CustomAmmoCategories) AoE semantics, with damage falloff the further the target is from the blast origin. If multiple blasts are configured, each blast past the first occurs in a hex adjacent to the origin hex.

* **Enabled**: Must be true for explosive ambushes to occur. If this is false, and `Explosion`is defined in `Ambush.AmbushWeights`, the mod treats this as a configuration error and will disable all mod functions.

* **VFX**: The visual effect that will be used for blasts. Defaults to `WFX_Nuke` and I suggest you leave it there. Other VFX may have timings that don't work will with the hardcoded durations in the mod.

* **SFX**: The sound effect that will be used for blasts. Defaults to `big_explosion` and I suggest you leave it there, for the same reasons as above.

* **Ambushes**: A list of ambush options for a given difficulty range. Multiple ambush definitions are possible, allowing you to specify values that scale as difficulty increases. You must include ambush definitions for the standard 1-10 difficulty values. No ambush definition can overlap with another ambush definition's difficulty range. 

  * **MinDifficulty** and **MaxDifficulty**: The minimum and maximum contract difficulty that this ambush definition will apply to. All contract difficulties between are included as well, so min of 2 and max of 5 means the ambush definition will apply to difficulty 2, 3, 4 and 5.
  * **MinSpawns** and **MaxSpawns**: When the ambush is spawned, a random value between *MinSpawns* and *MaxSpawns* (inclusive) determines how many blasts will be generated. For a min of 1 and max of 3, between 1 and 3 blasts will trigger on the ambush. Each blast will use a randomly selected value from the **SpawnPool** to resolve the blast.
  * **SpawnPool**: A *weighted* list of blasts that can occur from this ambush definition. Each blast will randomly use one element from this list, so elements that occur more often have a greater chance to be selected.
    * **FloatieTextKey**: A key to a value in the `LocalizedText` dictionary. The associated value will be used as a floatie over the origin of the blast to indicate to the player what type of attack occurred. You should use something short here, like 'HE IED' or 'Inferno Blast'.
    * **Radius**: The range from the origin point in which the targets take damage. This is a float value, representing meters from the origin.
    * **Damage**: The armor and structure damage a target should take from the blast. Recall that this uses the CAC falloff mechanics which will reduce the damage the further from the blast the target is.
    * **Heat**: The heat damage a target should take from the blast. Recall that this uses the CAC falloff mechanics which will reduce the damage the further from the blast the target is.
    * **Stability**: The stability damage a target should take from the blast. Recall that this uses the CAC falloff mechanics which will reduce the damage the further from the blast the target is.
    * **FireRadius**: The number of hexes from the origin that will be set on fire after the blast. This is an integer value, with each hex corresponding to the CAC fire mechanics. See the CAC `settings.json` for `BurningForestCellRadius` , which defaults to 4 cells (where a cell is 5m wide).
    * **FireStrength**: The CAC 'strength' of the fire hexes that will be generated. An integer value that determines how much additional heat a unit takes.
    * **FireChance**: The chance (as an float) that a hex will be set on fire by the hex. 
    * **FireDurationNoForest**: The number of turns (as an integer) a non-forest hex should remain on fire from the blast. 

  

### Infantry Ambush Configuration

An Infantry Ambush spawns an invisible, invincible turret within a building. The outer building then is marked as having weapons, and will display the turret's name on the building's combat HUD. This allows a building to approximate being inhabited by infantry, who then attack their targets from within. This turret exists so long as the building exists, and will be removed when the building is destroyed. It fires from a position approximately 70% up the building's height, to better represent infantry shooting from the higher floors of a building.

* **Enabled**: Must be true for infantry ambushes to occur. If this is false, and `Infantry`is defined in `Ambush.AmbushWeights`, the mod treats this as a configuration error and will disable all mod functions.
* **FreeAttackEnabled**: If true, the spawned turrets will resolve a free round of attacks against the closest target on the turn the spawn. This provides an 'ambush' feeling, but can be very crippling.
* **Ambushes**: A list of ambush options for a given difficulty range. Multiple ambush definitions are possible, allowing you to specify values that scale as difficulty increases. You must include ambush definitions for the standard 1-10 difficulty values. No ambush definition can overlap with another ambush definition's difficulty range. 
  * **MinDifficulty** and **MaxDifficulty**: The minimum and maximum contract difficulty that this ambush definition will apply to. All contract difficulties between are included as well, so min of 2 and max of 5 means the ambush definition will apply to difficulty 2, 3, 4 and 5.
  * **MinSpawns** and **MaxSpawns**: When the ambush is spawned, a random value between *MinSpawns* and *MaxSpawns* (inclusive) determines how many blasts will be generated. For a min of 1 and max of 3, between 1 and 3 blasts will trigger on the ambush. Each blast will use a randomly selected value from the **SpawnPool** to resolve the blast.
  * **SpawnPool**: A *weighted* list of blasts that can occur from this ambush definition. Each blast will randomly use one element from this list, so elements that occur more often have a greater chance to be selected. 
    * **TurretDefId**: The turret definition that should be used to represent the infantry. Example: *turretdef_Light_Shredder*
    * **PilotDefId**: The pilot definition that will be attached to the spawned turret. Example: *pilot_d5_turret*

### Mech Ambush Configuration

A Mech Ambush spawns one or more units within nearby buildings. The buildings will be destroyed during the spawning process, with a small quip being played beforehand to taunt the player. 

* **Enabled**: Must be true for mech ambushes to occur. If this is false, and `Mech` is defined in `Ambush.AmbushWeights`, the mod treats this as a configuration error and will disable all mod functions.
* **FreeAttackEnabled**: If true, the spawned units will resolve a free round of attacks against the closest target on the turn the spawn. This provides an 'ambush' feeling, but can be very crippling.
* **Ambushes**: A list of ambush options for a given difficulty range. Multiple ambush definitions are possible, allowing you to specify values that scale as difficulty increases. You must include ambush definitions for the standard 1-10 difficulty values. No ambush definition can overlap with another ambush definition's difficulty range. 
  * **MinDifficulty** and **MaxDifficulty**: The minimum and maximum contract difficulty that this ambush definition will apply to. All contract difficulties between are included as well, so min of 2 and max of 5 means the ambush definition will apply to difficulty 2, 3, 4 and 5.
  * **MinSpawns** and **MaxSpawns**: When the ambush is spawned, a random value between *MinSpawns* and *MaxSpawns* (inclusive) determines how many blasts will be generated. For a min of 1 and max of 3, between 1 and 3 blasts will trigger on the ambush. Each blast will use a randomly selected value from the **SpawnPool** to resolve the blast.
  * **SpawnPool**: A *weighted* list of blasts that can occur from this ambush definition. Each blast will randomly use one element from this list, so elements that occur more often have a greater chance to be selected. 
    * **MechDefId**: The mech definition that should be used to represent the infantry. Example: *mechdef_urbanmech_UM-R60*
    * **PilotDefId**: The pilot definition that will be attached to the spawned turret. Example: *pilot_d3_gunner*

### Vehicle Ambush Configuration

A Vehicle Ambush spawns one or more units within nearby buildings. The buildings will be destroyed during the spawning process, with a small quip being played beforehand to taunt the player. 

* **Enabled**: Must be true for mech ambushes to occur. If this is false, and `Vehicle` is defined in `Ambush.AmbushWeights`, the mod treats this as a configuration error and will disable all mod functions.
* **FreeAttackEnabled**: If true, the spawned units will resolve a free round of attacks against the closest target on the turn the spawn. This provides an 'ambush' feeling, but can be very crippling.
* **Ambushes**: A list of ambush options for a given difficulty range. Multiple ambush definitions are possible, allowing you to specify values that scale as difficulty increases. You must include ambush definitions for the standard 1-10 difficulty values. No ambush definition can overlap with another ambush definition's difficulty range. 
  * **MinDifficulty** and **MaxDifficulty**: The minimum and maximum contract difficulty that this ambush definition will apply to. All contract difficulties between are included as well, so min of 2 and max of 5 means the ambush definition will apply to difficulty 2, 3, 4 and 5.
  * **MinSpawns** and **MaxSpawns**: When the ambush is spawned, a random value between *MinSpawns* and *MaxSpawns* (inclusive) determines how many blasts will be generated. For a min of 1 and max of 3, between 1 and 3 blasts will trigger on the ambush. Each blast will use a randomly selected value from the **SpawnPool** to resolve the blast.
  * **SpawnPool**: A *weighted* list of blasts that can occur from this ambush definition. Each blast will randomly use one element from this list, so elements that occur more often have a greater chance to be selected. 
    * **VehicleDefId**: The mech definition that should be used to represent the infantry. Example: *vehicledef_CARRIER_SRM*
    * **PilotDefId**: The pilot definition that will be attached to the spawned turret. Example: *pilot_d3_gunner*

### Quips Configuration

Most ambushes will play a short taunt before the effects are resolves. These are configured through the `Quips` element in mod.json. All quips sections are lists of strings, one of which will be randomly determined and used during an ambush. The different types of ambushes are served by different quips:

* **ExplosiveAmbush**: One of these will be used during an explosive ambush.
* **InfantryAmbush**: One of these will be used during an infantry ambush
* **SpawnAmbush**: One of these will be used during Mech or Vehicle ambushes.

## DEV NOTES

### Todo

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

* Explosives should be compatible with CAC

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

  

  



