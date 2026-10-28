"""Run with Blender --background --python ... -- <skin-json-folder>.

Imports the exact runtime-generated weighted geometry, preserving editability.
The reference sheet guides authoring; it is never used as a character billboard.
"""
import bpy
import json
import os
import sys
from mathutils import Vector

folder = os.path.abspath(sys.argv[sys.argv.index('--') + 1])

def vec(v):
    return (v['x'], -v['z'], v['y'])

for preset in ('Explorer', 'Roman', 'Doctor'):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    with open(os.path.join(folder, preset + '-skin.json'), encoding='utf-8-sig') as stream:
        data = json.load(stream)
    mesh = bpy.data.meshes.new(preset + ' authored weighted geometry')
    faces = [data['triangles'][i:i + 3] for i in range(0, len(data['triangles']), 3)]
    mesh.from_pydata([vec(p) for p in data['vertices']], [], faces)
    mesh.update()
    obj = bpy.data.objects.new(preset, mesh)
    bpy.context.collection.objects.link(obj)
    colors = mesh.color_attributes.new(name='Color', type='FLOAT_COLOR', domain='POINT')
    metallic = mesh.attributes.new(name='Metallic', type='FLOAT', domain='POINT')
    for i, color in enumerate(data['colors']):
        colors.data[i].color = (color['r'], color['g'], color['b'], 1)
        metallic.data[i].value = color['a']
    material = bpy.data.materials.new('Authored surface colors')
    material.use_nodes = True
    shader = material.node_tree.nodes.get('Principled BSDF')
    attribute = material.node_tree.nodes.new('ShaderNodeVertexColor')
    attribute.layer_name = 'Color'
    material.node_tree.links.new(attribute.outputs['Color'], shader.inputs['Base Color'])
    metal_attribute = material.node_tree.nodes.new('ShaderNodeAttribute')
    metal_attribute.attribute_name = 'Metallic'
    material.node_tree.links.new(metal_attribute.outputs['Fac'], shader.inputs['Metallic'])
    shader.inputs['Roughness'].default_value = .65
    obj.data.materials.append(material)
    for polygon in mesh.polygons:
        polygon.use_smooth = True
    armature = bpy.data.armatures.new('Reusable humanoid armature')
    rig = bpy.data.objects.new('Humanoid rig', armature)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')
    bones = []
    for i, source in enumerate(data['bones']):
        bone = armature.edit_bones.new(source['name'])
        bone.head = vec(source['position'])
        children = [b for b in data['bones'] if b['parent'] == i]
        bone.tail = vec(children[0]['position']) if children else bone.head + Vector((0, 0, .08))
        if (bone.tail - bone.head).length < .001:
            bone.tail = bone.head + Vector((0, 0, .08))
        if source['parent'] >= 0:
            bone.parent = bones[source['parent']]
        bones.append(bone)
    bpy.ops.object.mode_set(mode='OBJECT')
    groups = [obj.vertex_groups.new(name=b['name']) for b in data['bones']]
    for vertex, weight in enumerate(data['weights']):
        for i in range(4):
            amount = weight['m_Weight' + str(i)]
            if amount > 0:
                groups[weight['m_BoneIndex' + str(i)]].add([vertex], amount, 'ADD')
    modifier = obj.modifiers.new('Humanoid skinning', 'ARMATURE')
    modifier.object = rig
    obj.parent = rig
    if data.get('caneVertices'):
        cane_mesh = bpy.data.meshes.new('Cane shaft and grip')
        cane_faces = [data['caneTriangles'][i:i + 3] for i in range(0, len(data['caneTriangles']), 3)]
        cane_mesh.from_pydata([vec(p) for p in data['caneVertices']], [], cane_faces)
        cane_obj = bpy.data.objects.new('Left hand walking cane', cane_mesh)
        bpy.context.collection.objects.link(cane_obj)
        group = cane_obj.vertex_groups.new(name='Left wrist')
        group.add(list(range(len(data['caneVertices']))), 1, 'ADD')
        cane_modifier = cane_obj.modifiers.new('Left wrist attachment', 'ARMATURE')
        cane_modifier.object = rig
        cane_obj.parent = rig
    rig.show_in_front = True
    obj['runtime_source'] = 'Assets/Scripts/Presentation/HumanoidPresentation.cs'
    obj['description'] = 'Editable export of runtime procedural rig; reference likeness requires native review.'
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(folder, preset + '.blend'))
print('AEVORIA_EDITABLE_CHARACTERS_COMPLETE')
