# NoTransitions

A Hollow Knight mod that removes transitions to provide a more seamless experience.

This mod is currently unfinished and is here for demonstration purposes only.
Feel free to explore, but expect some buggy behavior.

## Things to know
- I recommend having Benchwarp or Debug installed to fix things that will break.
- You can use benchwarp to set your save point, but you will need to quit and reopen the save file to respawn properly.

## Known bugs/issues
- Hazard respawns and benchwarping can put you in strange places (quitting to the main menu should fix this)
- Upward transitions are impossible without Claw or Wings
	- In fact, attempting to jump up can produce buggy behavior (described below)
- If a room has multiple transitions to the same adjacent room, the exits may not be aligned properly
- If your computer is too slow, you may fall through the floor upon entering a room
- Unusual movement in/near transitions may cause both adjacent rooms to have collision disabled
	- This can include taking damage, moving back and forth too quickly, misalignment due to multiple transitions to the same room, or other random cases
- Some transitions are just blatantly misplaced (Notably the well in Dirtmouth)
- Internal scenes (such as boss arenas) do not appear correctly
- Travelling too far downward from your savepoint can cause you to hit the hazard respawn box that refuses to listen to me
	- This is at y=-130 and spans from x=-610 to 910
- In rooms with breakable walls/floors leading to the next room, the barrier will not immediately update on the other side
	- For a temporary solution, leave the room through a different transition and come back
- In some cases, the state of the room will not be saved properly (i.e. bosses fought, levers flipped, etc)
- The lighting/color of the room will reflect the last scene loaded, which can mean the wrong room is dark and requires Lantern
- Unexpected hidden objects can sometimes be seen outside the room or below the floor
	- This includes enemy templates, assorted visual effects, etc
- Some black boxes are not properly disabled
- The platforms in west Crossroads will become invisible on contact
- Some enemies do not move or move incorrectly
- Audio doesn't always follow you into the next room
- Geo rocks might appear in the wrong locations
- Spontaneous hazard respawn???
- Probably some other stuff I either forgot about or haven't found yet

## Things I'm pretty sure I've fixed but might still be broken
- I rewrote a lot of the camera controller to be able to see past the scene boundaries
- Swimming in water/acid might teleport you to the wrong y-level