#!python3
import json
import sys

def load_save(path: str) -> dict:
    with open(path, 'r') as f:
        return json.load(f)

def get_units(save, player_id):
    return [unit for unit in save['units'] if unit['owner'] == player_id]

def unit_prototype_breakdown(units: list):
    breakdown = {}
    for unit in units:
        proto = unit['prototype']
        breakdown[proto] = breakdown.get(proto, 0) + 1
    return breakdown

def summarize_player_units(save, player_id):
    print(player_id)
    units = get_units(save, player_id)
    if len(units) == 0:
        print('\tno units')
    else:
        print(f'\t{len(units)} units:')
        for proto, count in unit_prototype_breakdown(units).items():
            print(f'\t\t{proto}: {count}')

def get_cities(save, player_id):
    return [city for city in save['cities'] if city['owner'] == player_id]

def summarize_player_cities(save, player_id):
    print(player_id)
    cities = get_cities(save, player_id)
    print(f'\t{len(cities)} cities')

c7save = load_save(sys.argv[1])

players = c7save['players']
for player in players:
    pid = player['id']
    # summarize_player_units(c7save, pid)
    summarize_player_cities(c7save, pid)
