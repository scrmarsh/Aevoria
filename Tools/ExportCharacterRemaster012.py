"""Export edited Remaster012 .blend files, without regenerating their geometry.

blender --background --python Tools/ExportCharacterRemaster012.py -- M:/Aevoria [file.blend ...]
Exports rest geometry, split normals, corner UV/colors, Metallic and two-bone skin.
The Blender file remains the authority. Does not save or overwrite the source.
"""
import bpy, json, os, sys
from mathutils import Vector

args=sys.argv[sys.argv.index('--')+1:]
root=os.path.abspath(args[0])
paths=args[1:] or [os.path.join(root,'SourceAssets',kind,'Remaster012',name+'.blend')
    for kind,names in [('Characters',['Doctor012']),('Equipment',['Pickaxe012','Shovel012','Axe012','Stick012','Crate012','Cane012'])] for name in names]

def xyz(v): return dict(x=v.x,y=v.z,z=-v.y)
for path in paths:
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(path))
    name=os.path.splitext(os.path.basename(path))[0]
    rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    if len(rigs)!=1: raise ValueError('Expected one rest skeleton in '+path)
    rig=rigs[0];rig.data.pose_position='REST'
    bones=list(rig.data.bones);ids={b.name:i for i,b in enumerate(bones)}
    skeleton=[dict(name=b.name,parent=ids[b.parent.name] if b.parent else -1,p=xyz(rig.matrix_world@b.head_local)) for b in bones]
    vertices=[];triangles=[]
    for obj in bpy.context.scene.objects:
        if obj.type!='MESH' or obj.hide_render: continue
        # Apply all modeling modifiers, with the armature held in rest pose.
        graph=bpy.context.evaluated_depsgraph_get();evaluated=obj.evaluated_get(graph)
        mesh=evaluated.to_mesh(preserve_all_data_layers=True,depsgraph=graph)
        try:
            mesh.calc_loop_triangles();uv=mesh.uv_layers.get('AuthoringUV') or mesh.uv_layers.active;face_uv=mesh.uv_layers.get('HouseReferenceUV')
            if uv is None: raise ValueError('Missing UVs: '+obj.name)
            colors=mesh.color_attributes.get('Color');metal=mesh.attributes.get('Metallic');reference=mesh.attributes.get('ReferenceSkin')
            if colors is None or metal is None: raise ValueError('Missing Color/Metallic: '+obj.name)
            normal_matrix=obj.matrix_world.to_3x3().inverted().transposed();exported={}
            for tri in mesh.loop_triangles:
                for loop in tri.loops:
                    index=mesh.loops[loop].vertex_index;v=mesh.vertices[index]
                    n=(normal_matrix@mesh.corner_normals[loop].vector).normalized();tex=uv.data[loop].uv
                    color=colors.data[index if colors.domain=='POINT' else loop].color
                    metallic=metal.data[index if metal.domain=='POINT' else loop].value
                    projection=face_uv.data[loop].uv if face_uv else Vector((0,0))
                    key=(index,tuple(n),tuple(tex),tuple(projection),tuple(color),metallic)
                    if key not in exported:
                        weights=[(ids[obj.vertex_groups[g.group].name],g.weight) for g in v.groups if obj.vertex_groups[g.group].name in ids and g.weight>0]
                        weights.sort(key=lambda w:-w[1])
                        if not weights: raise ValueError('Unweighted vertex: '+obj.name)
                        a,wa=weights[0];b,wb=weights[1] if len(weights)>1 else (a,0)
                        if '_LOD' not in name and sum(w for _,w in weights[2:])>.01: raise ValueError('Runtime needs <=2 significant bone weights: '+obj.name)
                        exported[key]=len(vertices)
                        vertices.append(dict(p=xyz(obj.matrix_world@v.co),n=xyz(n),uv=dict(x=tex.x,y=tex.y),c=dict(r=color[0],g=color[1],b=color[2],a=metallic),skin=reference.data[index].value if reference else 0,faceUV=dict(x=projection.x,y=projection.y),a=a,b=b,w=wb/(wa+wb)))
                    triangles.append(exported[key])
        finally: evaluated.to_mesh_clear()
    target=os.path.join(root,'Assets','Resources','Characters' if name.startswith('Doctor012') else 'Equipment',name+'.json')
    with open(target,'w') as f: json.dump(dict(version=12,name=name,bones=skeleton,vertices=vertices,triangles=triangles),f,separators=(',',':'))
    print('EXPORTED_EDITABLE_SOURCE',name,len(vertices),'vertices',len(triangles)//3,'triangles')
