## 0.0.6

QoL update! Basically a small intermediate update with lots of random stuff that will hopefully make the gameplay a bit smoother. AE2 will be next update

Important:
- Added Magic Storage gregtech piping support, both in/out from heart and access points
- Added Manual Crafting Stations for better Magic Storage compatibility and reducing recipe amount when hand crafting
- Added Gregtech Mutlitool for better wires/pipes setup UX (thanks Flungus)
- Added functionality to Prospector device (thanks Flungus, TrRzeczy)
- Added terraria metals superconductors (thanks TrRzeczy)
- Fixed plascrete recipe (thanks CreepersX17)
- Fixed distillery first output (thanks CreepersX17)
- Fixed gray pressure plate recipe (thanks hhhhhhhhhhhhhhh)
- Fixed removed calcite recipes
- Changed compat recipes internal format for easier contributions
- Changed questbook data storage so its multiplayer compatible and all the players share the same progress
- Removed duplicated ingots, with legacy conversion recipes (thanks Flungus)
- Removed shimmer recipes for all gt stuff because people are unable to self-restrain from duping stuff
- Removed shimmer for substituded vanilla ingots, can enable back in mod config

QOL:
- Added ultimate battery as a requirement of the final Gregith (thanks Flungus)
- Added nether star recipe from mana stars (thanks Royal Tek)
- Added questbook editor
- Added shimmer and honey fluids for later compatibility
- Added bottomless bucket extractor recipes (thanks TheLarvi)
- Added recipes between Chum Bucket and fermented biomass
- Added hotkeys for TMI and questbook
- Added more rubber into starter bags
- Changed raw rubber dust yield so its more rewarding to automate it (thanks Bup Supreme)
- Changed recipe browser favorites list to be persistent on the player
- Changed questbook to track tasks separately (thanks Leha44581)

Visuals:
- Added move/resize to recipe browser, improved visuals (thanks Flungus)
- Added pin button to recipe browser
- Added gt armor sprites (thanks Flareguy)
- Added multiblocks, saw actions, ore veins in recipe browser (thanks hamood)
- Added tooltip to wooden form so its obvious its an intentional recipe change
- Changed nether star texture
- Changed the rest of wood textures
- Fixed dimension condition tooltip in recipe browser (thanks TheLarvi)
- Removed all the unresolved recipes from the recipe browser

## 0.0.5

Bugfix update! I want to get core gregtech logic to a full stability until introducing new mechanics and overhauls

Important:
- Added sulfur and redstone trades to EBF chan until we figure out the better early game source
- Added extractinator item piping support (thanks Three)
- Changed Large Miner outputs (thanks HiKoNe)
- Changed primitive water pump to give a bit more water to avoid extreme pump spam (thanks Three)
- Changed battery buffer to support directional in/out (thanks TrRzeczy, TheLarvi)
- Fixed non-normal simple pipes drop item (thanks HiKoNe)
- Fixed fisher, item collector, miner pipes interactions (thanks PotatoSagall)
- Fixed hammer having pickaxe abilities (thanks Leha44581)
- Fixed Shift+click logic in storages (thanks Three)
- Fixed wires overamperage logic and battery buffers energy dupe (thanks TheLarvi, TrRzeczy)
- Fixed super tank capacity (thanks HiKoNe)
- Fixed parallel recipes output for byproducts
- Removed gt default harderRods because Lathe should be better (thanks Flungus)

QOL:
- Added fluid drum RMB onto fluid slots (thanks Three)
- Added terraria coins packing recipe
- Added more safe ore substitutions
- Added gt ores to vanilla ores recipe
- Added medium oil source (thanks HiKoNe)
- Added shovel chance to get bait from grass (thanks Flareguy)
- Added FAQ to EBF Chan until proper questbook chapter
- Fixed auto-output into bus lag (thanks TheLarvi)
- Fixed recipe browser space tokenization logic
- Removed whitelist by default
- Removed 999 stack size limit, was inconvenient (thanks Three)

Visuals:
- Added drums fluid render in inventory
- Changed ore block render to fit terraria style
- Changed cables render in inventory (thanks PotatoSagall)
- Fixed fluid drilling rig recipe browser tooltip (thanks TrRzeczy)
- Fixed solar boiler tooltips (thanks Leha44581)
- Fixed multiblock ghosts render near formed multiblocks
- Fixed warning tooltips color render (thanks TheLarvi)
- Fixed steam machines energy consumption tooltip (thanks TheLarvi)
- Fixed terraria paint onto hatches (thanks TrRzeczy)
- Fixed EBF coils active state of world rejoin
- Fixed parallel hatches texture
- Fixed sound collision when lots of machines are running
- Removed stack size tooltip conversion (thanks TheLarvi)

## 0.0.4

Pipe update! Wires/pipes now have a proper neat render, animated in world and can intersect (check Pipe Intersection item)

- Added native gt tooltips for crafting ingredients (thanks Konomi)
- Added nether star to WoF drop pool (thanks Flungus, hamood)
- Added redstone relation to amber (thanks Neko CR-21)
- Added bigger variations of simple pipes (thanks Flungus)
- Added warning to empty whitelist on filters for better UX (thanks TrRzeczy)
- Added questbook wip disclaimer (thanks megacraftbuilder)
- Added extra resin source
- Added ability to use nether stars in a star cannon (thanks Royal Tek)
- Changed oil drilling rig to give you more oil per cycle (thanks TrRzeczy)
- Changed solar boilers and panels sky check so its more convenient to use (thanks TrRzeczy, TheLarvi)
- Changed ore rarity so lava won't destroy them (thanks hamood)
- Fixed running recipe loading inconsistency on changing mods (thanks TrRzeczy)
- Fixed duplicated mining speed tooltip (thanks Konomi)
- Fixed EBF-chan name render and census support (thanks Konomi for implementation)
- Fixed recipe browser chance render for very low chances (thanks TrRzeczy)
- Fixed trees dropping rubber wood instead of rubber logs, added acorns (thanks HiKoNe, Natsuki)
- Fixed EoW and BoC loot droprates (thanks HiKoNe)
- Fixed wrenches pipe highlight (thanks hamood)
- Fixed edge case with 2 touching fluid pipes of different type interacting with the same tile (thanks PotatoSagall)
- Fixed several obsolete quests from stone and steam age (thanks Flungus)

## 0.0.3

- Added recipe browser station query clicks (thanks PotatoSagall)
- Added ender pearls to LV bosses drop pool for earlier fluid voiding cover (thanks Neko CR-21)
- Added missing Fluid Detector and Item Detector Cover recipe as crafting ingredient
- Added clay recipe from sand and water (thanks PotatoSagall)
- Added terraprisma gt recipe (thanks abxx5)
- Added so you can use gregtech rounds in guns (thanks PotatoSagall)
- Added vanilla gems compat conversions (thanks hamood, thanks pruberu for implementation)
- Changed EBF-chan to spawn on world creation, updated visuals (thanks QiuQiu)
- Changed pipes logic so they can interact from behind of the machines (thanks Dedust, ADtsd)
- Changed recipe browser search logic to ignore crafting tools when those aren't relevant
- Changed fluid slot render (thanks TrRzeczy)
- Fixed compatibility with Quality of Terraria mod (internal name: ImproveGame) (thanks quickgk)
- Fixed multiblock macerator picked up unresolved recipes as valid without input (thanks TrRzeczy)
- Fixed ghost circuits dropping in world (thanks TrRzeczy)
- Fixed wood planks recipes inconsistency, removed stripped wood (thanks Flungus)
- Fixed hoe couldn't harvest from planter boxes (thanks TrRzeczy)
- Fixed steam machines speed debuff (thanks TrRzeczy)
- Fixed parallel hatch active recipe state on world reload (thanks TrRzeczy)
- Fixed mining hammer aoe mining power and machine removal (thanks TrRzeczy, Char)
- Fixed crafting station icons in recipe browser for hand recipes (thanks Char, PotatoSagall)
- Fixed axe power (thanks MrKo_no)
- Fixed steam multiblocks casings (thanks TrRzeczy)
- Fixed multiplayer multiblock recipe display sync output amounts (thanks TrRzeczy)
- Fixed pre-hv macerator byproduct (thanks TrRzeczy)
- Fixed right click to open machine UI when holding item (thanks TrRzeczy)
- Fixed slow Magic Storage loading time (thanks Flungus)
- Fixed multiblock preview render overhead
- Fixed tools render rotation (thanks pruberu)
- Disabled crate+tape interaction until its fixed (thanks hamood)

Existing worlds should be fine to update

## 0.0.2

- Added gregtech-ported localization
- Fixed search bar non-english typing
- Fixed Steam Boiler ticking
- Fixed fluid crafting in hand
- Fixed several minor bugs
