## Caveprefab editor commands:

- marker: Add a cave marker into the selection.
- replaceterrain, rt: Replace all terrain blocks in the selection with the selected item.
- selectall, sa: add all the prefab volume to the selection box.
- room: create an empty room in the selection box, with selected item in belt.
- setwater, sw [mode]:
    * 'empty': set all water blocks of the selection to air.
    * 'fill': set all air blocks of the selection to water.

## How to create a cave marker ?

* Select empty blocks with the tool selection
* open debug console (F1)
* write `ce marker` then press enter
* save the prefab


## Supported EditorGroups:

* `cave` (mandatory)

## Supported Tags:

* `devonly` (mandatory)
* `cave` (mandatory)
* `cavefiller`
* `entrance`
* `flooded`
* `bunker`

## Cave prefab editor commands:

```
ce [subcommand] [parameters]
```


