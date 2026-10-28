"""Author editable meshes/rigs in Blender and export the actual runtime mesh.

Run Blender --background --python this_file -- <repository root>.
Coordinates in authoring helpers are Unity metres (right/up/forward); Blender
stores right/back/up. No generated reference photograph is used as geometry.
"""
import bpy, math, json, os, sys, random
from mathutils import Vector

ROOT=os.path.abspath(sys.argv[sys.argv.index('--')+1]); random.seed(12012)
SOURCE=os.path.join(ROOT,'SourceAssets','Characters','Remaster012')
OUT=os.path.join(ROOT,'Assets','Resources','Characters')
os.makedirs(SOURCE,exist_ok=True);os.makedirs(OUT,exist_ok=True)
V=[];F=[];C=[];W=[];UV=[];REF=[];bones=[]
SKIN=(.64,.445,.345,0); NAVY=(.026,.039,.063,0); SHIRT=(.26,.43,.61,0)
HAIR=(.075,.066,.059,0); SHOE=(.025,.023,.021,0); STEEL=(.32,.36,.39,.85)
def addbone(name,parent,p):
    bones.append(dict(name=name,parent=parent,position=p));return len(bones)-1
def vertex(p,c,w,uv=(0,0),reference=0):
    V.append(tuple(p));C.append(c);W.append(w);UV.append(uv);REF.append(reference);return len(V)-1
def face(*v):F.append(v)
def reset():V.clear();F.clear();C.clear();W.clear();UV.clear();REF.clear();bones.clear()
def mix(a,b,t):return tuple(x+(y-x)*t for x,y in zip(a,b))
def smooth(t):return max(0,min(1,t))**2*(3-2*max(0,min(1,t)))
def tube(points,radii,color,weight,sides=16,flatten=1,caps=True):
    start=len(V)
    for r,p in enumerate(points):
        direction=Vector(points[min(r+1,len(points)-1)])-Vector(points[max(0,r-1)])
        direction.normalize();ref=Vector((0,0,1)) if abs(direction.z)<.9 else Vector((1,0,0))
        x=direction.cross(ref).normalized();z=direction.cross(x).normalized()
        for i in range(sides+1):
            a=i/sides*math.tau;offset=x*math.cos(a)*radii[r]+z*math.sin(a)*radii[r]*flatten
            c=color(r,i,p) if callable(color) else color;w=weight(r) if callable(weight) else weight
            vertex(Vector(p)+offset,c,w,(i/sides,r/(len(points)-1)))
    for r in range(len(points)-1):
        for i in range(sides):
            a=start+r*(sides+1)+i;face(a,a+1,a+sides+2,a+sides+1)
    if caps:
        face(*[start+i for i in reversed(range(sides))]);top=start+(len(points)-1)*(sides+1);face(*[top+i for i in range(sides)])
def loft(rows,color,weight,sides=48,opening=None,fold=0):
    start=len(V)
    for r,(y,rx,rz,zc) in enumerate(rows):
        gap=opening(y) if opening else 0
        for i in range(sides+1):
            a=gap+(math.tau-2*gap)*i/sides
            flutter=fold*math.sin(a*9+y*31)*math.sin(math.pi*r/(len(rows)-1))
            p=(math.sin(a)*(rx+flutter),y,zc+math.cos(a)*(rz+flutter*.5))
            vertex(p,color(r,i,p) if callable(color) else color,weight(y) if callable(weight) else weight,(i/sides,r/(len(rows)-1)))
    for r in range(len(rows)-1):
        for i in range(sides):
            a=start+r*(sides+1)+i;face(a,a+1,a+sides+2,a+sides+1)
def patch(points,color,weight,thickness=.003):
    # Shaped lapel/pocket panel; rear/edge topology gives actual fabric thickness.
    start=len(V)
    for offset in (0,-thickness):
        for i,p in enumerate(points):vertex((p[0],p[1],p[2]+offset),color,weight,(i%2,i//2))
    n=len(points);face(*[start+i for i in range(n)]);face(*[start+n+i for i in reversed(range(n))])
    for i in range(n):j=(i+1)%n;face(start+i,start+j,start+j+n,start+i+n)
def oval(center,radius,color,weight,rings=10,sides=20):
    # Accessories and eyes only. The final face uses the sculpted head surface.
    start=len(V)
    for r in range(rings+1):
        b=math.pi*r/rings
        for i in range(sides+1):
            a=i/sides*math.tau;p=(center[0]+radius[0]*math.sin(b)*math.cos(a),center[1]+radius[1]*math.cos(b),center[2]+radius[2]*math.sin(b)*math.sin(a))
            vertex(p,color,weight,(i/sides,r/rings))
    for r in range(rings):
        for i in range(sides):
            k=start+r*(sides+1)+i;face(k,k+1,k+sides+2,k+sides+1)
def gauss(x,y,cx,cy,sx,sy):return math.exp(-((x-cx)/sx)**2-((y-cy)/sy)**2)
def skin_color(x,y,z):
    t=.006*math.sin(x*710+y*489)*math.sin(y*811+z*133)
    stubble=max(0,1-abs(y-1.673)/.061)*smooth((z-.012)/.07)*(.36+.025*random.random())
    cheek=gauss(abs(x),y,.061,1.704,.027,.025)*.035
    return (SKIN[0]+t-stubble*.35+cheek,SKIN[1]+t-stubble*.23,SKIN[2]+t-stubble*.15,0)
def sculpt(x,y,z,front):
    # Broad brow, deep-set sockets, bridge/tip/alae, cheekbones, philtrum,
    # nasolabial folds and chin are deformations of one continuous head mesh.
    nose=.038*gauss(x,y,0,1.735,.014,.039)+.019*gauss(x,y,.001,1.711,.020,.012)
    nose+=.010*gauss(abs(x),y,.016,1.707,.010,.009)
    socket=-.014*gauss(abs(x),y,.038,1.757,.027,.013)
    brow=.013*gauss(abs(x),y,.035,1.779,.036,.011)
    cheek=.009*gauss(abs(x),y,.066,1.720,.027,.026)
    mouth=-.004*gauss(x,y,0,1.670,.033,.007)
    folds=-.003*gauss(abs(x),y,.031+(1.709-y)*.2,1.693,.006,.020)
    chin=.009*gauss(x,y,0,1.632,.034,.018)
    wrinkles=-.00065*(.5+.5*math.cos(y*780))*gauss(x,y,0,1.811,.072,.025)
    return z+(nose+socket+brow+cheek+mouth+folds+chin+wrinkles)*max(0,front)**4
def make_head(head):
    profile=[(1.601,.033,.035,-.008),(1.612,.049,.049,-.004),(1.630,.064,.060,-.004),(1.650,.074,.069,-.007),(1.673,.082,.075,-.010),(1.697,.090,.079,-.012),(1.720,.096,.082,-.012),(1.745,.095,.085,-.010),(1.770,.093,.085,-.009),(1.795,.092,.085,-.010),(1.820,.090,.083,-.013),(1.844,.083,.078,-.017),(1.864,.069,.065,-.019),(1.881,.043,.040,-.020),(1.888,.002,.002,-.020)]
    def section(y):
        for a,b in zip(profile,profile[1:]):
            if a[0]<=y<=b[0]:return mix(a,b,(y-a[0])/(b[0]-a[0]))
        return profile[-1]
    def front_at(x,y):
        _,rx,rz,zc=section(y);f=math.sqrt(max(0,1-(x/rx)**2));return sculpt(x,y,zc+f*rz,f)
    start=len(V);nr=72;ns=96
    for r in range(nr+1):
        y=profile[0][0]+r/nr*(profile[-1][0]-profile[0][0]);_,rx,rz,zc=section(y)
        for i in range(ns+1):
            a=i/ns*math.tau;x=math.sin(a)*rx;f=math.cos(a);z=zc+f*rz
            x+=.0007*math.sin(y*31)*max(0,f);z=sculpt(x,y,z,f)
            vertex((x,y,z),skin_color(x,y,z),{head:1},(i/ns,r/nr))
    for r in range(nr):
        for i in range(ns):
            k=start+r*(ns+1)+i;face(k,k+1,k+ns+2,k+ns+1)
    # Eye surfaces seated in sculpted sockets with eyelid and tear-duct rims.
    for side in (-1,1):
        x=side*.038
        eyez=front_at(x,1.755)+.003
        start=len(V)
        for ring in range(9):
            r=ring/8
            for i in range(33):
                a=i/32*math.tau;xx=x+math.cos(a)*.024*r;yy=1.755+math.sin(a)*.0055*r
                zz=eyez*(1-smooth(r))+(front_at(xx,yy)+.0007)*smooth(r)
                vertex((xx,yy,zz),(.48,.49,.45,0),{head:1},(i/32,r))
        for ring in range(8):
            for i in range(32):k=start+ring*33+i;face(k,k+1,k+34,k+33)
        oval((x,1.755,eyez+.0007),(.007,.0048,.0005),(.10,.235,.30,.05),{head:1},10,28)
        oval((x,1.755,eyez+.0012),(.0028,.0037,.0003),(.009,.012,.014,.2),{head:1},8,20)
        for upper in (False,True):
            pts=[]
            for i in range(25):
                a=math.pi*i/24;xx=x+math.cos(a)*.025;yy=1.755+math.sin(a)*(.006 if upper else -.0048)
                pts.append((xx,yy,front_at(xx,yy)+.0012))
            tube(pts,[.0018 if upper else .0012]*len(pts),(.50,.335,.265,0),{head:1},8)
        for i in range(22):
            xx=x+(-.023+i*.0021);yy=1.779+.003*math.sin(i/21*math.pi)-side*(i/21-.5)*.002
            zz=front_at(xx,yy)+.001;tube([(xx,yy,zz),(xx+side*.001,yy+.003,zz+.001)],[.0012,.00025],HAIR,{head:1},5)
        # Contoured ear rim, antihelix and concha, rather than a bead on the skull.
        pts=[(side*(.092+.010*math.sin(math.pi*i/23)),1.725+i/23*.054,-.012+.010*math.sin(i/23*math.tau)) for i in range(24)]
        tube(pts,[.004]*24,SKIN,{head:1},10)
        oval((side*.097,1.749,-.008),(.007,.019,.009),(.43,.28,.22,0),{head:1},10,16)
        tube([(side*.101,1.733,-.002),(side*.105,1.749,.001),(side*.100,1.765,-.004)],[.0025,.003,.002],SKIN,{head:1},8)
        oval((side*.013,1.705,.111),(.006,.002,.002),(.21,.13,.10,0),{head:1},6,12)
    for upper in (True,False):
        pts=[]
        for i in range(33):
            x=(i/32-.5)*.057;arch=math.sin(i/32*math.pi)
            y=1.668+(.0025 if upper else -.0025)*arch-.001*(x/.03)**2
            pts.append((x,y,front_at(x,y)+.0007))
        tube(pts,[.0014]*len(pts),(.44,.285,.255,0) if upper else (.52,.34,.30,0),{head:1},8,flatten=.65)
    # Close-cropped scalp follows the authored head profile, plus tapered strands.
    capstart=len(V);caprings=24;capsides=80
    for r in range(caprings+1):
        for i in range(capsides+1):
            a=i/capsides*math.tau;bottom=1.765+max(0,math.cos(a))*.065;yy=bottom+(1.887-bottom)*r/caprings;_,rx,rz,zc=section(yy)
            vertex((math.sin(a)*(rx+.001),yy,zc+math.cos(a)*(rz+.001)),HAIR,{head:1},(i/capsides,r/caprings))
    for r in range(caprings):
        for i in range(capsides):k=capstart+r*(capsides+1)+i;face(k,k+1,k+capsides+2,k+capsides+1)
    for strand in range(1200):
        a=random.uniform(0,math.tau);y=random.uniform(1.767,1.885);_,rx,rz,zc=section(y)
        f=math.cos(a)
        if f>.15 and y<1.824+.008*math.cos(a*2):continue
        x=math.sin(a)*rx;z=zc+math.cos(a)*rz
        outward=Vector((math.sin(a)*.5,.7,math.cos(a)*.5)).normalized();p=Vector((x,y,z));lean=Vector((random.uniform(-.010,.010),.005,random.uniform(-.008,.008)))
        pts=[tuple(p+outward*(.001+t*.004)+lean*math.sin(t*math.pi/2)) for t in (0,.3,.65,1)]
        shade=random.choices((.035,.055,.085,.18,.27),(3,3,3,1,1))[0];tube(pts,[.0014,.0012,.0008,.0001],(shade,shade*.93,shade*.86,0),{head:1},5)
def body_weight(y,hips,chest):
    t=smooth((y-1.04)/.30);return {hips:1-t,chest:t}
def make_hand(side,hand):
    x=bones[hand]['position'][0];y=bones[hand]['position'][1]
    loft([(y-.09,.034,.020,.017),(y-.045,.038,.023,.013),(y,.027,.022,.010)],SKIN,{hand:1},24)
    # Move the just-built palm from the centre line to its wrist.
    count=3*25
    for i in range(len(V)-count,len(V)):p=V[i];V[i]=(p[0]+x,p[1],p[2])
    for finger in range(5):
        thumb=finger==4;offset=(finger-1.5)*.019 if not thumb else -side*.039
        top=(x+offset,y-(.058 if thumb else .082),.018)
        length=(.070,.083,.078,.061)[finger] if not thumb else .060
        a=addbone(('Left' if side<0 else 'Right')+' finger '+str(finger)+' proximal',hand,top)
        middle=(top[0]+(side*.008 if thumb else 0),top[1]-length*.52,top[2]+.014)
        b=addbone(('Left' if side<0 else 'Right')+' finger '+str(finger)+' distal',a,middle)
        pts=[top,mix(top,middle,.5),middle,(middle[0],top[1]-length*.80,.039),(middle[0],top[1]-length,.044)]
        radius=.010 if thumb else .0085
        tube(pts,[radius,radius*.99,radius*.95,radius*.77,.002],SKIN,lambda r:{a:1} if r<2 else {b:1},12)
        p=pts[-2];patch([(p[0]-.004,p[1]-.008,p[2]-.006),(p[0]+.004,p[1]-.008,p[2]-.006),(p[0]+.004,p[1]+.002,p[2]-.006),(p[0]-.004,p[1]+.002,p[2]-.006)],(.61,.45,.38,0),{b:1},.0004)
def make_anatomical_head(head):
    folder=os.path.join(SOURCE,'MakeHuman');points=[];tex=[];groups={};group=''
    for line in open(os.path.join(folder,'base.obj')):
        b=line.split()
        if not b:continue
        if b[0]=='v':points.append(Vector(tuple(map(float,b[1:4]))))
        elif b[0]=='vt':tex.append(tuple(map(float,b[1:3])))
        elif b[0]=='g':group=b[1];groups[group]=[]
        elif b[0]=='f':groups[group].append([tuple(int(x)-1 for x in entry.split('/')[:2]) for entry in b[1:]])
    for name,amount in [('caucasian-male-old.target',.70),('caucasian-male-young.target',.30),('nose-hump-incr.target',.24),('nose-scale-depth-incr.target',.16),('nose-scale-vert-incr.target',.13)]:
        for line in open(os.path.join(folder,name)):
            b=line.split()
            if not b or b[0].startswith('#'):continue
            points[int(b[0])]+=Vector(tuple(map(float,b[1:4])))*amount
    # Keep the neck continuous below the shirt's upper edge. The lower skin
    # flares inside the upper torso instead of ending at the collar plane.
    cutoff=5.95
    selected=[face for face in groups['body'] if max(points[i].y for i,uv in face)>cutoff]
    scale=.118;top=max(points[i].y for f in selected for i,uv in f);offset=1.888-top*scale
    def transform_point(p):
        q=Vector((p.x*scale*.96,p.y*scale+offset,p.z*scale-.075))
        if q.y<1.645:
            flare=1-smooth((q.y-1.515)/.085)
            radius_x=.054+.057*flare;radius_z=.049+.028*flare
            radial=math.sqrt((q.x/radius_x)**2+((q.z+.007)/radius_z)**2)
            if radial>1:
                shrink=1+(1/radial-1)*(1-smooth((q.y-1.610)/.035));q.x*=shrink;q.z=(q.z+.007)*shrink-.007
        front=smooth((q.z-.01)/.045)
        # Shallow continuous forehead creases and medial brow tension remain
        # part of the anatomical surface, rather than separate face primitives.
        for y in (1.808,1.822,1.835):q.z-=front*.0009*gauss(q.x,q.y,0,y,.064,.0015)
        q.z+=front*.0018*gauss(abs(q.x),q.y,.020,1.782,.019,.010)
        return q
    def neck_weights(y):
        neck=bones[head]['parent'];chest=bones[neck]['parent']
        if y<1.620:
            t=smooth((y-1.510)/.095);return {chest:1-t,neck:t}
        t=smooth((y-1.620)/.055);return {neck:1-t,head:t}
    mapping={};surface=[]
    for f in selected:
        polygon=[(points[i],Vector(tex[uv])) for i,uv in f];clipped=[]
        for previous,current in zip(polygon[-1:]+polygon[:-1],polygon):
            pa,ua=previous;pb,ub=current;insideA=pa.y>=cutoff;insideB=pb.y>=cutoff
            if insideA!=insideB:
                t=(cutoff-pa.y)/(pb.y-pa.y);clipped.append((pa.lerp(pb,t),ua.lerp(ub,t)))
            if insideB:clipped.append(current)
        if len(clipped)<3:continue
        out=[]
        for point,uv in clipped:
            key=tuple(round(v,7) for v in (*point,*uv))
            if key not in mapping:
                p=transform_point(point);mapping[key]=vertex(p,skin_color(*p),neck_weights(p.y),tuple(uv),smooth((p.y-1.61)/.035));surface.append(p)
            out.append(mapping[key])
        # MakeHuman OBJ winding is already outward in the Unity-coordinate data.
        face(*out)
    # Short individual stubble follows the actual cheek/chin surface normals.
    # Keep the lips and the nose clear rather than painting a uniform beard mask.
    beard=[]
    for f in F[:]:
        if len(f)<3:continue
        a,b,c=(Vector(V[f[i]]) for i in range(3));center=(a+b+c)/3
        normal=(b-a).cross(c-a).normalized()
        if not (1.605<center.y<1.704 and center.z>.025 and normal.z>.25):continue
        if abs(center.x)<.034 and abs(center.y-1.650)<.010:continue
        if abs(center.x)<.022 and center.y>1.683:continue
        beard.append((a,b,c,normal))
    for i in range(400):
        if not beard:break
        a,b,c,normal=random.choice(beard);u=random.random();v=random.random()
        if u+v>1:u=1-u;v=1-v
        p=a+(b-a)*u+(c-a)*v+normal*.00015;length=random.uniform(.0006,.0013)
        gray=random.random()<.30;shade=random.uniform(.22,.32) if gray else random.uniform(.065,.105)
        tube([p,p+normal*length+Vector((0,-length*.3,0))],[.00010,.00003],(shade,shade*.95,shade*.90,0),neck_weights(p.y),4)
    for name in ('joint-l-eye','joint-r-eye'):
        ids=set(i for f in groups[name] for i,uv in f);p=transform_point(sum((points[i] for i in ids),Vector())/len(ids))
        oval(p,(.013,.013,.013),(.46,.47,.43,0),{head:1},20,32)
        oval(p+Vector((0,0,.0128)),(.0063,.0063,.0005),(.10,.25,.33,.05),{head:1},12,24)
        oval(p+Vector((0,0,.0132)),(.0026,.0031,.0003),(.008,.01,.012,.2),{head:1},10,20)
    def surface_front(x,y):
        close=[p.z for p in surface if abs(p.x-x)<.009 and abs(p.y-y)<.009]
        return max(close) if close else .08
    # Eyebrows and fine skin detail use the supplied reference albedo, avoiding
    # a second geometrical eyebrow floating above the anatomical brow ridge.
    # A scalp shell follows actual cranial topology; strands originate on it.
    scalp=[];scalpmap={}
    for f in selected:
        pts=[transform_point(points[i]) for i,uv in f]
        if all(p.y>1.765+smooth((p.z-.01)/.08)*.068 for p in pts):
            indices=[]
            for p in pts:
                normal=Vector((p.x,(p.y-1.76)*.8,p.z+.01)).normalized();q=p+normal*(.0035+.0015*math.sin(p.x*370+p.z*140));key=tuple(round(v,6) for v in q)
                if key not in scalpmap:scalpmap[key]=vertex(q,(.085,.078,.070,0),{head:1},(p.x,p.z))
                indices.append(scalpmap[key]);scalp.append((q,normal))
            face(*indices)
    for i in range(1800):
        p,normal=random.choice(scalp);length=random.uniform(.014,.025);brush=Vector((random.uniform(-.3,.3),.10,-.8 if p.y<1.82 else .4))
        brush=(brush-normal*brush.dot(normal)).normalized()
        gray=random.random()<(.65 if abs(p.x)>.065 and p.y<1.82 else .32);shade=random.choice((.18,.27,.34)) if gray else random.choice((.055,.085,.11))
        pts=[p+normal*(math.sin(t*math.pi)*.0015)+brush*t*length for t in (0,.25,.5,.75,1)]
        tube(pts,[.00045,.00045,.0004,.00025,.00006],(shade,shade*.96,shade*.91,0),{head:1},5)
def make_doctor():
    reset();hips=addbone('Pelvis',-1,(0,.99,0));chest=addbone('Spine and rib cage',hips,(0,1.38,0));neck=addbone('Neck',chest,(0,1.59,0));head=addbone('Head',neck,(0,1.74,0))
    legs=[];arms=[]
    for side,label in ((-1,'Left'),(1,'Right')):
        thigh=addbone(label+' thigh',hips,(side*.101,.97,0));shin=addbone(label+' knee',thigh,(side*.103,.55,.015));foot=addbone(label+' ankle',shin,(side*.104,.115,-.005));legs.append((side,thigh,shin,foot))
    for side,label in ((-1,'Left'),(1,'Right')):
        upper=addbone(label+' shoulder',chest,(side*.216,1.525,0));lower=addbone(label+' elbow',upper,(side*.251,1.214,.008));hand=addbone(label+' wrist',lower,(side*.259,.949,.016));arms.append((side,upper,lower,hand))
    addbone('Short cape',chest,(0,1.45,-.13))
    rows=[(.94,.176,.105,0),(1.02,.17,.112,0),(1.12,.154,.105,0),(1.25,.168,.113,0),(1.38,.206,.123,0),(1.49,.218,.112,0),(1.535,.188,.083,-.010),(1.564,.071,.054,-.005)]
    loft(rows,SHIRT,lambda y:body_weight(y,hips,chest),64,fold=.0014)
    coatrows=[(.94,.187,.121,-.005),(1.01,.179,.125,0),(1.12,.164,.120,0),(1.25,.181,.128,0),(1.38,.215,.137,0),(1.48,.230,.126,-.005),(1.535,.205,.096,-.012),(1.558,.079,.061,-.009)]
    loft(coatrows,NAVY,lambda y:body_weight(y,hips,chest),72,opening=lambda y:.11+smooth((y-1.20)/.35)*.45,fold=.0028)
    for side in (-1,1):
        patch([(side*.043,1.554,.066),(side*.102,1.483,.110),(side*.071,1.420,.132),(side*.029,1.503,.087)],SHIRT,{chest:1},.002)
        patch([(side*.070,1.547,.071),(side*.137,1.464,.123),(side*.103,1.441,.141),(side*.054,1.293,.145),(side*.073,1.456,.132)],(.036,.052,.080,0),{chest:1},.004)
        patch([(side*.105,1.065,.109),(side*.166,1.065,.078),(side*.167,1.084,.080),(side*.105,1.084,.112)],NAVY,{hips:.8,chest:.2},.003)
        tube([(side*.207,1.44,-.018),(side*.181,1.29,-.111),(side*.151,1.10,-.112),(side*.168,.95,-.114)],[.0013]*4,(.017,.026,.044,0),lambda r:{chest:1} if r<2 else {hips:1},6)
    for y in (1.17,1.26,1.35,1.43):oval((0,y,.126),(.0032,.0032,.0017),(.56,.62,.66,.15),{chest:1},6,12)
    loft([(.905,.169,.101,0),(.97,.177,.110,0),(1.025,.165,.104,0)],NAVY,{hips:1},48,fold=.001)
    for side,thigh,shin,foot in legs:
        x=side*.103;pts=[(x,.99,0),(x,.90,0),(x,.76,.008),(x,.58,.015),(x,.54,.015),(x,.46,.008),(x,.29,0),(x,.13,-.005)]
        def legw(r):
            t=smooth((r-2.6)/2.0);return {thigh:1-t,shin:t} if r<7 else {shin:.55,foot:.45}
        tube(pts,[.079,.080,.070,.058,.060,.059,.051,.046],NAVY,legw,32,flatten=1.06)
        # Shaped shoe upper/sole along the actual foot, with toe box and heel.
        for sole in (True,False):
            start=len(V);sections=[(-.090,.037,.050),(-.065,.051,.075),(0,.058,.095),(.09,.061,.065),(.155,.048,.042),(.173,.002,.015)]
            for r,(z,rx,h) in enumerate(sections):
                for i in range(25):
                    a=i/24*math.tau;yy=.025+(math.sin(a)+1)*(.012 if sole else h)*.5
                    vertex((x+math.cos(a)*rx,yy,z),(.20,.20,.18,0) if sole else SHOE,{foot:1},(i/24,r/5))
            for r in range(5):
                for i in range(24):k=start+r*25+i;face(k,k+1,k+26,k+25)
        for j in range(5):
            z=.015+j*.018;tube([(x-.022,.093-j*.005,z),(x+.022,.093-j*.005,z+.008)],[.0015,.0015],(.34,.33,.30,0),{foot:1},6)
    for side,upper,lower,hand in arms:
        a=Vector(bones[upper]['position']);b=Vector(bones[lower]['position']);c=Vector(bones[hand]['position'])
        pts=[a+Vector((-side*.035,.025,0)),a,mix(a,b,.22),mix(a,b,.70),mix(a,b,.95),mix(b,c,.10),mix(b,c,.40),mix(b,c,.78),c]
        tube(pts,[.024,.062,.060,.052,.045,.045,.046,.037,.031],NAVY,lambda r:{upper:1-smooth((r-3)/3),lower:smooth((r-3)/3)} if r<8 else {lower:.6,hand:.4},32,flatten=.92)
        tube([c+Vector((0,.013,0)),c-Vector((0,.007,0))],[.032,.032],SHIRT,{lower:1},24)
        for j in range(3):oval((side*.284,1.001+j*.012,.018),(.003,.003,.002),(.06,.07,.08,.25),{lower:1},5,10)
        make_hand(side,hand)
    make_anatomical_head(head)

def save_and_export(name):
    bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
    # This coordinate conversion is a rotation (determinant +1), not a
    # reflection. Preserve winding so exported normals match visible faces.
    mesh=bpy.data.meshes.new(name+' authored topology');mesh.from_pydata([(x,-z,y) for x,y,z in V],[],F);mesh.update()
    obj=bpy.data.objects.new(name,mesh);bpy.context.collection.objects.link(obj)
    color=mesh.color_attributes.new(name='Color',type='FLOAT_COLOR',domain='POINT')
    metal=mesh.attributes.new(name='Metallic',type='FLOAT',domain='POINT')
    for i,c in enumerate(C):color.data[i].color=(*c[:3],1);metal.data[i].value=c[3]
    uv=mesh.uv_layers.new(name='AuthoringUV')
    for polygon in mesh.polygons:
        polygon.use_smooth=name=='Doctor012' or sum(C[i][3] for i in polygon.vertices)/len(polygon.vertices)<.5
        for loop in polygon.loop_indices:uv.data[loop].uv=UV[mesh.loops[loop].vertex_index]
    # Cache normals before writing attributes; RNA attribute updates invalidate
    # the lazy normal cache and otherwise trigger quadratic recomputation.
    normal_y=[v.normal.y for v in mesh.vertices]
    reference=mesh.attributes.new(name='ReferenceSkin',type='FLOAT',domain='POINT');reference_uv=mesh.uv_layers.new(name='HouseReferenceUV')
    for i,ny in enumerate(normal_y):reference.data[i].value=REF[i]*smooth((-ny-.10)/.50)
    for loop in mesh.loops:
        x,y,z=V[loop.vertex_index];reference_uv.data[loop.index].uv=((104+x*780)/1448,1-(855-(y-1.755)*705)/1086)
    mat=bpy.data.materials.new('Skin cloth and metal PBR');mat.use_nodes=True;nodes=mat.node_tree.nodes;shader=nodes.get('Principled BSDF');attribute=nodes.new('ShaderNodeVertexColor');attribute.layer_name='Color';mat.node_tree.links.new(attribute.outputs['Color'],shader.inputs['Base Color']);metallic=nodes.new('ShaderNodeAttribute');metallic.attribute_name='Metallic';mat.node_tree.links.new(metallic.outputs['Fac'],shader.inputs['Metallic']);shader.inputs['Roughness'].default_value=.63;obj.data.materials.append(mat)
    if name=='Doctor012':
        image=bpy.data.images.load(os.path.join(ROOT,'Assets','Resources','Characters','DoctorReference.png'));image.pack();texture=nodes.new('ShaderNodeTexImage');texture.image=image;uv_node=nodes.new('ShaderNodeUVMap');uv_node.uv_map='HouseReferenceUV';mat.node_tree.links.new(uv_node.outputs['UV'],texture.inputs['Vector'])
        mask=nodes.new('ShaderNodeAttribute');mask.attribute_name='ReferenceSkin';mix_node=nodes.new('ShaderNodeMixRGB');mat.node_tree.links.new(mask.outputs['Fac'],mix_node.inputs[0]);mat.node_tree.links.new(attribute.outputs['Color'],mix_node.inputs[1]);mat.node_tree.links.new(texture.outputs['Color'],mix_node.inputs[2]);mat.node_tree.links.new(mix_node.outputs[0],shader.inputs['Base Color'])
    arm=bpy.data.armatures.new('Doctor012 anatomical skeleton');rig=bpy.data.objects.new('Doctor012 rig',arm);bpy.context.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT');eb=[]
    for i,b in enumerate(bones):
        bone=arm.edit_bones.new(b['name']);x,y,z=b['position'];bone.head=(x,-z,y)
        children=[c for c in bones if c['parent']==i];tail=children[0]['position'] if children else (x,y-.06,z)
        bone.tail=(tail[0],-tail[2],tail[1]);
        if (bone.tail-bone.head).length<.001:bone.tail=bone.head+Vector((0,0,.04))
        if b['parent']>=0:bone.parent=eb[b['parent']]
        eb.append(bone)
    bpy.ops.object.mode_set(mode='OBJECT');groups=[obj.vertex_groups.new(name=b['name']) for b in bones]
    for i,w in enumerate(W):
        for bone,amount in w.items():
            if amount>0:groups[bone].add([i],amount,'REPLACE')
    modifier=obj.modifiers.new('Anatomical skin weights','ARMATURE');modifier.object=rig;obj.parent=rig;rig.show_in_front=True
    obj['runtime_contract']='Exported mesh, UVs, normals, colors, weights and exact bind skeleton; no runtime primitive Doctor fallback.'
    source=SOURCE if name=='Doctor012' else os.path.join(ROOT,'SourceAssets','Equipment','Remaster012');os.makedirs(source,exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(source,name+'.blend'))
    # Blender triangulates the editable topology; export loop normals and UVs.
    mesh.calc_loop_triangles();verts=[];triangles=[];exported={}
    for tri in mesh.loop_triangles:
        for loop in tri.loops:
            i=mesh.loops[loop].vertex_index;p=V[i];n=mesh.corner_normals[loop].vector;key=(i,round(n.x,5),round(n.y,5),round(n.z,5))
            if key not in exported:
                w=sorted(W[i].items(),key=lambda v:-v[1]);a,wa=w[0];b,wb=w[1] if len(w)>1 else (a,0)
                exported[key]=len(verts);verts.append(dict(p=dict(zip(('x','y','z'),p)),n=dict(x=n.x,y=n.z,z=-n.y),uv=dict(zip(('x','y'),UV[i])),c=dict(zip(('r','g','b','a'),C[i])),skin=reference.data[i].value,faceUV=dict(x=reference_uv.data[loop].uv.x,y=reference_uv.data[loop].uv.y),a=a,b=b,w=wb))
            triangles.append(exported[key])
    data=dict(version=12,name=name,bones=[dict(name=b['name'],parent=b['parent'],p=dict(zip(('x','y','z'),b['position']))) for b in bones],vertices=verts,triangles=triangles)
    output=OUT if name=='Doctor012' else os.path.join(ROOT,'Assets','Resources','Equipment');os.makedirs(output,exist_ok=True)
    with open(os.path.join(output,name+'.json'),'w') as f:json.dump(data,f,separators=(',',':'))
    with open(os.path.join(source,name+'-audit.json'),'w') as f:json.dump(dict(vertices=len(V),faces=len(F),triangles=len(triangles)//3,bones=len(bones),uvLayers=len(mesh.uv_layers),runtimeVertices=len(verts)),f,indent=2)
    print('AEVORIA012_AUTHORED',name,len(V),len(triangles)//3,'triangles')

def equipment_start():reset();addbone('Grip',-1,(0,0,0))
def wood(r,i,p):
    grain=.025*math.sin(i*.71+p[1]*61)+.012*math.cos(i*2+p[1]*7)
    return (.235+grain,.122+grain*.6,.048+grain*.25,0)
def handle(top=.67):
    pts=[(.004*math.sin(j*.8),-.19+j/12*(top+.19),.003*math.sin(j)) for j in range(13)]
    tube(pts,[.018+.006*math.sin(math.pi*j/12)**2 for j in range(13)],wood,{0:1},24,flatten=.9)
    for y in (-.18,.48):tube([(0,y,0),(0,y+.024,0)],[.024,.024],STEEL,{0:1},24)
def pickaxe():
    equipment_start();handle(.73)
    points=[(-.37,.51,0),(-.30,.60,0),(-.20,.68,0),(-.09,.72,0),(0,.735,0),(.10,.71,0),(.23,.63,0),(.36,.50,0)]
    tube(points,[.002,.011,.025,.037,.047,.034,.018,.0015],STEEL,{0:1},8,flatten=.78)
    tube([(0,.685,0),(0,.775,0)],[.037,.032],(.22,.245,.26,.9),{0:1},16)
    oval((0,.777,0),(.023,.003,.021),(.30,.16,.055,0),{0:1},6,16)
    patch([(-.009,.769,-.012),(.009,.769,-.012),(.009,.786,.012),(-.009,.786,.012)],(.36,.39,.40,.9),{0:1},.002)
def shovel():
    equipment_start();handle(.69)
    tube([(0,-.18,0),(-.053,-.215,0),(-.05,-.29,0),(0,-.315,0),(.05,-.29,0),(.053,-.215,0),(0,-.18,0)],[.010]*7,wood,{0:1},12)
    tube([(0,.51,0),(0,.72,.008)],[.030,.034],STEEL,{0:1},20)
    start=len(V);rows=14;columns=16
    for back in (False,True):
        for r in range(rows+1):
            t=r/rows;half=.125*max(.01,math.sin(math.pi*(.12+t*.88)))**.45
            for i in range(columns+1):
                u=i/columns*2-1;z=.029*(1-u*u)+(.004 if back else 0)
                vertex((u*half,.65+t*.36,z),(.30,.34,.355,.86),{0:1},(i/columns,t))
        for r in range(rows):
            for i in range(columns):
                k=start+int(back)*(rows+1)*(columns+1)+r*(columns+1)+i
                face(k,k+columns+1,k+columns+2,k+1) if back else face(k,k+1,k+columns+2,k+columns+1)
    span=(rows+1)*(columns+1)
    for r in range(rows):
        for edge in (0,columns):k=start+r*(columns+1)+edge;face(k,k+span,k+columns+1+span,k+columns+1)
    for r in (0,rows):
        for i in range(columns):k=start+r*(columns+1)+i;face(k,k+1,k+span+1,k+span)
    for side in (-1,1):tube([(side*.04,.668,0),(side*.115,.668,0)],[.008,.008],STEEL,{0:1},8)
    for y in (.58,.63):oval((0,y,.032),(.005,.005,.003),(.42,.45,.46,.9),{0:1},6,12)
def axe():
    equipment_start();handle(.72)
    # Forged wedge: broad cheeks taper into a thin convex cutting edge.
    profile=[(-.075,.61),(-.085,.73),(-.040,.77),(.085,.765),(.23,.81),(.255,.69),(.224,.555),(.085,.605)]
    start=len(V)
    for side in (-1,1):
        for x,y in profile:vertex((x,y,side*(.003 if x>.22 else .028 if x<.1 else .018)),STEEL,{0:1},(x,y))
    n=len(profile);face(*[start+i for i in reversed(range(n))]);face(*[start+n+i for i in range(n)])
    for i in range(n):j=(i+1)%n;face(start+i,start+j,start+n+j,start+n+i)
    tube([(0,.62,0),(0,.76,0)],[.033,.035],(.24,.265,.28,.85),{0:1},16)
    for z in (-.029,.029):oval((-.04,.69,z),(.007,.007,.002),(.44,.46,.47,.9),{0:1},6,12)
def stick():
    equipment_start();pts=[(.018*math.sin(j*.65),-.15+j*.095,.012*math.sin(j*1.4)) for j in range(11)]
    tube(pts,[.022-j*.0011 for j in range(11)],wood,{0:1},18)
    for j in (3,7):p=Vector(pts[j]);tube([p,p+Vector((.045,.04,.01)),p+Vector((.065,.045,.01))],[.010,.006,.001],wood,{0:1},10)
def block(center,size,color):
    x,y,z=center;a,b,c=[v*.5 for v in size];start=len(V)
    for zz in (-c,c):
        for yy in (-b,b):
            for xx in (-a,a):vertex((x+xx,y+yy,z+zz),color,{0:1},(xx/a,yy/b))
    for f in ((0,2,3,1),(4,5,7,6),(0,1,5,4),(2,6,7,3),(0,4,6,2),(1,3,7,5)):face(*[start+i for i in f])
def crate():
    equipment_start()
    for i in range(5):
        for z in (-.16,.16):block(((i-2)*.063,.18,z),(.059,.31,.026),wood(0,i,(0,0,0)))
        for x in (-.16,.16):block((x,.18,(i-2)*.063),(.026,.31,.059),wood(0,i+7,(0,0,0)))
        for y in (.025,.335):block(((i-2)*.063,y,0),(.059,.026,.32),wood(0,i+3,(0,0,0)))
    for x in (-.13,.13):
        for z in (-.18,.18):block((x,.18,z),(.025,.34,.009),(.15,.17,.18,.75))
    for x in (-.13,.13):
        for y in (.055,.30):
            for z in (-.187,.187):oval((x,y,z),(.006,.006,.003),STEEL,{0:1},5,10)
def cane():
    equipment_start();tube([(0,0,0),(0,-.18,0),(0,-.55,0),(0,-.87,0)],[.013,.012,.010,.010],(.025,.025,.024,.45),{0:1},24)
    tube([(-.055,-.012,0),(-.065,.005,0),(-.045,.018,0),(.025,.018,0),(.060,.006,0),(.065,-.012,0)],[.015,.019,.022,.023,.021,.014],(.025,.024,.022,.1),{0:1},16)
    tube([(0,-.86,0),(0,-.885,0),(0,-.900,0)],[.014,.020,.020],(.018,.018,.017,0),{0:1},24)
    for j in range(3):tube([(0,-.30-j*.18,0),(0,-.31-j*.18,0)],[.012,.012],(.46,.22,.045,.2),{0:1},24)

make_doctor();save_and_export('Doctor012')
for name,builder in [('Pickaxe012',pickaxe),('Shovel012',shovel),('Axe012',axe),('Stick012',stick),('Crate012',crate),('Cane012',cane)]:builder();save_and_export(name)
