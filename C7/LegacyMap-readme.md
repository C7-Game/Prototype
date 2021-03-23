# LegacyMap Dev notes

This readme file is for dev planning and should eventually be deleted
or otherwise archived.

LegacyMap is intended to be a scene and script that simply draws a map using legacy/original media. Map selection should be separate from this scene. It is yet to be understood whether any map UI will need to be coded in this scene or if it can be handled by parent scenes. It should be easily swappable with a native media implementation ~~, and ideally new and legacy media could be mixed and matched on the same scene, but it is yet to be understood if this is feasible. (A manual conversion of legacy assets to new format may be needed instead.)~~ (Upon further thought, it will be assumed that this scene will only do maps consisting entirely of legacy media; trying to allow for yet-unknown future format mixing will be too cumbersome to develop.)

## Some thoughts on drawing the map using original/legacy art format

- The map display code should not constrain the code structure
- The map display should be swappable for new native media/code
- So, use interfaces for pulling data from tiles

---

- The legacy map drawing relies on ordered drawing of layers upon layers, and even of Northernmost tiles before more Southern tiles
- That constraint should not necessarily be inherited by a native map renderer
- Designing interfaces that allow the legacy map renderer to work but don't constrain native code will take some thought

---

- Example: Mountain tile
- In legacy civ3, a mountain is a grassland base with a mountain overlay
- Future native mods need not permanently codify a base/overlay terrain model
- But drawing the legacy map requires identifying the base and drawing it before identifing the overaly and drawing it
- Furthermore, a bonus grassland may exist under overlay terrain but the bonus shield tic is not drawn even in clean map mode when overlay terrain exists; this requires some coding logic

## Takeaways

- Will assume that LegacyMap will ONLY handle displaying maps consisting entirely of legacy media assets
- Furthermore, will assume map is structured as legacy tile data, meaning in particular inerface may include getters for both base and overlay terrain
- To avoid constraining future native code structure, perhaps a native tile can optionally have a legacy tile object property if the particular native mod is compatible with legacy display logic and media.
