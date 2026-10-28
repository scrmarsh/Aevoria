"""Generate editable distance LODs from the authored rest mesh, then export.
Run after authoring/exporting Doctor012.blend. Never modifies the near source.
"""
import bpy, os, sys, runpy
root=os.path.abspath(sys.argv[sys.argv.index('--')+1]);folder=os.path.join(root,'SourceAssets','Characters','Remaster012');paths=[]
for level,ratio in [(1,.40),(2,.12)]:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(folder,'Doctor012.blend'))
    for rig in [o for o in bpy.context.scene.objects if o.type=='ARMATURE']:rig.data.pose_position='REST'
    for obj in [o for o in bpy.context.scene.objects if o.type=='MESH']:
        bpy.ops.object.select_all(action='DESELECT');obj.select_set(True);bpy.context.view_layer.objects.active=obj
        modifier=obj.modifiers.new('Distance LOD collapse','DECIMATE');modifier.ratio=ratio
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        obj['near_source']='Doctor012.blend';obj['lod_ratio']=ratio
    path=os.path.join(folder,'Doctor012_LOD'+str(level)+'.blend');bpy.ops.wm.save_as_mainfile(filepath=path);paths.append(path)
sys.argv=['export','--',root]+paths
runpy.run_path(os.path.join(root,'Tools','ExportCharacterRemaster012.py'),run_name='__main__')
