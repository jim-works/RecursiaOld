# Recursia
In progress voxel adventure game made in Godot, primarly with C#.

Eventually, I plan on publishing it somewhere (Steam and Itch.io) and leaving it open source. Not sure exaclty what I want to include, I might only share the code for a while.

Current Features:
  - Rough AABB physics
  - Multithreaded world generation and meshing
    - Support for very large, multi-chunk structures
  - Terrain saving/loading
  - Robust block system, blocks can contain arbitrary data
  - Item/Inventory system
    - With crafting
  - Combat system with multiple gun/ammo types
  - Boss fight with Patrick Quack (giant skeleton man)
  - Several enemy types with unique behavior:
    - Chain striker - has many segments connected by spring physics, tries to launch itself at you
    - Marp - chases you, picks you up, and throws you into the air. Carries you to its master (or to the edge of the world). Defeating Patrick gives you the ability to summon your own friendly Marps to fight for you.
  - Very "beautiful" art and models. They're something.

# How to run

Download the latest release from the releases page, or...

Download the project and open in Godot 4.0-mono.
run `dotnet restore`
Run in the editor.

Controls:
  -  WASD/Space to move and jump
  -  1/2 to select item
  -  F11 for fullscreen
  -  ESC for locking/unlocking mouse
  -  Left click to break blocks
  -  Right click to use item

# Notes/Issues

- Currently structures do not properly mesh, so sometimes (quite often) you'll see trees with holes in them.
- The game crashes if the player dies. Skill issue
- Godot 4.0 has a bug where viewport textures do not work in release builds, so healthbars will look weird.

# Future Plans

I would like to develop this into an adventure/town building game. Similar to Terraria but with a much larger focus on the NPCs.
The idea is explore -> find civilization -> fight enemy/play minigame -> earn reputation -> get loot and embassy in your home base from them -> repeat.

All rights reserved.
