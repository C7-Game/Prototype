
-- C# program will use this!
civ3_home = "/Users/jim/civ3"

-- variable init

byte_chunks = {}
offset = 0
num_bytes = 32
idx = 1

num_bic_with_bldg = 0
num_bic_with_wchr = 0
num_bic_with_both = 0

-- called for each sav
function process_save (sav, path)
    print(sav.sav.getString(0,4))
end

-- called for each bic
function process_bic (bic, path)
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
function hex_dump (str)
    local len = #str
    local dump = ""
    local hex = ""
    local asc = ""
    
    for i = 1, len do
        if 1 == i % 8 then
            dump = dump .. hex .. asc .. "\n"
            hex = string.format( "%04x: ", i - 1 )
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