local tech_boxes = {}

local ADVISORS = "Art/Advisors/"

local tech_boxes_texture = ADVISORS .. "techboxes.pcx"
local non_required_texture = ADVISORS .. "non_required.pcx"

-- different era boxes have slightly different sizes in pcx files
-- this is the max size they could be
-- seeing where the "Not required" sign renders in the original as well
-- this is probably how they did it originally
local table_small = { 1, 1, 106, 82 }
local table_medium = { 1, 84, 163, 82 }
local table_large = { 1, 167, 163, 106 }
local table_long = { 1, 274, 188, 82 }

-- Add the x, y, width, height variables of two different rectangles,
-- to allow us to avoid repeating the sizes of each tech box.
--
-- `offset` is assumed to be a crop_region with a width and height
--   of 0.
--
-- Returns a crop region table.
local function combine_offset_with_crop_region(crop_region, offset)
	local result = {}
	for i = 1, math.min(#crop_region, #offset) do
		result[i] = crop_region[i] + offset[i]
	end
	return result
end

local function create_entry(offset)
	local entry = {
		small = {
			path = tech_boxes_texture,
			crop_region = combine_offset_with_crop_region(table_small, offset),
		},
		medium = {
			path = tech_boxes_texture,
			crop_region = combine_offset_with_crop_region(table_medium, offset),
		},
		large = {
			path = tech_boxes_texture,
			crop_region = combine_offset_with_crop_region(table_large, offset),
		},
		long = {
			path = tech_boxes_texture,
			crop_region = combine_offset_with_crop_region(table_long, offset),
		},
	}
	return entry
end

tech_boxes = {
	known = {
		ancient = create_entry({0, 0, 0, 0}),
		middle = create_entry({0, 356, 0, 0}),
		industrial = create_entry({0, 712, 0, 0}),
		modern = create_entry({0, 1068, 0, 0}),
	},
	in_progress = {
		ancient = create_entry({189, 0, 0, 0}),
		middle = create_entry({189, 356, 0, 0}),
		industrial = create_entry({189, 712, 0, 0}),
		modern = create_entry({189, 1068, 0, 0}),
	},
	possible = {
		ancient = create_entry({378, 0, 0, 0}),
		middle = create_entry({378, 356, 0, 0}),
		industrial = create_entry({378, 712, 0, 0}),
		modern = create_entry({378, 1068, 0, 0}),
	},
	blocked = {
		ancient = create_entry({567, 0, 0, 0}),
		middle = create_entry({567, 356, 0, 0}),
		industrial = create_entry({567, 712, 0, 0}),
		modern = create_entry({567, 1068, 0, 0}),
	},
	non_required = non_required_texture
}

return tech_boxes
