# Concrete Jungle
This mod for the [HBS BattleTech](http://battletechgame.com/) game 

Ideas

* Destroy X% number of buildings to represent battle grounds
* Trigger a building destruction and spawn a handful of units
* Trigger a turret to spawn within a building as an 'infantry attack'
* Create vbied mines with an indicator that they are present
* Trigger an explosion centered on a building
* Could turret spawns represent Dropships? 

DEV NOTES

Interesting methods

* ObstructionGameLogic.SpawnExplodeEffectOnCells, ObstructionGameLogic.ExplodeBuildingIfNeeded
* DropshipGameLogic.SpawnDropshipForFlyby
* Ability.ActivateSpawnTurret
* Ability.ActivateStrafe

Todo: 

* Remove turrets when you move more than N meters away / lose LoS to the building
* Make trap shells tab selectable
* Roll for trap, deploy X traps of a single type?
* Need to store association of turret -> building in a stat for save loading purposes
* Verify AI can't attack/destroy a trap turret directly
* Debug will kill turrets directly
* Test AoE effect ordering
  * How to prevent the turrets from taking damage
* WTF won't building highlighting work? Probably not a huge deal

