# AutoBroadcast
![Version 2.0.0](https://img.shields.io/badge/Version-2.0.0-blue.svg)
![API 2.0](https://img.shields.io/badge/API-2.0-green.svg)

Introduction
-----
Pre-defined messages can be broadcasted to players in a variety of ways such as time-based intervals or triggers by certain words in chat or by entering a region.

Configuration
-----
_AutoBroadcastConfig.json_

| Option | Type | Default |
|---|---|---|
| Broadcasts | Broadcast[] | |

_Broadcast_

| Option | Type | Description |
|---|---|---|
| Name | string | |
| Enabled | boolean | |
| Messages | string[] | |
| ColorRGB | float[] | |
| Interval | int | The interval in seconds for time-based broadcasts. |
| StartDelay | int | The delay in seconds before sending the first broadcast. |
| TriggerRegions | string[] | A list of regions where the broadcast will be triggered upon a player entering. |
| RegionTrigger | string | "none": Broadcasts to all players<br />"region": Broadcasts to players in current region<br />"self": Broadcasts only to triggering player. |
| Groups | string[] | A list of groups that the broadcast is displayed to or the groups that can trigger the broadcast. Use `*` for all groups. | 
| TriggerWords | string[] | A list of words that will trigger the broadcast. |
| TriggerToWholeGroup | boolean | |