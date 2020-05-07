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
* 