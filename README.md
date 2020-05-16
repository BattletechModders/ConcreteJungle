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

* Ambushes should spawn a new lance on the team.

* Need to store association of turret -> building in a stat for save loading purposes

* Verify AI can't attack/destroy a trap turret directly

* Debug will kill turrets directly

* Test AoE effect ordering
  
  * How to prevent the turrets from taking damage
  
* WTF won't building highlighting work? Probably not a huge deal

* Make sure turrets salvage isn't included

  * From Denadan - remove them from enemies after death

  * ```
                        var turrets = Contract.Contract.BattleTechGame.Combat.AllEnemies.OfType<Turret>()
                            .Where(t => t.IsDead)
    ```

    

* Check that a building isn't marked a condition mid-mission

* Remove candidate buildings when they become infantry / vehicle traps

*  I have two suggestions: (a) Toggle or slider for having infantry be more likely if the target owns the planet. (b) falling buildings do damage, mostly STAB damage

* Collapse buildings from jumping if tonnage > structure points. Alternatively - jumping onto a building does X damage? Probably too much.

* Add ambushes that target the enemy or enemies in a 3 way 

* Add ambushes that are hostile to all

* Explosion should put up a floatie that gives the blast name

* Should explosions use CAC AoE weapons?

  * I think if they use a weapon with 1 or 2 range, this should work by default... need to test

* Add mech ambush (equivalent to vehicle)

* Can we make map devastation more efficient? Add it earlier in the processing?

  

  



