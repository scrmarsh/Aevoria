using System.Collections.Generic;
using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    // Runtime-authored skeletal asset: all vertices have explicit bind-pose
    // weights. Rigid armor stays attached to a single bone; flesh at joints
    // blends between adjacent bones. No animation moves the physics root.
    public partial class HumanoidPresentation:MonoBehaviour
    {
        private readonly List<Transform> bones=new List<Transform>();private readonly List<Vector3> bind=new List<Vector3>();
        private readonly List<Vector3> vertices=new List<Vector3>();private readonly List<Color> colors=new List<Color>();private readonly List<int> indices=new List<int>();private readonly List<BoneWeight> weights=new List<BoneWeight>();
        private int hips,chest,neck,head,leftThigh,leftShin,leftFoot,rightThigh,rightShin,rightFoot,leftUpper,leftLower,leftHand,rightUpper,rightLower,rightHand,cape;
        private Mesh mesh;private Material material;private CharacterAppearance appearance;private Transform cane;private float phase,blend,landing,lastVertical;private bool wasGrounded;private readonly Quaternion[] pose=new Quaternion[64];
        public CharacterAppearance Appearance=>appearance;public Transform LeftHand=>bones[leftHand];public Transform RightHand=>bones[rightHand];public SkinnedMeshRenderer Skin{get;private set;}public string AnimationState{get;private set;}="Idle";
        public Transform Cane=>cane;
        private static readonly Color Flesh=new Color(.64f,.43f,.29f,0),Crimson=new Color(.32f,.018f,.028f,0),Leather=new Color(.14f,.075f,.035f,0),Silver=new Color(.49f,.54f,.57f,.78f),Dark=new Color(.035f,.025f,.02f,0);
        private int Bone(string name,int parent,Vector3 position)
        {var t=new GameObject(name).transform;t.SetParent(parent<0?transform:bones[parent],false);t.localPosition=parent<0?position:position-bind[parent];int id=bones.Count;bones.Add(t);bind.Add(position);return id;}
        public void Initialize(CharacterAppearance preset=CharacterAppearance.Roman)
        {
            appearance=preset;
            if(preset==CharacterAppearance.Doctor){LoadAuthoredDoctor();CreateCane();return;}
            hips=Bone("Pelvis",-1,new Vector3(0,.94f,0));chest=Bone("Spine and rib cage",hips,new Vector3(0,1.30f,0));neck=Bone("Neck",chest,new Vector3(0,1.51f,0));head=Bone("Head",neck,new Vector3(0,1.65f,0));
            leftThigh=Bone("Left thigh",hips,new Vector3(-.105f,.92f,0));leftShin=Bone("Left knee",leftThigh,new Vector3(-.105f,.51f,0));leftFoot=Bone("Left ankle",leftShin,new Vector3(-.105f,.10f,0));rightThigh=Bone("Right thigh",hips,new Vector3(.105f,.92f,0));rightShin=Bone("Right knee",rightThigh,new Vector3(.105f,.51f,0));rightFoot=Bone("Right ankle",rightShin,new Vector3(.105f,.10f,0));
            leftUpper=Bone("Left shoulder",chest,new Vector3(-.235f,1.43f,0));leftLower=Bone("Left elbow",leftUpper,new Vector3(-.27f,1.13f,.015f));leftHand=Bone("Left wrist",leftLower,new Vector3(-.28f,.87f,.04f));rightUpper=Bone("Right shoulder",chest,new Vector3(.235f,1.43f,0));rightLower=Bone("Right elbow",rightUpper,new Vector3(.27f,1.13f,.015f));rightHand=Bone("Right wrist",rightLower,new Vector3(.28f,.87f,.04f));cape=Bone("Short cape",chest,new Vector3(0,1.43f,-.13f));
            // Tunic and torso have a human shoulder/waist silhouette.
            Loft(new[]{new Vector3(0,.77f,0),new Vector3(0,1.02f,0),new Vector3(0,1.29f,0),new Vector3(0,1.44f,0)},new[]{new Vector2(.23f,.15f),new Vector2(.165f,.115f),new Vector2(.22f,.14f),new Vector2(.205f,.12f)},appearance==CharacterAppearance.Roman?Crimson:appearance==CharacterAppearance.Doctor?new Color(.075f,.085f,.11f,0):new Color(.22f,.28f,.20f,0),hips,chest);
            Ellipsoid(new Vector3(0,1.49f,0),new Vector3(.065f,.085f,.065f),Flesh,neck);
            if(appearance==CharacterAppearance.Doctor)BuildDoctorHead();else
            {
                Ellipsoid(new Vector3(0,1.66f,.005f),new Vector3(.098f,.133f,.092f),Flesh,head);
            Ellipsoid(new Vector3(0,1.665f,.094f),new Vector3(.022f,.034f,.027f),Flesh,head);Ellipsoid(new Vector3(-.098f,1.66f,0),new Vector3(.018f,.034f,.022f),Flesh,head);Ellipsoid(new Vector3(.098f,1.66f,0),new Vector3(.018f,.034f,.022f),Flesh,head);
            foreach(float side in new[]{-1f,1f}){Ellipsoid(new Vector3(side*.039f,1.69f,.085f),new Vector3(.025f,.010f,.009f),new Color(.72f,.72f,.65f,0),head);Ellipsoid(new Vector3(side*.039f,1.69f,.094f),new Vector3(.009f,.009f,.004f),Dark,head);Box(new Vector3(side*.039f,1.711f,.087f),new Vector3(.049f,.008f,.012f),Leather,head);}
            Box(new Vector3(0,1.609f,.089f),new Vector3(.044f,.006f,.008f),new Color(.34f,.17f,.13f,0),head);
            }
            var trousers=appearance==CharacterAppearance.Roman?Flesh:new Color(.055f,.065f,.085f,0);ArticulatedLimb(leftThigh,leftShin,leftFoot,.091f,.065f,.043f,trousers);ArticulatedLimb(rightThigh,rightShin,rightFoot,.091f,.065f,.043f,trousers);
            var sleeves=appearance==CharacterAppearance.Roman?Flesh:appearance==CharacterAppearance.Doctor?new Color(.075f,.085f,.11f,0):new Color(.22f,.28f,.20f,0);ArticulatedLimb(leftUpper,leftLower,leftHand,.073f,.055f,.036f,sleeves);ArticulatedLimb(rightUpper,rightLower,rightHand,.073f,.055f,.036f,sleeves);
            Ellipsoid(bind[leftHand]+new Vector3(0,-.055f,.01f),new Vector3(.044f,.075f,.029f),Flesh,leftHand);Ellipsoid(bind[rightHand]+new Vector3(0,-.055f,.01f),new Vector3(.044f,.075f,.029f),Flesh,rightHand);
            if(appearance==CharacterAppearance.Roman)
            {
            // Five overlapping curved lorica bands, raised fasteners and leather belt.
            for(int band=0;band<5;band++){float y=1.075f+band*.065f;float rx=Mathf.Lerp(.172f,.229f,band/4f);Loft(new[]{new Vector3(0,y,0),new Vector3(0,y+.052f,0)},new[]{new Vector2(rx,.135f),new Vector2(rx+.004f,.14f)},Silver,chest,chest);foreach(float side in new[]{-1f,1f})Box(new Vector3(side*.055f,y+.026f,.143f),new Vector3(.016f,.016f,.014f),Leather,chest);}
            foreach(int arm in new[]{leftUpper,rightUpper})for(int i=0;i<3;i++)ShoulderBand(arm,Mathf.Sign(bind[arm].x)*(.19f+i*.045f),1.445f-i*.018f);
            Loft(new[]{new Vector3(0,.986f,0),new Vector3(0,1.035f,0)},new[]{new Vector2(.178f,.134f),new Vector2(.178f,.134f)},Leather,hips,hips);Box(new Vector3(0,1.01f,.14f),new Vector3(.047f,.038f,.018f),new Color(.48f,.33f,.12f,.6f),hips);
            for(int i=-3;i<=3;i++){float x=i*.048f;Box(new Vector3(x,.87f,.151f-Mathf.Abs(x)*.12f),new Vector3(.031f,.22f,.018f),Leather,hips);}
            foreach(int foot in new[]{leftFoot,rightFoot}){var p=bind[foot];Ellipsoid(p+new Vector3(0,-.047f,.065f),new Vector3(.066f,.046f,.13f),Leather,foot);for(int i=0;i<3;i++)Box(p+new Vector3(0,-.001f,.012f+i*.052f),new Vector3(.13f,.018f,.023f),new Color(.24f,.14f,.07f,0),foot);Loft(new[]{p+Vector3.up*.04f,p+Vector3.up*.13f},new[]{new Vector2(.047f,.05f),new Vector2(.049f,.05f)},Leather,foot,foot);}
            // Helmet dome, brow, neck guard, cheek guards and longitudinal crest.
            Ellipsoid(new Vector3(0,1.742f,-.008f),new Vector3(.112f,.082f,.109f),Silver,head);Box(new Vector3(0,1.736f,.093f),new Vector3(.215f,.022f,.046f),Silver,head);Box(new Vector3(0,1.674f,-.099f),new Vector3(.22f,.095f,.025f),Silver,head);foreach(float side in new[]{-1f,1f})Ellipsoid(new Vector3(side*.086f,1.641f,.042f),new Vector3(.024f,.073f,.042f),Silver,head);
            Box(new Vector3(0,1.82f,-.005f),new Vector3(.039f,.024f,.16f),Leather,head);for(int i=0;i<11;i++)Ellipsoid(new Vector3(0,1.865f-Mathf.Abs(i-5)*.004f,-.092f+i*.017f),new Vector3(.025f,.061f,.014f),new Color(.54f,.025f,.035f,0),head);
            // Broad short cloth cape with a small fold at each panel edge.
            for(int i=0;i<8;i++){float x=(i-3.5f)*.055f;Box(new Vector3(x,1.14f,-.174f-Mathf.Abs(x)*.14f),new Vector3(.057f,.52f,.019f),Crimson,cape);}foreach(float side in new[]{-1f,1f})Ellipsoid(new Vector3(side*.17f,1.43f,.094f),Vector3.one*.025f,new Color(.48f,.33f,.12f,.6f),chest);
            }
            else BuildCivilian();
            mesh=new Mesh{name=appearance+" weighted humanoid skin"};mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.boneWeights=weights.ToArray();var poses=new Matrix4x4[bones.Count];for(int i=0;i<poses.Length;i++)poses[i]=bones[i].worldToLocalMatrix*transform.localToWorldMatrix;mesh.bindposes=poses;mesh.RecalculateNormals();mesh.RecalculateBounds();
            material=new Material(Resources.Load<Material>("RomanSoldier"));Skin=gameObject.AddComponent<SkinnedMeshRenderer>();Skin.sharedMesh=mesh;Skin.sharedMaterial=material;Skin.bones=bones.ToArray();Skin.rootBone=bones[hips];Skin.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2.6f,2.2f));Skin.updateWhenOffscreen=false;
            if(appearance==CharacterAppearance.Doctor)CreateCane();
        }
        private void BuildCivilian()
        {
            bool doctor=appearance==CharacterAppearance.Doctor;var cloth=new Color(.075f,.085f,.11f,0);var shirt=doctor?new Color(.36f,.55f,.72f,0):new Color(.38f,.32f,.21f,0);
            // A shaped open jacket, shirt placket and separate collar pieces.
            Loft(new[]{new Vector3(0,1.02f,.122f),new Vector3(0,1.28f,.143f),new Vector3(0,1.43f,.12f)},new[]{new Vector2(.078f,.018f),new Vector2(.085f,.022f),new Vector2(.065f,.018f)},shirt,chest,chest);
            foreach(float side in new[]{-1f,1f})
            {
                Box(new Vector3(side*.095f,1.31f,.144f),new Vector3(.044f,.21f,.018f),doctor?cloth:shirt,chest);
                Box(new Vector3(side*.052f,1.42f,.116f),new Vector3(.055f,.055f,.026f),shirt,chest);
                Box(new Vector3(side*.13f,1.075f,.13f),new Vector3(.075f,.018f,.02f),cloth,chest);
            }
            for(int i=0;i<4;i++)Ellipsoid(new Vector3(0,1.06f+i*.075f,.165f),Vector3.one*.004f,new Color(.7f,.73f,.76f,0),chest);
            foreach(int foot in new[]{leftFoot,rightFoot})
            {
                var p=bind[foot];Ellipsoid(p+new Vector3(0,-.045f,.065f),new Vector3(.067f,.05f,.13f),Dark,foot);
                Box(p+new Vector3(0,-.075f,.065f),new Vector3(.13f,.015f,.24f),new Color(.45f,.44f,.39f,0),foot);
                for(int i=0;i<4;i++)Box(p+new Vector3(0,-.005f,.05f+i*.018f),new Vector3(.07f,.005f,.006f),new Color(.49f,.47f,.42f,0),foot);
            }
            BuildHair(doctor);
        }
        private void BuildDoctorHead()
        {
            float[] heights={1.53f,1.545f,1.56f,1.58f,1.60f,1.62f,1.64f,1.66f,1.68f,1.70f,1.72f,1.74f,1.76f,1.78f,1.795f};
            float[] widths={.039f,.057f,.069f,.077f,.082f,.091f,.097f,.099f,.098f,.097f,.096f,.091f,.079f,.057f,.008f};
            float[] depths={.049f,.063f,.070f,.074f,.077f,.080f,.083f,.085f,.085f,.084f,.082f,.078f,.070f,.052f,.008f};
            const int sides=64;int start=vertices.Count;var skin=new Color(.69f,.51f,.40f,0);
            for(int r=0;r<heights.Length;r++)for(int i=0;i<=sides;i++)
            {
                float angle=i*Mathf.PI*2/sides,y=heights[r],x=Mathf.Cos(angle)*widths[r],front=Mathf.Max(0,Mathf.Sin(angle)),z=Mathf.Sin(angle)*depths[r]-.005f;
                // Continuous facial surface: connected nose, cheeks, chin and eye sockets.
                float bridge=Mathf.Exp(-x*x/.00043f)*Mathf.Exp(-Mathf.Pow((y-1.645f)/.041f,2))*.034f;
                float sockets=(Mathf.Exp(-Mathf.Pow((x-.037f)/.026f,2))+Mathf.Exp(-Mathf.Pow((x+.037f)/.026f,2)))*Mathf.Exp(-Mathf.Pow((y-1.682f)/.014f,2))*.009f;
                float cheeks=Mathf.Exp(-Mathf.Pow((Mathf.Abs(x)-.061f)/.023f,2))*Mathf.Exp(-Mathf.Pow((y-1.637f)/.024f,2))*.011f;
                float chin=Mathf.Exp(-x*x/.002f)*Mathf.Exp(-Mathf.Pow((y-1.555f)/.018f,2))*.009f;
                z+=Mathf.Pow(front,5)*(bridge-sockets+cheeks+chin);Color color=skin;
                if(y<1.625f&&front>.2f){float stubble=.15f+((i*17+r*23)%11)/11f*.16f;color=Color.Lerp(skin,new Color(.27f,.25f,.23f,0),stubble);}
                Vertex(new Vector3(x,y,z),color,head,head,0);
            }
            for(int r=0;r<heights.Length-1;r++)for(int i=0;i<sides;i++){int n=start+r*(sides+1)+i;Quad(n,n+sides+1,n+1,n+sides+2);}
            foreach(float side in new[]{-1f,1f})
            {
                Ellipsoid(new Vector3(side*.098f,1.66f,-.005f),new Vector3(.016f,.031f,.02f),skin,head);
                Ellipsoid(new Vector3(side*.036f,1.683f,.076f),new Vector3(.024f,.009f,.010f),new Color(.70f,.70f,.65f,0),head);
                Ellipsoid(new Vector3(side*.036f,1.683f,.085f),new Vector3(.009f,.008f,.003f),new Color(.22f,.46f,.64f,0),head);
                Ellipsoid(new Vector3(side*.036f,1.683f,.088f),new Vector3(.0035f,.005f,.0015f),Dark,head);
                Box(new Vector3(side*.037f,1.701f,.078f),new Vector3(.047f,.006f,.008f),new Color(.20f,.18f,.16f,0),head);
                Ellipsoid(new Vector3(side*.014f,1.619f,.103f),new Vector3(.007f,.003f,.003f),new Color(.27f,.19f,.16f,0),head);
            }
            Ellipsoid(new Vector3(0,1.594f,.076f),new Vector3(.031f,.003f,.006f),new Color(.40f,.24f,.21f,0),head);
        }
        private void BuildHair(bool doctor)
        {
            // Short overlapping tapered locks form volume without round bead shapes.
            for(int i=0;i<380;i++)
            {
                float angle=i*2.39996323f,r=Mathf.Sqrt((i+.5f)/380f),x=Mathf.Cos(angle)*r*.099f,z=Mathf.Sin(angle)*r*.09f;
                float y=1.738f+Mathf.Sqrt(1-r*r)*.054f,shade=doctor?(i%5==0?.48f:.15f):.12f;var color=new Color(shade,shade*.94f,shade*.88f,0);
                var p=new Vector3(x,y,z-.008f);var tip=p+new Vector3(Mathf.Sin(i*1.7f)*.009f,.009f+(i%4)*.003f,Mathf.Cos(i*.9f)*.01f);int start=vertices.Count;
                Vertex(p+new Vector3(-.008f,0,-.005f),color,head,head,0);Vertex(p+new Vector3(.008f,0,-.005f),color,head,head,0);Vertex(p+new Vector3(.008f,0,.005f),color,head,head,0);Vertex(p+new Vector3(-.008f,0,.005f),color,head,head,0);Vertex(tip,color,head,head,0);
                foreach(int n in new[]{0,4,1,1,4,2,2,4,3,3,4,0})indices.Add(start+n);
            }
        }
        private void CreateCane()
        {
            cane=new GameObject("Right-hand walking cane").transform;cane.SetParent(RightHand,false);cane.localPosition=new Vector3(0,-.045f,.045f);
            var filter=cane.gameObject.AddComponent<MeshFilter>();filter.sharedMesh=AuthoredEquipment012.Load("Cane012");caneMaterial=new Material(Resources.Load<Material>("EquipmentSurface")??material);cane.gameObject.AddComponent<MeshRenderer>().sharedMaterial=caneMaterial;
        }
        private void Vertex(Vector3 p,Color color,int a,int b,float blendWeight)
        {vertices.Add(p);colors.Add(color);weights.Add(new BoneWeight{boneIndex0=a,weight0=1-blendWeight,boneIndex1=b,weight1=blendWeight});}
        private void Loft(Vector3[] centres,Vector2[] radii,Color color,int a,int b)
        {
            const int sides=20;int start=vertices.Count;for(int r=0;r<centres.Length;r++)for(int i=0;i<=sides;i++){float angle=i*Mathf.PI*2/sides;Vertex(centres[r]+new Vector3(Mathf.Cos(angle)*radii[r].x,0,Mathf.Sin(angle)*radii[r].y),color,a,b,a==b?0:r/(float)(centres.Length-1));}
            for(int r=0;r<centres.Length-1;r++)for(int i=0;i<sides;i++){int n=start+r*(sides+1)+i;Quad(n,n+sides+1,n+1,n+sides+2);} // outward wound
            for(int i=1;i<sides-1;i++){indices.Add(start);indices.Add(start+i);indices.Add(start+i+1);int top=start+(centres.Length-1)*(sides+1);indices.Add(top);indices.Add(top+i+1);indices.Add(top+i);}
        }
        private void ArticulatedLimb(int a,int b,int c,float upper,float joint,float lower,Color color)
        {
            const int sides=20;var centres=new[]{bind[a],Vector3.Lerp(bind[a],bind[b],.28f),Vector3.Lerp(bind[a],bind[b],.82f),bind[b],Vector3.Lerp(bind[b],bind[c],.26f),Vector3.Lerp(bind[b],bind[c],.74f),bind[c]};var radii=new[]{upper,upper*1.04f,joint*1.06f,joint,joint*1.04f,lower*1.3f,lower};int start=vertices.Count;
            for(int ring=0;ring<centres.Length;ring++)for(int i=0;i<=sides;i++){float angle=i*Mathf.PI*2/sides;int first=ring<3?a:b,second=ring<3?b:c;float weight=ring<3?Mathf.InverseLerp(.68f,1,ring/3f)*.5f:ring==3?0:ring==6?1:Mathf.InverseLerp(4,6,ring)*.5f;Vertex(centres[ring]+new Vector3(Mathf.Cos(angle)*radii[ring],0,Mathf.Sin(angle)*radii[ring]*.9f),color,first,second,weight);}
            for(int ring=0;ring<centres.Length-1;ring++)for(int i=0;i<sides;i++){int n=start+ring*(sides+1)+i;Quad(n,n+1,n+sides+1,n+sides+2);}
        }
        private void ShoulderBand(int bone,float x,float y)
        {
            int start=vertices.Count;const int sides=16;for(int i=0;i<=sides;i++){float angle=(i/(float)sides-.5f)*Mathf.PI;foreach(float side in new[]{-1f,1f})Vertex(new Vector3(x+side*.03f,y+Mathf.Cos(angle)*.06f,Mathf.Sin(angle)*.139f),Silver,bone,bone,0);}for(int i=0;i<sides;i++){int n=start+i*2;Quad(n,n+2,n+1,n+3);}
        }
        private void Ellipsoid(Vector3 centre,Vector3 radius,Color color,int bone)
        {const int rings=10,sides=16;int start=vertices.Count;for(int r=0;r<=rings;r++)for(int i=0;i<=sides;i++){float y=r*Mathf.PI/rings,angle=i*Mathf.PI*2/sides;Vertex(centre+Vector3.Scale(new Vector3(Mathf.Sin(y)*Mathf.Cos(angle),Mathf.Cos(y),Mathf.Sin(y)*Mathf.Sin(angle)),radius),color,bone,bone,0);}for(int r=0;r<rings;r++)for(int i=0;i<sides;i++){int n=start+r*(sides+1)+i;Quad(n,n+1,n+sides+1,n+sides+2);}}
        private void Box(Vector3 centre,Vector3 size,Color color,int bone)
        {int start=vertices.Count;int[] face={0,2,1,1,2,3,4,5,6,5,7,6,0,1,4,1,5,4,2,6,3,3,6,7,0,4,2,2,4,6,1,3,5,3,7,5};for(int i=0;i<8;i++)Vertex(centre+Vector3.Scale(new Vector3((i&1)==0?-.5f:.5f,(i&2)==0?-.5f:.5f,(i&4)==0?-.5f:.5f),size),color,bone,bone,0);foreach(int i in face)indices.Add(start+i);}
        private void Quad(int a,int b,int c,int d){indices.Add(a);indices.Add(b);indices.Add(c);indices.Add(c);indices.Add(b);indices.Add(d);}
        public void SetClothColor(Color color)
        {if(appearance!=CharacterAppearance.Explorer||mesh==null)return;var values=mesh.colors;for(int i=0;i<values.Length;i++)if(Mathf.Abs(values[i].r-.22f)<.001f&&Mathf.Abs(values[i].g-.28f)<.001f&&Mathf.Abs(values[i].b-.20f)<.001f){color.a=0;values[i]=color;}mesh.colors=values;}
        public void Animate(float movement,bool running,bool grounded,bool flying,float vertical,bool digging,bool crouched,float dt,float cadenceScale=1)
        {AnimateCharacter(movement,running,grounded,flying,vertical,digging,crouched,dt,cadenceScale);}
        protected virtual void OnDestroy(){ReleaseDistanceMeshes();var caneMesh=cane==null?null:cane.GetComponent<MeshFilter>().sharedMesh;if(Application.isPlaying){if(caneMesh!=null)Destroy(caneMesh);if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);if(caneMaterial!=null)Destroy(caneMaterial);}else{if(caneMesh!=null)DestroyImmediate(caneMesh);if(mesh!=null)DestroyImmediate(mesh);if(material!=null)DestroyImmediate(material);if(caneMaterial!=null)DestroyImmediate(caneMaterial);}}
    }
}
