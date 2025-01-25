Prefab editor Plugin
==========================================

The base mod includes a command-line tool, to make easier the cave prefab edition.

To invoke one of the following commands, you must be in the prefab editor, then press **F1** to open the debug console, then you can run one the available command using this syntax:

.. code-block::

    ce [command-name] [command-options]

You can find documentation about the avaible commands from the game, by running this command:

.. code-block::

    ce help

Here is the exhaustive list of the available sub-commands:

create
------
Creates a new empty prefab with the required tags.


marker
-------------------

.. code-block:: bash

    ce marker [direction]

Add a cave marker into the selection. (fails if selection is empty)

directions:

- `'n'`: Set marker direction to north.
- `'s'`: Set marker direction to south.
- `'e'`: Set marker direction to east.
- `'w'`: Set marker direction to west.


replaceall, ra
--------------

Replace all blocks in the selection box except air and water, with the selected item.

Select blocks to replace with the selection tool, then run:

.. code-block:: bash

    ce ra

replaceground, rg
-----------------
Replace all terrain blocks inside the selection box, which have air above them, with the selected item.


replaceterrain, rt
------------------
Replace all terrain blocks in the selection with the selected item.


rename [name]
-------------
Rename all files of the current prefab with the given new name.


room [options]
--------------
Create an empty room of the selected item in the selection box.

Options:

- `'empty'`: Generate a square empty room with wall width = 1 block.
- `'proc'`: Generate a procedural cave room in the selection box.


selectall, sa
-------------
Add all the prefab volume to the selection box.


setmarkerdirection, smd [direction]
----------------------------------
Once a marker is selected, set its direction.

Options:

- `'n'`: Set direction to north.
- `'s'`: Set direction to south.
- `'e'`: Set direction to east.
- `'w'`: Set direction to west.


setwater, sw [mode]
-------------------
Modify water blocks in the selection box.

Options:

- `'empty'`: Set all water blocks of the selection to air.
- `'fill'`: Set all air blocks of the selection to water.


stalactite [height]
-------------------
Creates a procedural stalactite of the specified height at the start position of the selection box.


tags [type]
-----------
Add the required tags to get a valid cave prefab. Type is optional and accepts the following keywords:

Options:

- `'entrance'`: The prefab is a cave entrance.
- `'underground'` or `'ug'`: The prefab is an underground prefab.

