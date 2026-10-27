"""Create/recover this private GitHub repository using the OS credential manager.
Never prints, writes to disk, or embeds the credential in a remote URL.
Run from the prepared Git worktree after the user has authenticated.
"""
import json
import subprocess
import urllib.error
import urllib.request
from pathlib import Path

OWNER='11iking'
NAME='qingming-bianliang'
proc=subprocess.run(['git','credential-manager','get'],input=f'protocol=https\nhost=github.com\nusername={OWNER}\n\n',capture_output=True,text=True,check=True)
credential=dict(line.split('=',1) for line in proc.stdout.splitlines() if '=' in line)
token=credential.get('password')
if not token: raise SystemExit('AUTH_REQUIRED: no usable credential')
def api(path,method='GET',body=None):
    req=urllib.request.Request('https://api.github.com'+path,method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','Content-Type':'application/json','User-Agent':'Qingming-project-transfer','X-GitHub-Api-Version':'2022-11-28'})
    with urllib.request.urlopen(req,timeout=30) as response:return json.load(response)
try:
    account=api('/user')
    if account['login'].casefold()!=OWNER.casefold():raise SystemExit('Authenticated account differs from selected owner; no changes made.')
    try:
        repo=api(f'/repos/{OWNER}/{NAME}')
        if not repo['private']:raise SystemExit('Existing repository is public; no changes made.')
        if repo['size']!=0:
            local=subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip()
            try:remote=api(f'/repos/{OWNER}/{NAME}/git/ref/heads/main')['object']['sha']
            except urllib.error.HTTPError:raise SystemExit('Existing repository contains other work; no changes made.')
            if remote!=local:raise SystemExit('Existing repository has different work; no changes made.')
    except urllib.error.HTTPError as error:
        if error.code!=404:raise
        repo=api('/user/repos','POST',{'name':NAME,'description':'清明上河图 · 汴梁一日：Unity/Godot 市井生活 Demo、人物立绘与 Mac/Vision Pro 开发交接','private':True,'auto_init':False})
    print(json.dumps({'login':account['login'],'url':repo['html_url'],'private':repo['private']},ensure_ascii=False))
    expected=f'https://github.com/{OWNER}/{NAME}.git'
    remotes=subprocess.check_output(['git','remote'],text=True).splitlines()
    if 'origin' not in remotes:subprocess.run(['git','remote','add','origin',expected],check=True)
    elif subprocess.check_output(['git','remote','get-url','origin'],text=True).strip()!=expected:raise SystemExit('Unexpected origin; left unchanged.')
except urllib.error.HTTPError as error:
    raise SystemExit(f'GitHub API returned HTTP {error.code}; no credential details printed.')
