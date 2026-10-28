import os,json,sys
root=os.path.abspath(sys.argv[sys.argv.index('--')+1]);folder=os.path.join(root,'SourceAssets','Characters','Remaster012','MakeHuman')
v=[];groups={};group=''
for line in open(os.path.join(folder,'base.obj')):
    bits=line.split()
    if not bits:continue
    if bits[0]=='v':v.append(tuple(map(float,bits[1:4])))
    elif bits[0]=='g':group=bits[1];groups[group]=[]
    elif bits[0]=='f':groups[group].append([int(b.split('/')[0])-1 for b in bits[1:]])
for name,amount in [('caucasian-male-old.target',.65),('caucasian-male-young.target',.35)]:
    for line in open(os.path.join(folder,name)):
        bits=line.split()
        if not bits or bits[0].startswith('#'):continue
        i=int(bits[0]);v[i]=tuple(a+float(b)*amount for a,b in zip(v[i],bits[1:4]))
result={}
for group in ['body','joint-head','joint-neck','joint-l-eye','joint-r-eye','joint-upperneck','joint-l-upperlid','joint-r-upperlid']:
    ids=set(i for f in groups.get(group,[]) for i in f)
    if ids:result[group]=dict(count=len(ids),min=[min(v[i][j] for i in ids) for j in range(3)],max=[max(v[i][j] for i in ids) for j in range(3)],center=[sum(v[i][j] for i in ids)/len(ids) for j in range(3)])
print(json.dumps(result,indent=2))
