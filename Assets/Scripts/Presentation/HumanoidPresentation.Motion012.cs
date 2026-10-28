using UnityEngine;
using Aevoria.World;
namespace Aevoria.Presentation
{
    public partial class HumanoidPresentation
    {
        public bool GroundContactEnabled{get;set;}
        public float CaneContactError{get;private set;}
        public float ToolActionProgress{get;set;}
        public Quaternion FirstPersonAim{get;set;}=Quaternion.identity;
        public Vector3 EyePosition=>bones[head].position+transform.up*.013f+transform.forward*.07f;
        private Vector3 leftPlant,rightPlant,canePlant;private bool leftPlanted,rightPlanted,canePlanted;
        private float squatBlend;
        public bool SampleEditableClips{get;set;}=true;
        private AnimationClip[] motionClips;
        private readonly Quaternion[] previousRotations=new Quaternion[64];
        public void ResetMotionPhase(){phase=0;}
        private void EditableMotion(bool running,bool grounded,bool flying,float vertical,bool digging,bool crouched,float dt)
        {
            if(!SampleEditableClips||appearance!=CharacterAppearance.Doctor)return;
            if(motionClips==null)motionClips=Resources.LoadAll<AnimationClip>("Characters/Motion012");
            string name=digging?"Mining":flying?"Flight":!grounded?(vertical>0?"Jump":"Freefall"):crouched?(blend>.05f?"CrouchWalk":"Crouch"):blend>.05f?(running?"Run":"Walk"):"Idle";
            AnimationClip selected=null;foreach(var clip in motionClips)if(clip.name==name){selected=clip;break;}if(selected==null)return;
            for(int i=0;i<bones.Count;i++)previousRotations[i]=bones[i].localRotation;
            selected.SampleAnimation(gameObject,(digging?ToolActionProgress:Mathf.Repeat(phase/(Mathf.PI*2),1))*selected.length);
            float blendPose=1-Mathf.Exp(-dt*18);
            for(int i=0;i<bones.Count;i++)bones[i].localRotation=Quaternion.Slerp(previousRotations[i],bones[i].localRotation,blendPose);
        }
        private float Ground(Vector3 point)
        {
            if(GroundContactEnabled&&Physics.Raycast(point+Vector3.up*.7f,Vector3.down,out var hit,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))return hit.point.y;
            return transform.position.y;
        }
        private void AnimateCharacter(float movement,bool running,bool grounded,bool flying,float vertical,bool digging,bool crouched,float dt,float cadenceScale)
        {
            dt=Mathf.Clamp(dt,0,.05f);bool doctor=appearance==CharacterAppearance.Doctor;
            blend=Mathf.MoveTowards(blend,Mathf.Clamp01(movement),dt*5);
            float speed=doctor?(running?3.3f:1.65f):(running?9:4.5f),strideLength=crouched?(doctor?.65f:.9f):doctor?(running?1.65f:1.18f):(running?3.2f:2.4f);
            phase+=dt*Mathf.PI*2*speed/strideLength*blend*cadenceScale;
            if(grounded&&!wasGrounded&&lastVertical<-1)landing=.28f;landing=Mathf.Max(0,landing-dt);
            squatBlend=Mathf.MoveTowards(squatBlend,crouched?1:0,dt*5);
            AnimationState=digging?"Digging":crouched?(movement>.05f?"Crouch walking":"Crouching"):landing>0?"Landing":flying?"Flying":!grounded?(vertical>0?"Jumping":"Freefall"):movement>.05f?(running?"Running":"Walking"):"Idle";
            for(int i=0;i<bones.Count;i++)pose[i]=Quaternion.identity;
            float cycle=Mathf.Repeat(phase/(Mathf.PI*2),1),gait=phase;
            // A shorter right support interval is paired with a shortened right
            // swing; timing and load compensation are separate from tool motion.
            if(doctor){float warped=cycle<.40f?cycle/.40f*.5f:.5f+(cycle-.40f)/.60f*.5f;gait=warped*Mathf.PI*2;}
            float stride=Mathf.Sin(gait)*(running?42:27)*blend;
            pose[leftThigh]=Quaternion.Euler(stride,0,0);pose[rightThigh]=Quaternion.Euler(-stride*(doctor?.66f:1),0,0);
            pose[leftShin]=Quaternion.Euler(Mathf.Max(0,-Mathf.Cos(gait))*(running?58:30)*blend,0,0);pose[rightShin]=Quaternion.Euler(Mathf.Max(0,Mathf.Cos(gait))*(doctor?20:running?58:30)*blend,0,0);
            // Negative Z opens the left shoulder; positive Z opens the right.
            pose[leftUpper]=Quaternion.Euler(-stride*.65f,0,-4);pose[rightUpper]=Quaternion.Euler(stride*.65f,0,4);
            pose[leftLower]=Quaternion.Euler(-10-(running?28:0),0,0);pose[rightLower]=pose[leftLower];
            pose[chest]=Quaternion.Euler(running?9:doctor?3:Mathf.Sin(Time.time*1.7f)*.5f,0,doctor?2+Mathf.Sin(gait)*2*blend:0);
            if(doctor){pose[rightThigh]*=Quaternion.Euler(3,0,0);pose[rightUpper]=Quaternion.Euler(-8,0,7);pose[rightLower]=Quaternion.Euler(-16,0,0);}
            if(flying){pose[leftUpper]=Quaternion.Euler(-32,0,-15);pose[rightUpper]=Quaternion.Euler(-32,0,15);pose[leftThigh]=Quaternion.Euler(12,0,0);pose[rightThigh]=Quaternion.Euler(18,0,0);pose[leftShin]=pose[rightShin]=Quaternion.Euler(25,0,0);}
            else if(!grounded){float flail=vertical>0?0:Mathf.Sin(Time.time*5)*7;pose[leftUpper]=Quaternion.Euler(-30+flail,0,-18);pose[rightUpper]=Quaternion.Euler(-35-flail,0,18);pose[leftThigh]=Quaternion.Euler(-18,0,0);pose[rightThigh]=Quaternion.Euler(12,0,0);pose[leftShin]=Quaternion.Euler(50,0,0);pose[rightShin]=Quaternion.Euler(28,0,0);}
            if(firstPerson&&doctor&&ToolEquipped&&!digging){pose[leftUpper]=Quaternion.Euler(-40,0,-8);pose[leftLower]=Quaternion.Euler(-55,0,0);}
            if(digging)
            {
                float t=ToolActionProgress,wind=t<.45f?Mathf.SmoothStep(0,1,t/.45f):1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.80f,t));float raise=-10-80*wind,bend=-10-30*wind;
                int upper=doctor?leftUpper:rightUpper,lower=doctor?leftLower:rightLower;
                pose[upper]=Quaternion.Euler(raise,doctor?-7:7,doctor?-18:18);pose[lower]=Quaternion.Euler(bend,0,0);
                pose[chest]=Quaternion.Euler(7+(1-wind)*12,(1-wind)*(doctor?-5:5),doctor?2:0);
            }
            if(squatBlend>0)
            {
                float crawl=Mathf.Sin(gait)*9*blend;
                pose[leftThigh]=Quaternion.Slerp(pose[leftThigh],Quaternion.Euler(-77+crawl,0,-3),squatBlend);
                pose[rightThigh]=Quaternion.Slerp(pose[rightThigh],Quaternion.Euler(-77-crawl,0,3),squatBlend);
                pose[leftShin]=Quaternion.Slerp(pose[leftShin],Quaternion.Euler(128-crawl,0,0),squatBlend);pose[rightShin]=Quaternion.Slerp(pose[rightShin],Quaternion.Euler(128+crawl,0,0),squatBlend);
                pose[leftFoot]=pose[rightFoot]=Quaternion.Euler(-51*squatBlend,0,0);
                pose[chest]=Quaternion.Slerp(pose[chest],Quaternion.Euler(32,0,doctor?2:0),squatBlend);pose[neck]=Quaternion.Euler(-18*squatBlend,0,0);
                // Counter the forward lean at the shoulders. Hands stay ahead of
                // the thighs instead of trailing behind the crouched torso.
                if(!digging){pose[leftUpper]=Quaternion.Slerp(pose[leftUpper],Quaternion.Euler(-48,0,-9),squatBlend);pose[leftLower]=Quaternion.Slerp(pose[leftLower],Quaternion.Euler(-30,0,0),squatBlend);if(!doctor){pose[rightUpper]=Quaternion.Slerp(pose[rightUpper],Quaternion.Euler(-48,0,9),squatBlend);pose[rightLower]=Quaternion.Slerp(pose[rightLower],Quaternion.Euler(-30,0,0),squatBlend);}}
            }
            foreach(int finger in leftFingers)pose[finger]=Quaternion.Euler(ToolEquipped?-38:-5,0,0);
            foreach(int finger in rightFingers)pose[finger]=Quaternion.Euler(-38,0,0);
            float settle=grounded&&!flying?(doctor?.025f:.007f):0;
            if(grounded&&!flying&&GroundContactEnabled)settle+=Mathf.Clamp(transform.position.y-Ground(transform.position),-.04f,.12f);
            float hipDrop=(doctor?.575f:.53f)*squatBlend+settle+Mathf.Sin(landing/.28f*Mathf.PI)*.05f;
            if(doctor&&grounded&&GroundContactEnabled){var support=canePlanted?canePlant:transform.TransformPoint(new Vector3(.30f,0,.05f));hipDrop+=Mathf.Clamp(Ground(transform.position)-Ground(support),0,.12f)*.85f;}
            Vector3 hip=bind[hips]-Vector3.up*hipDrop-Vector3.forward*(.13f*squatBlend);
            if(doctor&&grounded)hip+=new Vector3(Mathf.Sin(gait)*.012f*blend,-Mathf.Max(0,Mathf.Sin(gait))*.018f*blend,0);
            bones[hips].localPosition=Vector3.Lerp(bones[hips].localPosition,hip,1-Mathf.Exp(-dt*14));pose[cape]=Quaternion.Euler(5+blend*12,0,0);
            for(int i=0;i<bones.Count;i++)bones[i].localRotation=Quaternion.Slerp(bones[i].localRotation,pose[i],1-Mathf.Exp(-dt*16));
            EditableMotion(running,grounded,flying,vertical,digging,crouched,dt);
            FingerGrip(leftFingers,ToolEquipped);FingerGrip(rightFingers,doctor||ToolEquipped);
            if(ToolEquipped)ToolHand.localRotation=Quaternion.AngleAxis(doctor?-90:90,Vector3.forward);
            if(grounded&&!flying&&GroundContactEnabled)
            {
                FootContact(leftThigh,leftShin,leftFoot,doctor?Mathf.Repeat(cycle+.5f,1)<.60f:cycle<.38f,ref leftPlant,ref leftPlanted,blend);
                FootContact(rightThigh,rightShin,rightFoot,doctor?cycle<.40f:Mathf.Repeat(cycle+.5f,1)<.38f,ref rightPlant,ref rightPlanted,blend);
            }
            else leftPlanted=rightPlanted=false;
            if(cane!=null)CaneMotion(grounded&&!flying,cycle,blend);
            if(firstPerson&&doctor&&ToolEquipped)
            {
                float wind=digging?Mathf.Sin(Mathf.PI*ToolActionProgress):0;
                Vector3 grip=EyePosition+FirstPersonAim*new Vector3(-.23f,-.25f+wind*.09f,.43f-wind*.08f);
                SolveChain(bones[leftUpper],bones[leftLower],LeftHand,grip,-transform.right-transform.up*.3f);
                LeftHand.rotation=FirstPersonAim*Quaternion.Euler(-60-wind*15,0,0)*Quaternion.AngleAxis(-90,Vector3.forward);
            }
            wasGrounded=grounded;lastVertical=vertical;
        }
        private void FingerGrip(System.Collections.Generic.List<int> fingers,bool gripping)
        {
            foreach(int finger in fingers)
            {
                string name=bones[finger].name;bool thumb=name.Contains("finger 4"),distal=name.EndsWith("distal");
                float curl=gripping?(thumb?(distal?-65:-45):(distal?-95:-85)):-6;
                bones[finger].localRotation=Quaternion.Euler(curl,gripping&&thumb?(name.StartsWith("Left")?-30:30):0,0);
            }
        }
        private void FootContact(int upper,int lower,int ankle,bool stance,ref Vector3 planted,ref bool locked,float movement)
        {
            var current=bones[ankle].position;bool idle=movement<.08f;
            if(idle){current=transform.TransformPoint(new Vector3(bind[ankle].x,0,bind[ankle].z));locked=false;}
            if(stance&&!idle){if(!locked||Vector2.Distance(new Vector2(planted.x,planted.z),new Vector2(transform.position.x,transform.position.z))>.65f){planted=current;planted.y=Ground(current)+.09f;}current=planted;locked=true;}else locked=false;
            current.y=Mathf.Max(current.y,Ground(current)+.09f);SolveChain(bones[upper],bones[lower],bones[ankle],current,transform.forward);
            bones[ankle].rotation=Quaternion.Slerp(bones[ankle].rotation,transform.rotation,.85f);
        }
        private static void SolveChain(Transform root,Transform joint,Transform end,Vector3 target,Vector3 pole)
        {
            float a=Vector3.Distance(root.position,joint.position),b=Vector3.Distance(joint.position,end.position);Vector3 delta=target-root.position;
            if(a<.0001f||b<.0001f||delta.sqrMagnitude<1e-10f||float.IsNaN(delta.sqrMagnitude))return;
            float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(a-b)+.001f,(a+b)*.999f);Vector3 direction=delta.normalized;
            Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(Vector3.forward,direction).normalized;
            float along=(a*a-b*b+distance*distance)/(2*distance),height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));Vector3 knee=root.position+direction*along+bend*height;
            root.rotation=Quaternion.FromToRotation(joint.position-root.position,knee-root.position)*root.rotation;
            joint.rotation=Quaternion.FromToRotation(end.position-joint.position,target-joint.position)*joint.rotation;
        }
        private void CaneMotion(bool supported,float cycle,float movement)
        {
            if(!supported){canePlanted=false;cane.localPosition=new Vector3(0,-.045f,.045f);cane.rotation=transform.rotation*Quaternion.Euler(25,0,0);CaneContactError=0;return;}
            bool stance=cycle<.40f||movement<.08f;float swing=Mathf.InverseLerp(.40f,1,cycle);
            float rise=Mathf.Lerp(.90f,.57f,squatBlend),reach=Mathf.Sqrt(Mathf.Max(0,.9f*.9f-rise*rise));
            var tip=transform.TransformPoint(new Vector3(.31f,0,reach+Mathf.Lerp(-.12f,.24f,Mathf.SmoothStep(0,1,swing))*movement));tip.y=Ground(tip);
            if(stance&&movement>.08f){if(!canePlanted||Vector3.Distance(canePlant,transform.position)>.75f)canePlant=tip;tip=canePlant;canePlanted=true;}else canePlanted=false;
            if(!stance)tip.y+=Mathf.Sin(swing*Mathf.PI)*.07f*movement;
            var gripOffset=new Vector3(0,-.045f,.045f);var wrist=tip+Vector3.up*rise-transform.forward*reach-transform.rotation*gripOffset;
            SolveChain(bones[rightUpper],bones[rightLower],RightHand,wrist,transform.right*.4f-transform.forward*.5f);RightHand.rotation=transform.rotation;
            cane.localPosition=gripOffset;Vector3 down=tip-cane.position;cane.rotation=Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward,down).normalized,-down.normalized);
            CaneContactError=stance?Mathf.Abs(cane.TransformPoint(new Vector3(0,-.9f,0)).y-Ground(tip)):0;
        }
    }
}
