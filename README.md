# An extremely simple multiplayer FPS built in two weeks

On the 13th of December 2020 I challenged myself to build a multiplayer
FPS game based on an idea I'd had for a while and then host a play
session all before New Years. I am now writing this readme on the 29th
of December after said play session has concluded thus completing the
challenge.

**This is bad multiplayer code**
Please do not use this as learning material. It was built for the express
purpose of being thrown away and as a result I valued iteration time
over multiplayer correctness. The networking is entirely client authoritative,
there is almost no interpolation, the server crashes if to many players
connect at once (seems to be latency based), ect. If you really want
to learn how multiplayer *should* work then there are a ton of resources
online about the Source Engine networking which are great.

I've made the source publice under WTFPL after being berated to by those
who played during the play session. The repo contains various portions
of [Qodot](https://github.com/Shfty/qodot-plugin) under `addons/qodot`.
Additionally many but not all of the files under `MapAssets/` are derived
from Qodot. These files are all subject to Qodot's original MIT license.
