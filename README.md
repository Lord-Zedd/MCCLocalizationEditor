# MCC Localization Editor
Editor/Viewer for localization files in the Master Chief Collection

## Features
- Open and save the localization bin files within the MCC's `\data\ui\localization\` directory.
- Add, edit, or remove strings from MCC's localization.
- Export strings to XML for finer editing, then re-import back.

## Downloading
Precompiled builds are available through GitHub https://github.com/Lord-Zedd/MCCLocalizationEditor/releases

## About Localization Files
(This information is also available via a dialog in the application)

Localization files aren't the most modification-friendly files. Key strings are stored as CRC32MPEG hashes instead so they aren't easily searchable without the original key.
These keys are referenced by the game by prefixing a $ to the string. If there is no match the key is displayed instead. (The $ is not hashed)

The bin files themselves store an all-caps hash of the filename, so when saving a bin with this tool, you should use the intended name and avoid renaming past that point. A bad hash will hang the game.

In recent MCC builds game-specific bin files were introduced that override the localization stored in the cache files. String IDs become the key string and are hashed all the same.
If a string is missing from a bin file, then the engine seems to fall back to the cache file's string.

Always back up your files as any changes you make will flag the anticheat.

There is also limited HTML-like markup support available in these files; most work as expected but image source should use `img://IMAGE_NAME` where `IMAGE_NAME` refers to the name of an image within a texture pack.

However, not all text fields will actually resolve your markup, instead displaying the code as plain text.