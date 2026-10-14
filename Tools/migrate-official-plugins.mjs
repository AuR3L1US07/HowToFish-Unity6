import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {execFileSync} from 'node:child_process';
const toolProject=path.resolve(path.dirname(fileURLToPath(import.meta.url)),'..');
const projectArg=process.argv.find(x=>x.startsWith('--project='))?.slice(10);
const root=projectArg?path.resolve(projectArg):toolProject;
if(!fs.existsSync(path.join(root,'Assets'))||!fs.existsSync(path.join(root,'ProjectSettings/ProjectVersion.txt')))throw Error('Project must be an explicit existing Unity project directory.');
const sources=[path.join(root,'Packages'),path.join(root,'Library/PackageCache'),path.resolve(toolProject,'../tools/OfficialPackages'),'E:/Unity/6000.4.4f1/Editor/Data/Resources/PackageManager/BuiltInPackages'];
const targets=['com.unity.addressables','com.unity.localization','com.unity.animation.rigging','com.unity.ai.navigation','com.unity.visualeffectgraph','com.unity.transport'];
const walk=d=>fs.existsSync(d)?fs.readdirSync(d,{withFileTypes:true}).flatMap(e=>e.isDirectory()?walk(path.join(d,e.name)):[path.join(d,e.name)]):[];
const read=f=>fs.readFileSync(f,'utf8');
const guid=f=>read(f).match(/^guid: (\w+)/m)?.[1];
const catalog=new Map();
for(const dir of sources) if(fs.existsSync(dir)) for(const e of fs.readdirSync(dir,{withFileTypes:true})) {
 if(!e.isDirectory())continue;const d=path.join(dir,e.name),p=path.join(d,'package.json');if(!fs.existsSync(p))continue;
 const info=JSON.parse(read(p));if(!catalog.has(info.name))catalog.set(info.name,{dir:d,info});
}
const inventoryFile=fs.existsSync(path.join(root,'CompilationChecks/plugin-script-inventory.json'))?path.join(root,'CompilationChecks/plugin-script-inventory.json'):path.join(toolProject,'CompilationChecks/plugin-script-inventory.json');
const inventory=JSON.parse(read(inventoryFile)).entries;
const identities=JSON.parse(read(path.join(toolProject,'Tools/original-unity-assembly-identities.json')));
const atLeast=(actual,minimum)=>{const a=actual.split('.').map(Number),b=minimum.split('.').map(Number);for(let i=0;i<3;i++){if(a[i]!==b[i])return a[i]>b[i];}return true;};
const plans=[];
for(const target of targets){
 const packages=new Map(),missing=new Set(),constraints=[];
 function visit(n){if(n.startsWith('com.unity.modules.')||packages.has(n))return;const p=catalog.get(n);if(!p){missing.add(n);return;}packages.set(n,p);for(const dep of Object.keys(p.info.dependencies||{}))visit(dep);}
 visit(target);
 for(const [name,p]of packages)for(const [dep,minimum]of Object.entries(p.info.dependencies||{})){
  if(dep.startsWith('com.unity.modules.'))continue;const actual=packages.get(dep)?.info.version;
  if(actual&&!atLeast(actual,minimum))constraints.push(`${name} requires ${dep} >= ${minimum}; available ${actual}`);
 }
 const assemblies=new Set(),scripts=[];
 for(const [name,p] of packages)for(const f of walk(p.dir)){
  const segments=path.relative(p.dir,f).split(path.sep);
  if(segments.some(s=>s.startsWith('.')||s.includes('Tests')||s.startsWith('Samples~')))continue;
  if(f.endsWith('.asmdef'))assemblies.add(JSON.parse(read(f)).name);
  if(f.endsWith('.dll'))assemblies.add(path.basename(f,'.dll'));
  if(f.endsWith('.cs')&&fs.existsSync(f+'.meta')){const s=read(f),ns=s.match(/namespace\s+([\w.]+)/)?.[1]||'';scripts.push({file:f,guid:guid(f+'.meta'),type:(ns?ns+'.':'')+path.basename(f,'.cs')});}
 }
 const dlls=fs.readdirSync(path.join(root,'Assets/Plugins')).filter(f=>f.endsWith('.dll')&&assemblies.has(f.slice(0,-4)));
 for(const dll of dlls){if(!dll.startsWith('Unity.'))continue;const id=identities.find(x=>x.Assembly===dll.slice(0,-4));if(!id||id.Version!=='0.0.0.0'||id.PublicKeyToken)constraints.push(`Assembly identity needs explicit review: ${dll}`);}
 const oldGuids=new Set(dlls.map(d=>guid(path.join(root,'Assets/Plugins',d+'.meta'))));
 const mappings=new Map(),unmapped=[];
 for(const e of inventory.filter(e=>oldGuids.has(e.guid))){const matches=scripts.filter(s=>s.type===e.type);if(matches.length===1)mappings.set(e.guid+':'+e.fileID,matches[0]);else unmapped.push(e);}
 const edits=[],unresolved=[];
 for(const f of [...walk(path.join(root,'Assets')),...walk(path.join(root,'ProjectSettings'))]){
  if(!/\.(asset|prefab|unity|mat|controller|anim)$/.test(f))continue;const original=read(f);if(!original.startsWith('%YAML'))continue;
  const updated=original.replace(/\{fileID: (-?\d+), guid: ([a-f0-9]{32}), type: 3\}/g,(all,id,g)=>{if(!oldGuids.has(g))return all;const m=mappings.get(g+':'+id);if(!m){unresolved.push({file:f,id,guid:g});return all;}return `{fileID: 11500000, guid: ${m.guid}, type: 3}`;});
  if(original!==updated)edits.push({file:f,original,updated});
 }
 plans.push({target,packages,missing:[...missing],constraints,dlls,mappings:mappings.size,unmapped,unresolved,edits});
}
const report=plans.map(p=>({...p,packages:[...p.packages].map(([name,x])=>({name,version:x.info.version,source:x.dir})),edits:p.edits.map(e=>e.file),ready:!p.missing.length&&!p.unresolved.length&&!p.constraints.length}));
fs.mkdirSync(path.join(root,'Tools'),{recursive:true});
fs.writeFileSync(path.join(root,'Tools/official-plugin-migration-plan.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify(report.map(({target,packages,missing,dlls,mappings,unresolved,ready})=>({target,packages,missing,dlls,mappings,unresolved:unresolved.length,ready})),null,2));
if(!process.argv.includes('--apply'))process.exit(0);
const target=process.argv.find(x=>x.startsWith('--package='))?.slice(10),p=plans.find(x=>x.target===target);
if(!p)throw Error('Apply requires one explicit --package=<name>.');
if(p.missing.length||p.unresolved.length||p.constraints.length)throw Error('Missing source dependencies, assembly/version constraints or unresolved serialized references; no changes applied.');
const unityProcesses=JSON.parse(execFileSync('powershell',['-NoProfile','-Command',"$ErrorActionPreference = 'Stop'; ConvertTo-Json -Compress -InputObject @(Get-CimInstance Win32_Process -Filter \"Name = 'Unity.exe'\" | Select-Object ProcessId,CommandLine)"],{encoding:'utf8'}));
const running=unityProcesses.filter(p=>p.CommandLine?.toLowerCase().includes(root.toLowerCase()));
if(running.length)throw Error('Close Unity for selected project before migration.');
const backup=path.join(root,'ReferenceOnly','BeforeOfficialPluginMigration',target);
if(fs.existsSync(backup))throw Error('Backup exists; review previous migration first.');
fs.mkdirSync(backup,{recursive:true});
const preserve=f=>{const dest=path.join(backup,path.relative(root,f));fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(f,dest);};
const manifestFile=path.join(root,'Packages/manifest.json'),manifest=JSON.parse(read(manifestFile));preserve(manifestFile);
for(const [name,x]of p.packages){const destination=path.join(root,'Packages',name);if(!fs.existsSync(destination))fs.cpSync(x.dir,destination,{recursive:true});manifest.dependencies[name]=x.info.version;}
for(const d of p.dlls)for(const suffix of ['', '.meta']){const f=path.join(root,'Assets/Plugins',d+suffix);preserve(f);fs.unlinkSync(f);}
for(const e of p.edits){preserve(e.file);fs.writeFileSync(e.file,e.updated);}
fs.writeFileSync(manifestFile,JSON.stringify(manifest,null,2)+'\n');
fs.writeFileSync(path.join(backup,'plan.json'),JSON.stringify(report.find(x=>x.target===target),null,2));
