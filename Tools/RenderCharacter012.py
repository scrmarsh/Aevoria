import bpy,os,sys,math
from mathutils import Vector
out=os.path.abspath(sys.argv[sys.argv.index('--')+1]);os.makedirs(out,exist_ok=True)
face_y=float(sys.argv[sys.argv.index('--')+2]) if len(sys.argv)>sys.argv.index('--')+2 else 1.75
scene=bpy.context.scene;scene.render.engine='BLENDER_EEVEE';scene.render.resolution_percentage=100;scene.render.image_settings.file_format='PNG'
scene.world.color=(.08,.08,.08);scene.view_settings.exposure=-1.2
def aim(obj,point):obj.rotation_euler=(Vector(point)-obj.location).to_track_quat('-Z','Y').to_euler()
for name,pos,power,size in [('Key',(2,-3,3),350,2),('Fill',(-2,-1,2),140,2),('Rim',(0,2,3),400,2)]:
    data=bpy.data.lights.new(name,'AREA');data.energy=power;data.shape='DISK';data.size=size;obj=bpy.data.objects.new(name,data);bpy.context.collection.objects.link(obj);obj.location=pos;aim(obj,(0,0,1.2))
data=bpy.data.cameras.new('Review camera');camera=bpy.data.objects.new('Review camera',data);bpy.context.collection.objects.link(camera);scene.camera=camera;data.type='ORTHO'
for name,pos,target,scale,width,height in [('face-front',(0,-3,face_y),(0,0,face_y),.40,640,640),('face-profile',(3,-.1,face_y),(0,0,face_y),.40,640,640),('body-front',(0,-4,1.05),(0,0,.96),2.12,720,1080)]:
    camera.location=pos;aim(camera,target);data.ortho_scale=scale;scene.render.resolution_x=width;scene.render.resolution_y=height;scene.render.filepath=os.path.join(out,name+'.png');bpy.ops.render.render(write_still=True)
