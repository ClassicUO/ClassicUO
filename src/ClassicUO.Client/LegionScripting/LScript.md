# Legion Scripting




# Commands

## `msg`

`msg` *text*  
Make your player say something in game  
Example:  
`msg I banish thee!`  

## `togglefly`

If your player is a gargoyle this will send a toggle fly request  

## `useprimaryability`

Use your primary ability

## `usesecondaryability`

Use your secondary ability

## `clickobject`

`clickobject` 'serial'  
Example:
`clickobject 'self'` or `clickobject '0x1234567'`

## `attack`

`attack` 'serial'  
Example:  
`attack 'self'` or `attack '0x1234567'`


## `bandageself`

Attempt to bandage yourself


## `useobject`

Use an object(Double click)

| `useobject` | 'serial' | *'true/false'* |
| - | - | - |
| | Object serial | Use double click queue (Not required, default true) |

Example:  
`useobject '0x1234567'` or `useobject '0x1234567' false`


## `target`

Target an object or mobile  
`target` 'serial'  

Example:  
`target 'self'`



## `waitfortarget`

| `waitfortarget` | '0/1/2' | '10000' |
| - | - | - |
| | 0 = Any target type, 1 = harmful, 2 = beneficial | Timeout, 10000 is default(10 seconds) |

Example:  
`waitfortarget '0'`


## `usetype`

| `usetype` | 'container' | 'objtype(graphic)' | 'hue(optional)' |
| - | - | - | - |
| | Container serial | Object type | Hue(If not included, any hue will match) |

Example:  
`usetype 'backpack' '0x456'` or `usetype 'backpack' '0x456' '32'`


## `pause`

Pause the script for a duration of time in milliseconds  
`pause` 'duration in ms'  
Example: `pause 1000`


## `useskill`

Use a skill from skill name  
`useskill` 'skillname(Can be a partial name)'  
Example: `useskill 'evaluate'`


## `walk` or `run`

Send a walk or run request to the server  
`walk`/`run` 'direction'  
 
| Directions |
| - |
| north |
| right |
| east |
| down |
| south |
| left |
| west |
| up |  

Example: `run 'north'`


## `canceltarget`

Clear a target cursor if there is one


## `sysmsg`

Display a message in your system messages(Not sent to the server)  

| `sysmsg` | msg | hue |
| - | - | - |
| | required | optional |

Example: `sysmsg 'No more tools!' '33'`


## `moveitem`

Move an item to a container  
`moveitem` 'item' 'container' 'amount(optional)'  
If amount is 0 or not used it will attempt to move the entire stack  
Add ! to move items without stacking them: `moveitem!`  

Example: `moveitem '0x23553221' 'backpack'`


## `moveitemoffset`

Move an item to the ground near you  

| `moveitemoffset` | 'item' | 'amt' | 'x offset' | 'y offset' | 'z offset' |
| - | - | - | - | - |
| | | Use 0 to grab the full stack | | | |

Example: `moveitemoffset '0x32323535' '0' '1' '0' '0'`


## `cast`

Cast a spell by name  
`cast` 'spell name'  
Spell name can be a partial match  

Example: `cast 'greater he'`


## `waitforjournal`

Wait for text to appear in journal  
`waitforjournal` 'the text' 'duration in ms'  
Example: `waitforjournal 'you begin' '15000'` <- this waits for up to 15 seconds


## `settimer`

Create a timer. If the timer already exists, this is ignored until the timer expires.  
`settimer` 'name' 'duration'  
Note: Timers are shared between scripts so make sure to name them uniquely.  
Example: `settimer '123bandage' '10000'`   //Create a timer named 123bandage with a duration of 10 seconds


## `removetimer`

Remove a timer  
`removetimer 'name'`  
Example: `removetimer '123bandage'`


## `setalias`

Set an alias to a serial  
`setalias` 'name' 'serial'  
Example: `setalias 'pet' '0x1234567'`  


## `unsetalias`

Unset an alias  
`unsetalias 'pet'`  


## `movetype`

Move any object matching the type  
`movetype 'graphic' 'source' 'destination'  [amount] [color] [range]`  
Amount and color are optional  
Example: `movetype 0x55 'backpack' 'bank'`  


## `toggleautoloot`

Toggle the built in simple auto loot system  
`toggleautoloot`



## `info`

Create a target cursor to show info about an object  
`info`


## `setskill`

Set skill locked/up/down  
`setskill 'name' 'up/down/locked`  
Example: `setskill 'hiding' 'locked'`


## `getproperties`

Request item props from the server  
`getproperties 'serial'`  
Example: `getproperties 'found'`  
Note: This will pause the script until we have received the properties unless you add ! modifier  
```
getproperties 'found' #Script will pause here until we have the properties
if properties 'found' 'some text'
  sysmsg 'we found the holy grail'
endif
```


## `turn`

Turn your character in a direction  
`turn 'direction'`  
Example: `turn 'north'`  



## `createlist`

Create a list  
`createlist 'name'`  
Example: `createlist 'pets'`  



## `pushlist`

Add a value to a list  
`pushlist 'name' 'value' ['front']`  
Example: `pushlist 'pets' '0x8768766' 'front'`  
Note: `front` is optional, it will be added to the back of the list by default  
Add `!` modifier(`pushlist!`) to only add the item if it is not already in the list  


## `clearlist`

Clear a list of it's items  
`clearlist 'name'`


## `removelist`

Remove a list  
`removelist 'name'`


## `rename`

Rename a pet  
`rename 'serial' 'name'`  
Example: `rename '0x3435345' 'KillMe'`


## `logout`

Logout of the game  
`logout`


## `shownames`

Show all names of mobiles  
`shownames`


## `togglehands`

Equip/Unequip items in hand  
`togglehands 'left|right`  
Example: `togglehands 'left'`


## `equipitem`

Equip an item using its serial number.  
`equipitem 'serial'`  
Example: `equipitem '0x40001234'`


## `togglemounted` 

Toggle mounted state for the player.  
`togglemounted`  
Example: `togglemounted`  


## `promptalias` 
Prompt the user to set an alias for a targeted object.  
`promptalias 'name'`  
Example: `promptalias 'myAlias'`


## `waitforgump` 
Wait for a gump to appear.  
`waitforgump 'gumpID' 'timeout'`  
Example: `waitforgump '1234' '5000'`  
Gumpid and timeout are both optional. Can use 'lastgump' to get the last gump id after using this command.


## `replygump`  

Reply to a gump with a specific button ID.  
`replygump 'buttonid' 'gumpid'`  
Example: `replygump '12' '1234'`  
Gumpid is optional


## `closegump`  

Close a gump.  
`closegump 'gumpid'`  
Example: `closegump '1234'`  
This command closes a gump by its ID. If no ID is provided, it defaults to the last gump ID.


## `clearjournal`  

Clear the journal.  
`clearjournal`  
Example: `clearjournal`


## `poplist`  

Remove an item from a list.  
`poplist 'name' 'value'`  
Example: `poplist 'myList' 'item1'`


## `targettilerel`  

Target a tile relative to the player's position.  
`targettilerel 'x' 'y' ['graphic']`  
Example: `targettilerel '1' '2'` or `targettilerel '1' '2' '0x1234'`


## `targetlandrel`  

Target the land relative to the player's position.  
`targettilerel 'x' 'y'`  
Example: `targetlandrel '1' '1'`


## `virtue`  

Invoke a specific virtue.  
`virtue 'honor|sacrifice|valor'`  
Example: `virtue 'honor'`


## `playmacro`  

Play a specified macro.  
`playmacro 'macroname'`  
Example: `playmacro 'myMacro'`


## `headmsg`  

Display a message above an entity's head.  
`headmsg 'serial' 'msg'`
Example: `headmsg 'self' 'Hello, world!'`


## `partymsg`  

Send a message to the party chat.  
`partymsg 'msg'`  
Example: `partymsg 'Hello, party!'`


## `guildmsg`  

Send a message to the guild chat.  
`guildmsg 'msg'`  
Example: `guildmsg 'Hello, guild!'`


## `allymsg`  

Send a message to the alliance chat.  
`allymsg 'msg'`  
Example: `allymsg 'Hello, allies!'`


## `whispermsg`  

Send a whisper message.  
`whispermsg 'msg'`  
Example: `whispermsg 'Hello, this is a secret!'`


## `yellmsg`  

Send a yell message.  
`yellmsg 'msg'`  
Example: `yellmsg 'Hello, everyone!'`


## `emotemsg`  

Send an emote message.  
`emotemsg 'msg'`  
Example: `emotemsg 'Hello, everyone!'`


## `waitforprompt`  

Wait for a prompt to appear within a specified timeout.  
`waitforprompt 'duration'`  
Example: `waitforprompt '5000'`  
Note: The duration is optional and defaults to 10,000 milliseconds (10 seconds) if not provided.


## `cancelprompt`  

Cancel the current prompt.  
`cancelprompt`  
Example: `cancelprompt`


## `promptresponse`  

Send a response to the current prompt.  
`promptresponse 'msg'`  
Example: `promptresponse 'This is a response'`


## `contextmenu`  

Send a context menu request and select an option for a specific entity.  
`contextmenu 'serial' 'option'`  
Example: `contextmenu '0x40001234' '1'`


## `clearignorelist`

Clear the ignore list  
`clearignorelist`  

## `ignoreobject`

Ignore an object  
`ignoreobject 'serial`  
Example: `ignoreobject 'self'`  
Ignorelists are reset when you stop the script. They are only available to that specific script, they are not global.


## `follow`

Follow a mobile  
`follow 'serial'`  
Example: `follow 'enemy'`


## `pathfind`

Pathfind to a position  
`pathfind 'x' 'y' 'z'`  
Example: `pathfind '1235' '2367' '45'`


## `cancelpathfind`

Cancel pathfinding or auto follow  
`cancelpathfind`





# Expressions

## `findtypelist`  

Find items of a specific type in a list and set an alias for the found item.  
`findtypelist 'listname' 'source' [color] [range]`  
Example: `findtypelist 'myList' '0x40001234'` or `findtypelist 'myList' '0x40001234' '0x1234' '5'`


## `ping`  
Get the current network ping.  
`ping`  
Example: `if ping > 300`


## `timerexists`

Check if a timer exists  
`timerexists` '123bandage'  
returns `true`/`false`


## `timerexpired`

Check if a timer has expired  
`timerexpired` '123bandage'  
Example: `if timerexpired '123bandage'`


## `findtype`

Find an object by type  
`findtype 'graphic' 'source' [color] [range]`  
Example: `if findtype '0x1bf1' 'any' 'anycolor' '2' > 0` <- Find items in containers or ground within 2 tiles  
If an object is found, you can use `found` alias to reference it.


## `findalias`

Find an object you set as an alias  
`findalias 'backpack'`  
Example: `if findalias 'myalias'`  
If found, you can use `found` to reference it.


## `skill`

Get the value or a skill  
`skill 'name' [true/false]`  
false is default, returns value, true returns base  
Example: `if skill 'mining' >= 75`


## `poisoned`

Get poisoned status of mobile  
`posioned [serial]`  
If serial is not included, it will check your personal poisoned status  
Example: `if poisoned` or `if poisoned 'pet'`


## `war`

Check if you are in warmode  
`war`  
Example: `if war`


## `contents`

Count the contents of a container(Top level only)  
`contents 'container'`  
Example: `if contents 'backpack' > 10`  
Note: This counts a stack as a single item, so 10 stacks of 30 would only return 10 items.  


## `findobject`

Try to find an object by serial  
`findobject 'serial' [container]`  
Example: `if findobject 'bank'` or `if findobject '0x4245364' 'backpack'`


## `distance`

Get the distance of an item or mobile  
`distance 'enemy'`  
Example: `if distance 'enemy' < 7`


## `injournal`

Check if text exists in journal  
`injournal 'search text, case sensitive`  
Example: `if injournal 'You see'`


## `inparty`

Check if a mobile is in your party  
`inparty 'serial'`  
Example: `if inparty 'self'`


## `property`

Search properties of an item for text  
`property 'serial' 'text'`  
Example: `if property 'found' 'Fencing +3'`


## `buffexists`

Check if a buff is active  
`buffexists 'name'`  
Example: `if buffexists 'weak'`  
Note: name can be a partial match  


## `findlayer`

Check if there is an item in a layer  
`findlayer 'layername`  
Example: `if findlayer 'onehanded'`  
Note: If an item is found, you can use 'found' to access it.  

| onehanded    | twohanded | shoes     |
| ------------ | ----------| ----------|
| pants        | shirt     | helmet    |
| gloves       | ring      | talisman  |
| necklace     | hair      | waist     |
| torso        | bracelet  | face      |
| beard        | tunic     | earrings  |
| arms         | cloak     | backpack  |
| robe         | skirt     | legs      |
| mount        | bank |  |


## `gumpexists`

Check if a gump is open  
`gumpexists 'gumpid'`  
Example: `if gumpexists '1915258020'`  


## `listcount`

Count the number of items in a list. Will return 0 if the list does not exist  
`listcount 'name'`  
Example: `if listcount 'pets'`


## `listexists`

Check if a list exists  
`listexists 'name'`  
Example: `if not listexists 'pets'`  


## `inlist`

Check if a list contains a value  
`inlist 'name' 'value'`  
Example: `if inlist 'pets' '0x4532345'`


## `nearesthostile`

Find the nearest hostile(Gray, criminal, murderer, enemy)  
`nearesthostile ['distance']`  
Example: `if nearesthostile`  
Note: if a hostile was found, you can use `'found'` to reference it  


## `counttype`

Get the count of a type of item in a container  
`counttype 'graphic' 'source' ['hue'] ['ground range']`  
Example: `while counttype '0x1bf3' 'backpack' > 3`  


## `dead`

Check if a mobile is dead  
`dead ['serial']`  
Example: `if dead`  
Not: If you don't include serial, it will default to yourself


## `itemamt`  

Get the amount of an item by its serial number.  
`itemamt 'serial'`  
Example: `itemamt '0x40001234'`


## `primaryabilityactive`

Check if primary ability is active  
`primaryabilityactive`  
Example: `if primaryabilityactive`  


## `secondaryabilityactive`

Check if secondary ability is active  
`secondaryabilityactive`  
Example: `if secondaryabilityactive`  

## `mounted`  

Check if the player is mounted.  
`mounted`  
Example: `if mounted`


## `paralyzed`  

Check if a player or mobile is paralyzed.  
`paralyzed 'serial'`  
Example: `if paralyzed`  
Note: `serial` is optional, if omitted it will check yourself.

## `pathfinding`

Check if you are actively pathfinding or following  
`pathfinding`  
Example: `if pathfinding`  



# Aliases

## Values

- `name` 
- `hits`, `maxhits`, `diffhits` <-- These support a 'serial' parameter
- `stam`, `maxstam`  
- `mana`, `maxmana`  
- `x`, `y`, `z`  
- `str`, `dex`, `int`
- `followers`, `maxfollowers`
- `gold`, `hidden`
- `weight`, `maxweight`
- `mounted`
- `true`, `false`
- `found` <- Available when using commands like findtype  
- `count` <- Available when using commands like findtype


## Objects

- `backpack`
- `bank`
- `lastobject`
- `lasttarget`
- `lefthand`
- `righthand`
- `mount`
- `self`
- `bandage`
- `any` <- Can be used in place of containers
- `anycolor` <- Match any hue





# Syntax

`if 'condition'`  
	`elseif 'condition'`  
	`else`  
`endif`


`while 'condition'`  
`endwhile`  


`for 'count'`  
`endfor`  


`foreach 'item' in 'list'`  
`endfor`  


`break`, `continue`  


`stop` <- Stop the script  
`replay` <- Start the script over   

`goto 'line'` and `return`  
These should work together, if you don't have an equal amount of goto and returns it will end up causing you issues with your script.  
Lines start at 0, if your editor starts at line 1 remember to -1.  
Here is an example to demonstrate using them nest:  
```
sysmsg 'This is a test of the goto functionality'

for 3
    goto '7'
endfor
stop

sysmsg 'Oo test is worky!'
goto '11'
return

headmsg 'self' 'This works too!'
return
```  
