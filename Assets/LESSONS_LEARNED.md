# LESSONS LEARNED

## 🌐 Distributed Authority, but not too much
That's a cool way of programming the multiplayer logic.
By giving permission to clients to edit the game state,
based on the ownership of network objects, less responsibility is placed upon the host and less RPC need to be invoked.
That sounds amazing, but there is one big drawback.

### Desync problems—To each their own
Expertise is needed for such an agile way of programming.
Desynchronization problems hide around the corner and, **being non-deterministic, are a huge pain to debug.**

It's not like you get a runtime error at a given line in a given script.
That would be clear and pretty straightforward to address.
Desync problems, on the other hand, are hideous.
You may get frozen in the spawn without any possibility to move
because your rigidbody components missed their chance to disable the ragdoll configuration.
**In short, racing conditions are the cause behind this.**
At least that's what I came to realize.

### Server authority—The server holds the cards
Instead of giving each client the possibility to edit the game state, treat the latter as a singleton data.
Clients can read it, listen to changes, but cannot edit it, at least not direcly.
Any client edit must be requested to the server.

> Game = State + Rendering

Each client can handle the rendering part by their own, but the (writing of) state is where racing conditions hide.

### Inputs are the source of state editing (other than time)
_But when do clients need to edit the state?_ When the player **gives an input**. The input can be:
- a **trigger** input, like firing a weapon with the left thumb or jumping with `spacebar`,
- or a **continuous** input, like pressing `W` to move forward.

While a trigger input can often easily relay on a `ServerRPC` to deliver the state editing request,
the continuous inputs may require a more complex handling.

> By combining **client prediction**, **server reconciliation**, and **buffered interpolation**, you’ll get smooth, responsive movement even under high ping or packet loss—without giving clients unchecked authority.


## Singleton
Don't fear static fields in Unity! They preserve old values only when serialized and when are not initialized.
Look at the Singleton abstract class in this project. It is should be used for all the managers, it's much easier.