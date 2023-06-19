#!python3
import json
from jsonschema import validate

# json schema would be useful for identifying type errors in save files
# ie. storing unit hit points remaining as "3" instead of 3.
schema = {
    'type': 'object',
    'properties': {
        'version': {'type': 'string'}
    }
}

with open('../C7/Text/c7-static-map-save.json') as f:
    j = json.load(f)
    print(j['version'])

validate(j, schema=schema)
