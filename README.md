# SoundRandomizer

Randomizes the FMOD events Celeste intends to play.

> [!WARNING]
> This mod messes with the FMOD audio system used by Celeste.  
> Playing an FMOD audio event returns an EventInstance, and not all EventInstance references are kept around.
> Losing an EventInstance reference loses the ability to stop said instance.  
> This means that if an infinitely looping event is played (like music), it may in some cases play indefinitely until the game is restarted.
> 
> Please engage in The Silly with care.
