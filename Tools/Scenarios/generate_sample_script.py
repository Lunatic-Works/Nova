import sys
from random import randrange

sys.stdout.reconfigure(encoding='utf-8')

label = 'test_upgrade'
n = 100
d = 0
if len(sys.argv) > 1:
    d = int(sys.argv[1])
print('@<|')
print(f'label \'{label}\'')
print('is_debug()')
print('|>')
print('<|')
print('set_box()')
print('|>')


def diag(x):
    return f'对话{x:3}'


a = list(map(diag, range(100)))

for _ in range(d):
    if len(a) > 0 and randrange(2) == 0:
        a.pop(randrange(len(a)))
    else:
        a.insert(randrange(len(a) + 1), diag(n))
        n += 1

print('\n\n'.join(a))
print('@<| is_end() |>')
