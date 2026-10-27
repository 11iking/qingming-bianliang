$ErrorActionPreference = 'Stop'
$storyRoot = Split-Path -Parent $PSScriptRoot
$storyFiles = @(
    @{ Name = '01_阿安人物主线完整剧本.md'; Id = 'story'; Label = '完整剧本' },
    @{ Name = '02_史料研究与考据边界.md'; Id = 'history'; Label = '史料研究' },
    @{ Name = '03_交互设计与分支实施.md'; Id = 'interaction'; Label = '交互设计' }
)
$storyParts = foreach ($item in $storyFiles) {
    $storyText = Get-Content -LiteralPath (Join-Path $storyRoot $item.Name) -Raw -Encoding UTF8
    $storyHtml = (ConvertFrom-Markdown -InputObject $storyText).Html
    foreach ($target in $storyFiles) {
        $storyHtml = $storyHtml.Replace('href="' + $target.Name + '"', 'href="#' + $target.Id + '" data-tab="' + $target.Id + '"')
    }
    '<section class="document" id="' + $item.Id + '">' + $storyHtml + '</section>'
}
$storyHead = @'
<!doctype html>
<html lang="zh-CN">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>汴梁一日 阿安剧本与史料</title>
<style>
:root{--paper:#fffdf8;--ink:#292b28;--muted:#686c64;--line:#dcded4;--green:#304c40;--wash:#f0f2eb}
*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:100px}
body{margin:0;background:var(--paper);color:var(--ink);font-family:"Microsoft YaHei","Noto Sans SC",sans-serif;font-size:16px;line-height:1.9}
header{position:sticky;top:0;z-index:5;display:flex;align-items:center;gap:24px;flex-wrap:wrap;padding:14px 32px;background:#fffdf8f5;border-bottom:1px solid var(--line);backdrop-filter:blur(12px)}
.brand{font-weight:700;letter-spacing:.1em;color:var(--green)}nav{display:flex;gap:8px}button{font:inherit;font-size:14px;border:1px solid var(--line);background:transparent;color:var(--green);padding:6px 15px;border-radius:4px;cursor:pointer}button[aria-selected=true]{background:var(--green);color:white;border-color:var(--green)}button:hover{box-shadow:0 0 0 2px #304c4020}.print{margin-left:auto}
.layout{max-width:1360px;margin:auto;display:grid;grid-template-columns:230px minmax(0,1fr);gap:50px;padding:40px 32px 100px}
aside{position:sticky;top:100px;align-self:start;max-height:calc(100vh - 125px);overflow:auto;font-size:13px;padding-right:15px}aside strong{font-size:14px;color:var(--green)}aside a{display:block;text-decoration:none;color:var(--muted);padding:5px 0;line-height:1.6}aside a:hover{color:var(--green)}aside a.sub{padding-left:12px;font-size:12px}
main{min-width:0;max-width:940px}.document{display:none}.document.active{display:block}
h1,h2,h3,h4{color:#1c3027;line-height:1.5;scroll-margin-top:100px}h1{font-size:32px;margin:0 0 28px;font-family:"Noto Serif SC","SimSun",serif}h2{font-size:24px;margin:54px 0 22px;border-top:1px solid var(--line);padding-top:28px}h3{font-size:21px;margin:38px 0 16px}h4{font-size:18px;margin-top:28px}p{margin:15px 0}a{color:#315c4d;text-underline-offset:3px}li{margin:6px 0}strong{font-weight:650}blockquote{margin:20px 0;padding:8px 24px;border-left:3px solid #aab8aa;color:#445148;background:var(--wash)}blockquote p{white-space:normal}
table{width:100%;border-collapse:collapse;font-size:14px;line-height:1.7;margin:24px 0;table-layout:auto}th,td{border:1px solid var(--line);padding:10px 12px;text-align:left;vertical-align:top;overflow-wrap:anywhere}th{background:#e9eee5;color:#243f31;font-weight:650}tr:nth-child(even) td{background:#fafbf7}code{background:var(--wash);padding:2px 4px}footer{margin-top:64px;color:var(--muted);font-size:13px;border-top:1px solid var(--line);padding-top:20px}
@media(max-width:900px){.layout{grid-template-columns:1fr;gap:18px;padding:24px 18px 60px}aside{position:static;max-height:220px;border-bottom:1px solid var(--line);padding-bottom:16px}header{padding:12px 18px;gap:12px}.brand{width:100%}h1{font-size:27px}.print{margin-left:0}table{font-size:12px}th,td{padding:7px}}
@media print{header,aside{display:none!important}.layout{display:block;padding:0;max-width:none}main{max-width:none}.document{display:block!important;break-before:page}.document:first-child{break-before:auto}body{font-size:10pt;line-height:1.65;background:white}h1{font-size:22pt}h2{font-size:16pt}h3{font-size:13pt}h1,h2,h3,h4{break-after:avoid}tr,blockquote{break-inside:avoid}table{font-size:8pt}a{color:inherit;text-decoration:none}footer{display:none}}
</style>
</head>
<body>
<header><span class="brand">汴梁一日 · 阿安</span><nav aria-label="文稿">
<button type="button" data-tab="story" aria-selected="true">完整剧本</button>
<button type="button" data-tab="history" aria-selected="false">史料研究</button>
<button type="button" data-tab="interaction" aria-selected="false">交互设计</button>
</nav><button type="button" class="print" onclick="window.print()">打印全部文稿</button></header>
<div class="layout"><aside aria-label="章节目录"><strong>阅读目录</strong><div id="toc"></div></aside><main>
'@
$storyFoot = @'
<footer>汴梁一日 · 剧本研究稿 · 2026年10月21日<br>人物及对白为原创；历史记载、推演与交互转译分别标注。此稿尚未写入游戏。</footer>
</main></div>
<script>
const docs=[...document.querySelectorAll('.document')];
docs.forEach(section=>section.querySelectorAll('h2,h3').forEach((heading,index)=>heading.id=section.id+'-heading-'+index));
function showDoc(id,scroll=true){
 if(!docs.some(d=>d.id===id)) id='story';
 docs.forEach(d=>d.classList.toggle('active',d.id===id));
 document.querySelectorAll('nav button').forEach(b=>b.setAttribute('aria-selected',b.dataset.tab===id?'true':'false'));
 const toc=document.getElementById('toc');toc.replaceChildren();
 document.getElementById(id).querySelectorAll('h2,h3').forEach(h=>{
  const a=document.createElement('a');a.href='#'+h.id;a.textContent=h.textContent;
  if(h.tagName==='H3')a.className='sub';toc.appendChild(a);
 });
 if(scroll)window.scrollTo({top:0,behavior:'instant'});
}
document.querySelectorAll('[data-tab]').forEach(e=>e.addEventListener('click',event=>{
 event.preventDefault();showDoc(e.dataset.tab);history.replaceState(null,'','#'+e.dataset.tab);
}));
function followHash(){
 const hash=decodeURIComponent(location.hash.slice(1));
 const section=docs.find(d=>hash===d.id||hash.startsWith(d.id+'-heading-'));
 showDoc(section?section.id:'story',false);
 if(hash.includes('-heading-'))document.getElementById(hash)?.scrollIntoView();
}
window.addEventListener('hashchange',followHash);followHash();
</script>
</body></html>
'@
$storyOutput = Join-Path $storyRoot '汴梁一日_阿安剧本阅读版.html'
Set-Content -LiteralPath $storyOutput -Value ($storyHead + ($storyParts -join "`n") + $storyFoot) -Encoding utf8
$storyAll = ($storyFiles | ForEach-Object { Get-Content -LiteralPath (Join-Path $storyRoot $_.Name) -Raw -Encoding utf8 }) -join "`n"
$storyEvidence = [regex]::Matches((Get-Content -LiteralPath (Join-Path $storyRoot $storyFiles[1].Name) -Raw -Encoding utf8), '(?m)^### H\d{2} ').Count
$storyScenes = [regex]::Matches((Get-Content -LiteralPath (Join-Path $storyRoot $storyFiles[0].Name) -Raw -Encoding utf8), '(?m)^### S\d{2} ').Count
$storyInteractionText = Get-Content -LiteralPath (Join-Path $storyRoot $storyFiles[2].Name) -Raw -Encoding utf8
$storyActionTable = ($storyInteractionText -split '## 二 剧情结构与回流')[0]
$storyActions = [regex]::Matches($storyActionTable, '(?m)^\| [^|\r\n]+ \| (?:S\d|B\d)').Count
[pscustomobject]@{
    Output=$storyOutput
    ChineseCharacters=[regex]::Matches($storyAll,'\p{IsCJKUnifiedIdeographs}').Count
    Scenes=$storyScenes
    EvidenceEntries=$storyEvidence
    ActionMappings=$storyActions
    ExampleCash=80-48+10+16+36+8-4-3-4-6-2
    ExampleRemainingStock=24-3-4-12-2
} | ConvertTo-Json
