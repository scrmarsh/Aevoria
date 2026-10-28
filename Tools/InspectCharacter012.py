import bpy,json,os,sys
folder=os.path.abspath(sys.argv[sys.argv.index('--')+1])
report=[]
for obj in bpy.data.objects:
    row={'name':obj.name,'type':obj.type}
    if obj.type=='MESH':row.update(vertices=len(obj.data.vertices),faces=len(obj.data.polygons),uvLayers=len(obj.data.uv_layers),weights=len(obj.vertex_groups))
    if obj.type=='ARMATURE':row['bones']=[b.name for b in obj.data.bones]
    report.append(row)
with open(os.path.join(folder,'existing-doctor-inspection.json'),'w') as f:json.dump(report,f,indent=2)
print('AEVORIA012_EXISTING_ASSET_INSPECTED')
