"""Independent Python stdlib/Decimal shape oracle; does NOT execute delivered C#."""
import json
from decimal import Decimal

ROOT = {'schemaVersion', 'packageId', 'sourceManifestSha256', 'boardId', 'revision', 'isWholeBoard',
        'isExampleOnly', 'originX', 'originY', 'originZ', 'cellSize', 'seed', 'postSpacing',
        'networkCells', 'groupPlacements', 'allowedDecorCells'}
NETWORK = {'x', 'y', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ', 'family', 'connectionGroup'}
GROUP = {'placementId', 'groupKey', 'anchorX', 'anchorY', 'logicalLayer', 'heightKey', 'sortingOrder', 'worldZ'}
ALLOWED = {'x', 'y', 'logicalLayer', 'heightKey'}
MAX_FLOAT32 = Decimal(340282346638528859811704183484516925440)

def pairs(items):
    result = {}
    for key, value in items:
        if key in result: raise ValueError('duplicate property ' + key)
        result[key] = value
    return result

def object_fields(value, expected):
    if type(value) is not dict or set(value) != expected: raise ValueError('exact object fields required')

def integer(value, bits):
    if type(value) is not int or not -(2 ** (bits - 1)) <= value < 2 ** (bits - 1):
        raise ValueError('integer range/type')

def number(value):
    if type(value) not in [int, Decimal]: raise ValueError('number type')
    value = Decimal(value)
    if not value.is_finite() or abs(value) > MAX_FLOAT32: raise ValueError('finite float32 range')

def string(value):
    if type(value) is not str: raise ValueError('string type')

def validate_shape(text):
    if text.startswith('\ufeff'): text = text[1:]
    def no_constant(value): raise ValueError('non-JSON constant ' + value)
    value = json.loads(text, parse_int=int, parse_float=Decimal, object_pairs_hook=pairs, parse_constant=no_constant)
    object_fields(value, ROOT)
    for key in ['schemaVersion', 'seed', 'postSpacing']: integer(value[key], 32)
    integer(value['revision'], 64)
    for key in ['packageId', 'sourceManifestSha256', 'boardId']: string(value[key])
    for key in ['isWholeBoard', 'isExampleOnly']:
        if type(value[key]) is not bool: raise ValueError('boolean type')
    for key in ['originX', 'originY', 'originZ', 'cellSize']: number(value[key])
    for array, fields in [('networkCells', NETWORK), ('groupPlacements', GROUP), ('allowedDecorCells', ALLOWED)]:
        if type(value[array]) is not list: raise ValueError('array required')
        for row in value[array]:
            object_fields(row, fields)
            for key in fields:
                if key in ['family', 'connectionGroup', 'placementId', 'groupKey']: string(row[key])
                elif key == 'worldZ': number(row[key])
                else: integer(row[key], 32)
    return value

if __name__ == '__main__':
    from pathlib import Path
    import argparse
    p = argparse.ArgumentParser();p.add_argument('files', nargs='+');args = p.parse_args()
    for file in args.files:
        data = validate_shape(Path(file).read_text(encoding='utf-8'))
        print(json.dumps({'file':file, 'status':'PASS_INDEPENDENT_PYTHON_SHAPE_ORACLE',
                          'networkCells':len(data['networkCells']), 'groupPlacements':len(data['groupPlacements']),
                          'allowedCells':len(data['allowedDecorCells']), 'actualCSharpExecution':'NOT_RUN'}))
