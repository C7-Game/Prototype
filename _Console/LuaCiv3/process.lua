
-- C# program will use this!
civ3_home = "/Users/jim/civ3"

-- variable init

-- vars for bic flag hunt
byte_chunks = {}
offset = 0
num_bytes = 32
idx = 1
num_bic_with_bldg = 0
num_bic_with_wchr = 0
num_bic_with_both = 0

-- vars for tile hunt
tile_off = {}
num_mountains = 0

-- called for each sav
function process_save (sav, path)
    tile_hunt(sav, path)
end


-- called for each bic
function process_bic (bic, path)
    -- flag_hunt(bic, path)
    path_hunt(bic, path)
end

function path_hunt (bic, path)
    -- print("Hi")
    local gameoff = bic.bic.sectionOffset("GAME", 1)
    io.write(bic.title .. "\n\n")
    io.write(bic.description .. "\n\n")
    io.write(bic.modRelPath .. "\n\n")
    -- io.write(bic.bic.getString(gameoff + 0xdc, 64) .. "\n")
    -- io.write(hex_dump(bic.bic.getBytes(gameoff + 0xdc, 656)))
    io.write("\n\n\n")
end


function flag_hunt(bic, path)
    io.write("\n", path, "\n")
    -- print(bic.bic.getString(0,4))
    local foo = bic.bic.getBytes(offset, num_bytes)
    -- pcall(io.write(hex_dump(foo)))
    io.write("\n")
    table.insert(byte_chunks, foo)
    if bic.bic.sectionExists(string.upper("BLDG")) then
        num_bic_with_bldg = num_bic_with_bldg + 1
        byte_chunks[idx]["bldg"] = true
    end
    if bic.bic.sectionExists(string.upper("WCHR")) then
        num_bic_with_wchr = num_bic_with_wchr + 1
        byte_chunks[idx]["wchr"] = true
    end
    if bldg and wchr then num_bic_with_both = num_bic_with_both + 1 end
    io.write(tostring(bic.hasCustomRules) .. " " .. tostring(bic.hasCustomMap) .. "\n")
    idx = idx + 1
end

-- called after all files processed
function show_results()
    -- save_results()
    -- finish writing to stdout before exit
    io.flush()
end


function save_results()
    print("\n\n", num_mountains)
    for k, v in pairs(tile_off) do
        print(k, "\n\n")
        for kk, vv in pairs(v) do
            print(base2(kk), string.format( "%02x ", kk ), vv)
        end
    end
end


function bic_results()
    print("\ndone", #byte_chunks)
    print(string.upper("BLDG"), num_bic_with_bldg)
    print(string.upper("wchr"), num_bic_with_wchr)
    print("both", num_bic_with_both)

    -- see if bytes change between files
    for i=1, num_bytes do
        local t = {}
        local num_values = 0
        for j=1, #byte_chunks do
            local k = byte_chunks[j][i]
            if t[k] then
                t[k] = t[k] + 1
            else
                t[k] = 1
                num_values = num_values + 1
            end
        end
        if num_values > 1 then
            -- print(i - 1, num_values)
            print("offset", i-1, "\n___")
            for kk, vv in pairs(t) do
                -- print(kk, vv)
                print(base2(kk), string.format( "%02x ", kk ), vv)
            end
            print("\n\n")
        end

    end
end


-- other stuff

-- adapted from https://gist.github.com/Elemecca/6361899
function hex_dump (str, offset)
    local len = #str
    local dump = ""
    local hex = ""
    local asc = ""
    offset = offset or 0
    
    for i = 1, len do
        if 1 == i % 8 then
            dump = dump .. hex .. asc .. "\n"
            hex = string.format( "%04x: ", i - 1 + offset )
            asc = ""
        end
        
        local ord =str[i]
        hex = hex .. string.format( "%02x ", ord )
        if ord >= 32 and ord <= 126 then
            asc = asc .. string.char( ord )
        else
            asc = asc .. "."
        end
    end

    
    return dump .. hex
            .. string.rep( "   ", 8 - len % 8 ) .. asc
end

function base2 (num)
    local out = ""
    for i=7, 0, -1 do
        if bit32.band(bit32.arshift(num, i), 1) == 1 then
                out = out .. "1"
        else
            out = out .. "0"
        end
    end
    return out
end

function tile_hunt (sav, path)
    -- print(sav.sav.getString(0,4))
local mount = false
for i=18,23 do
    tile_off[i] = {}
    for ii, tile in ipairs(sav.tile) do
        if tile.overlayTerrain == 6 then
            io.write("Mountain! ")
            if not mount then num_mountains = num_mountains + 1 end
            local o = sav.sav.readByte(tile.offset + i)
            if (not tile_off[i][o]) then 
                tile_off[i][o] = 1
            else
                tile_off[i][o] = tile_off[i][o] + 1
            end
        end
    end
    if num_mountains > 0 then mount = true end
end
end
