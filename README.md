# Unity Source Tools

TODO polish this with pictures and videos and stuff.

Here's a video I made a while back:
https://www.youtube.com/watch?v=0a26cHPS-7g

## How to use

1. Convert bsp into vmf with bspsrc.
2. Open vmf with hammer, export fbx/textures with zfbx.
3. Convert bsp with crafty into obj, in order to get the compiled bsp mesh.
4. Use vim macros to fix up material names in the .mtl
5. Open obj with blender in order to load textures properly, then export compiled map as fbx.
6. Set import scale to 0.01 (to match original import's size)
6. Convert and import entire hl2 decals folder (zfbx won't do it for you, and we do spawn decals!)
7. Import vmf as txt
8. Run SourceTools->LoadVMF
9. Place vmf text into vmf text slot
10. Place compiled bsp fbx from crafty into MapFBX slot
11. Set shader to "Standard" (should already be set)
12. Set Rope and Decal prefabs to corresponding prefabs from SourceTools/Prefabs folder. (should already be set)
13. Set Root model folder to where your model folder is located (so if your models folder is `Assets/maps/trainstation01/models/`, then you'd put `Assets/maps/trainstation01/`)
14. Press generate, done!
