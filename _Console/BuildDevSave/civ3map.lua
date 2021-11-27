-- print "Hello from Lua"

function process_sav (mapReader)
    return
end

function TEMP_DISABLED_process_sav (mapReader)
    if not mapReader.wrld then
        return
    end
    for i, tile in ipairs(mapReader.tile) do
        print(i)
        print(tile.baseTerrain)
        print(tile.overlayTerrain)
        print(tile.baseTerrainFileID)
        print(tile.baseTerrainImageID)
        print("")
    end
end