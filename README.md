# HOGameBuilder
'Hidden Object' game and scene generator

Gameplay features:
- high variation of items and high replayability through scene randomization.
  Typical scene can have many items that can have many placeholders.
  When game starts it pick some random subset of items and pick random placeholders for that items.
  Items and placeholders are picked with the aim of evenly distribution across the scene and minimize overlapping.
- 'skills' - time bonus and item destroyer. Plus flexible mechanism for add new ones.
- 'silhouette' and 'text' search modes for items.
- touch (android) devices support.

Scene generator features:
- build as Editor plugin (GUI 'magic button'-style) with aim to minimizing scene build time.
  Only few mouse clicks are required from importing PSD-image to play imported scene.
- generator imports PSD image file, load all layers into scene and automatically group them by names into hierarchy like this:
  - Book (SceneItem)
    - Placeholder 1
      - shadow layer (shadow layers are hided when object is picked up)
      - patch layer
      ...
    - Placeholder 2
      ...
    ...
  - Candle (another SceneItem)
    - Placeholder 1
      ...
    ...
  ...

  Grouping is done using pattern matching and suffix checks of layer name.
  For example, layer name 'book_01_sh' is placed into 'book' SceneItem into first placeholder ('_01') as shadow layer ('_sh', '_shadow' or '_light').
  And 'rusty_screw_05_patch' is placed into 'rusty_screw' SceneItem into five placeholder ('_05') as patch layer (suffix '_patch' or not-shadow-suffix).  

  Then generator convert the scene into intermediate format (serialized JSON) and save to file.
  This is needed for ability to create scenes and redistribute them as DLC (independent from the game).

Features:
- optimization for large scenes (more than 1000 scene items that can have many placeholders)
- unit tests
