import os
import sys
import re
import subprocess

ext = '.mcs'

def helped(msg: str):
 global help
 return f'{msg}\n{help}'

stn = str | None

meta_dirs: dict[str, tuple[stn, bool, str, str]] = {
# Mandatory
 'workpath': (None, True, 'path', 'required to set the working directory'),
 'result': (None, True, 'path', 'required to set the result prefix (commonly extension) of files'),

# Optional
 'include': (None, False, 'path', 'used to set the directory which is contains files to be included in every processing file'),
}

help=f'''
------ help message ------
{ext} file can contains directories (~directory) and comments (--- comment).
you can use "--- help" in {ext} to get that message 

Mandatory{
''.join([
f"""
  - {disc}
  ~{name} \"{inside}\"
""" for name, (_, mand, inside, disc) in meta_dirs.items() if mand])
}
Optional{
''.join([
f"""
  - {disc}
  ~{name} \"{inside}\"
""" for name, (_, mand, inside, disc) in meta_dirs.items() if not mand])
}'''

dirs  = {name: value for name, (value, *_) in meta_dirs.items()}

try:
 with open(ext) as f:
  lr = re.compile(r'\s*\~([a-zA-Z_][a-zA-Z0-9_]*)\s*\"([^\"]*)\"\s*')
  cr = re.compile(r'\s*---(.*)')
  for i, l in enumerate(f):
   l = l.strip()
   if l == '':
    continue
   i += 1
   m = lr.fullmatch(l)
   if m is not None:
    name = m.group(1)
    text = m.group(2)
    if name not in dirs:
     print(helped(f'Unexpected directory ~{name} at line {i}'))
     exit(1)
    dirs[name] = text
    continue
   m = cr.fullmatch(l)
   if m is None:
    print(helped(f'Unexpected line {i}: {l}'))
    exit(1)
   if m.group(1).strip() == 'help':
    print(help)
except IOError:
 print('''
''')
 exit(1)

for name, (_, mand, *_) in meta_dirs.items():
 if mand and dirs[name] is None:
  print(f"~{name} is not defined")
  exit(1)
  
def gmpath(name: str) -> str: return os.path.realpath(dirs[name].strip()) # type: ignore
def gmand(name: str) -> str: return dirs[name] # type: ignore
def gpath(name: str) -> stn:
 n = dirs[name]
 if n is None: return None
 return os.path.realpath(n.strip())

dir: str = gmpath('workpath')
res: str = gmand('result')
inc: stn = gpath('include')

if len(sys.argv) > 2:
 print('Program allows only -c | --clear argument to clear generated files')
 exit(1)

clear = False
if len(sys.argv) == 2:
 a = sys.argv[1].strip()
 if a == '-c' or a == '--clear':
  clear = True
 else:
  print('Program allows only -c(lear) argument to clear generated files')
  exit(1)

if clear:
 ext += res
 for root, _, files in os.walk(dir):
  for file in files:
   if file.endswith(ext):
    os.remove(os.path.join(root, file))
 exit(0)

incs: list[str] = []
if inc is not None:
 for root, _, files in os.walk(inc):
  for file in files:
   path = os.path.join(root, file)
   incs.append('--include')
   incs.append(f'{path}')

cmd = (
 'gpp',
 '-U', '\'', '', '(\\W', '\\W,\\W', '\\W)', '(\\W', '\\W)', '~', '',
 '-M', '~', '\\n', '(\\W', '\\W:\\W', '\\W)', '(\\W', '\\W)',
 '+c', '---', '\\n',
 '+sqqq', '~(', ')~', '',
 *incs
)

s: list[tuple[tuple[str, ...], str, str]] = []

for root, _, files in os.walk(dir):
 for file in files:
  if file.endswith(ext):
   path = os.path.join(root, file)
   s.append(((*cmd, '-o', f'{path}{res}', f'{path}'), os.path.relpath(path), root))
 
err_fix_re = re.compile(r'([^\:]*)(\:\d+\:.*)')

print('--- processing...')
cwd = os.getcwd()
for e, f, ff in s:
 print(f'--- file: {f}')
 result = subprocess.run(e, cwd=cwd, capture_output=True, text=True)
 # all GPP messages is "errors"
 lines = result.stderr.split('\n')
 for l in lines:
  # no empty lines
  if l.strip() == '':
   continue
  # annoying warning
  #if l.endswith('warning: the defined(...) macro is already defined'):
  # continue
  
  def err_fix(m: re.Match[str]):
   path = m.group(1)
   other = m.group(2)
   os.chdir(ff)
   path = os.path.realpath(path)
   os.chdir(cwd)
   path = os.path.relpath(path)
   return path+other
  
  print(err_fix_re.sub(err_fix, l))
 
 print(result.stdout, end='')

print('--- done.')
