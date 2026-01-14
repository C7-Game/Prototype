-- Recursively walk the configuration table, copying the table and
-- applying 'path_transformer' to texture paths
local function transform_paths(value, path_transformer)
  if type(value) == "string" then
    return path_transformer(value)
  end

  if type(value) == "table" then
    local result = {}
    for k, v in pairs(value) do
      result[k] = transform_paths(v, path_transformer)
    end
    return result
  end

  return value
end

-- Helper: Strip file extension from path
local function strip_extension(path)
  return path:match "^(.-)%.[^%.]+$" or path
end

return { transform_paths = transform_paths, strip_extension = strip_extension }
