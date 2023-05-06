# Wheel RMB Actor
This is a showcase actor using the bus-actor-client to communicate with the rmb.

# Runtime
 ```.Net Core 5.0``` executables are generated for windows, mac and linux by jenkins builds. They are available in the artifactory.


# Building
* Open the ```wheel.sln``` in visualstudio and build the ```actor``` project
* Alternatively you can build & publish using cli, run the following command from the folder containing the actor.csproj file (root folder/actor)
* * ```dotnet publish --configuration Release --runtime win-x64 -p:PublishSingleFile=true  --self-contained true```
* * ```dotnet publish --configuration Release --runtime osx-x64 -p:PublishSingleFile=true  --self-contained true```
* * ```dotnet publish --configuration Release --runtime linux-x64 -p:PublishSingleFile=true  --self-contained true```


## Running
* The solution comes with the ```actor``` project set as startup, just press the start button in visual studio.
* You could also navigate to the output folder from the command line and launch.
* * Then copy the main executable ```Timeplay.WheelOfFortune.Actor```(.exe for windows) and ```log4net.config``` next to the solution file and run former.

### Communication

#### Default Connection Values (lib/rmb.config.json):
##### Local IP: 127.0.0.1
##### RMB Mothership TCP Port: 7128
##### RMB Mothership WebSocket Port: 7129

#### Create the custom target with the name ```wheel```
#### Send the custom target to the all players
```{"cmd":0, "payload": 50}```
#### Ask to pick left or right
```questionId = 1```
```{"cmd":1000, "payload": {"id":questionId, "text": "Pick Left or Right", "left": "Left Text", "right": "Right Text"}}```
#### Players should pick one of the answers
#### Ask the actor to compile the answers
```{"cmd":3, "payload": questionId}```
#### The actor will compile everyting and send the results to the gameserver with the following payload:
```{"cmd":4, "payload": [{left_choices_count},{right_choices_count}]}```
The actor also sends the following to all players:
```{"cmd":5, "payload": questionId```
#### The player can then request its score with the payload
```{"cmd":4, "payload": [questionId,"{player_id}", {player_score}, {int_indicating_correctness} ]```


The code is intended to be very simple and please feel free to request any update of this document.

