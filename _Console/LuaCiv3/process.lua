
-- C# program will use this!
civ3_home = "/Users/jim/civ3"


-- called for each sav
function process_save (sav)
    print(sav.sav.getString(0,4))
end

-- called for each bic
function process_bic (bic)
    print(bic.bic.getString(0,4))
end

-- called after all files processed
function show_results()
    print("done")
end
