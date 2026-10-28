using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Aevoria.World;
using Aevoria.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
namespace Aevoria.Presentation
{
    public sealed class ExperimentalExplorer : MonoBehaviour
    {
        private sealed class GroundData {public SolidPoint[] Source;public Vector3[] Vertices;public int[] Indices;}
        private AevoriaBootstrap app;private GameObject player,body,groundObject,firstTool;private CharacterController controller;private Camera camera;
        private Mesh groundMesh;private Material material;private readonly List<Mesh> meshes=new List<Mesh>();private readonly Mesh[] tools=new Mesh[6];private Transform rightArm;private MeshFilter held,firstHeld;
        private Task<GroundData> groundJob;private Vector2 colliderCenter=new Vector2(float.MaxValue,float.MaxValue);private int groundRevision=-1;
        private float vertical,phase,actionUntil,nextAction;private bool manualCursor,pendingSpawn;private int stoneEffort;private Vector3 lastImpact;
        private ParticleSystem debris;private float walkBlend;
        private HumanoidPresentation soldier;private ViewModeTransition viewTransition;private bool running;
        public HumanoidPresentation Character=>soldier;
        private CharacterDialogue dialogue;
        private ExcavationTarget targeting;private Light undergroundLamp;
        private Vector3 flightVelocity,fallVelocity;private float lastSpaceTap=float.NegativeInfinity;
        private GroundData installedGround;private Vector2 installedCenter;
        private Func<int,int,int,bool> groundOwned;
        private readonly List<double> flightCrossings=new List<double>(64);
        private static readonly Vector2[] footprint={Vector2.zero,Vector2.right,Vector2.left,Vector2.up,Vector2.down,new Vector2(.7071068f,.7071068f),new Vector2(-.7071068f,.7071068f),new Vector2(.7071068f,-.7071068f),new Vector2(-.7071068f,-.7071068f)};
        public float VerticalSpeed=>vertical;
        public bool Grounded=>controller!=null&&controller.isGrounded;
        public bool Falling=>!Flying&&vertical<-.5f&&!controller.isGrounded;
        public bool Flying=>app?.Living?.Traveler.Flying==true;
        public bool InventoryOpen {get;private set;}
        public bool AcceptInput {get;set;}=true;
        public bool AimingAvailable=>!GameInput.Blocked&&app?.Living?.Traveler.Exploring==true&&!InventoryOpen&&!manualCursor&&app.Menu.Page==ApplicationPage.World;
        public Ray AimRay=>new Ray(player.transform.position+Vector3.up*(controller.height-.15f),Quaternion.Euler((float)app.Living.Traveler.Pitch,(float)app.Living.Traveler.Yaw,0)*Vector3.forward);
        public bool IsTerrainCollider(Collider hit)=>hit!=null&&(hit.gameObject==groundObject||app.Excavations.OwnsCollider(hit));
        public ResourceKind DigMaterial(RaycastHit hit)
        {double x=(double)hit.point.x+WorldOrigin.Offset.x,z=(double)hit.point.z+WorldOrigin.Offset.z;return app.Terrain.Geography.Materials.GroundMaterialAt(x,hit.point.y+WorldOrigin.Offset.y,z,hit.normal.y);}
        public void CloseInventory()=>InventoryOpen=false;
        public bool Ready=>!app.Living.Traveler.Exploring||!pendingSpawn&&groundJob==null&&groundRevision==app.Excavations.InstalledRevision&&app.Excavations.Ready;
        private string message="";private float messageUntil;public string Message {get=>message;private set{message=value;messageUntil=Time.unscaledTime+3;}}
        private static readonly string[] ToolNames={"Hands","Shovel","Pickaxe","Axe","Stick","Test crate"};
        private static readonly Key[] ToolKeys={Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6};
        public Vector3 Position=>player==null?Vector3.zero:WorldOrigin.Geographic(player.transform);
        public SolidPoint GeographicPosition=>player==null?new SolidPoint(0,0,0):new SolidPoint((double)player.transform.position.x+WorldOrigin.Offset.x,player.transform.position.y,(double)player.transform.position.z+WorldOrigin.Offset.z);
        public void Initialize(AevoriaBootstrap value)
        {
            if(app?.Excavations!=null)app.Excavations.GroundOwnershipChanged-=RefreshGroundOwnership;app=value;app.Excavations.GroundOwnershipChanged+=RefreshGroundOwnership;camera=Camera.main;colliderCenter=new Vector2(float.MaxValue,float.MaxValue);groundRevision=-1;InventoryOpen=false;pendingSpawn=false;manualCursor=false;phase=vertical=walkBlend=0;lastSpaceTap=float.NegativeInfinity;flightVelocity=fallVelocity=Vector3.zero;installedGround=null;
            if(groundJob!=null)Observe(groundJob);groundJob=null;groundOwned=app.Excavations.OwnsChunk;
            if(groundObject!=null)groundObject.GetComponent<MeshCollider>().sharedMesh=null;if(groundMesh!=null)Destroy(groundMesh);groundMesh=null;
            if(player==null)CreatePlayer();else SetAppearance(app.Living.Traveler.CharacterPreset);player.SetActive(false);if(groundObject!=null)groundObject.SetActive(false);
            dialogue=gameObject.AddComponent<CharacterDialogue>();dialogue.Initialize(app,this);
            if(targeting==null)targeting=gameObject.AddComponent<ExcavationTarget>();targeting.Initialize(app,this);
            app.ToggleExploration=Toggle;
            if(app.Living.Traveler.Exploring)EnterAt(new GeoPoint(app.Living.Traveler.X,app.Living.Traveler.Z),true);
        }
        private static void Observe(Task task)=>task.ContinueWith(t=>{var ignored=t.Exception;},TaskContinuationOptions.OnlyOnFaulted);
        private Mesh Own(WorldShapes shape,string name){var mesh=shape.Build(name);meshes.Add(mesh);return mesh;}
        private GameObject Part(string name,Transform parent,Vector3 position,Mesh mesh)
        {var obj=new GameObject(name);obj.layer=2;obj.transform.SetParent(parent,false);obj.transform.localPosition=position;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=material;return obj;}
        private void CreatePlayer()
        {
            material=new Material(Resources.Load<Material>("EquipmentSurface"));
            player=new GameObject("Experimental traveler · 1.8 metres");player.layer=2;var lamp=new GameObject("Traveler hooded oil lamp");lamp.transform.SetParent(player.transform,false);lamp.transform.localPosition=new Vector3(.27f,1.35f,.15f);undergroundLamp=lamp.AddComponent<Light>();undergroundLamp.type=LightType.Spot;undergroundLamp.color=new Color(1,.76f,.43f);undergroundLamp.range=16;undergroundLamp.spotAngle=110;undergroundLamp.innerSpotAngle=65;undergroundLamp.intensity=0;undergroundLamp.shadows=LightShadows.Soft;undergroundLamp.shadowBias=.025f;undergroundLamp.shadowNormalBias=.1f;controller=player.AddComponent<CharacterController>();controller.height=1.8f;controller.radius=.24f;controller.center=Vector3.up*.9f;controller.stepOffset=.25f;controller.slopeLimit=48;
            body=new GameObject("Traveler appearance");body.layer=2;body.transform.SetParent(player.transform,false);soldier=body.AddComponent<HumanoidPresentation>();soldier.Initialize(CharacterProfiles.Appearance(app.Living.Traveler.CharacterPreset));soldier.GroundContactEnabled=true;rightArm=soldier.ToolHand;var shape=new WorldShapes();
            for(int i=1;i<6;i++){tools[i]=AuthoredEquipment012.LoadTool(i);meshes.Add(tools[i]);}
            held=Part("Equipped hand tool",rightArm,new Vector3(0,-.06f,.025f),tools[1]).GetComponent<MeshFilter>();held.transform.localRotation=EquipmentGrip012.Rotation(app.Living.Traveler.Tool,IsDoctor,false,false,0);
            firstTool=new GameObject("First-person equipped tool");firstTool.transform.SetParent(camera.transform,false);firstTool.transform.localPosition=new Vector3(.28f,-.42f,.62f);firstTool.transform.localRotation=Quaternion.Euler(18,0,-20);firstHeld=firstTool.AddComponent<MeshFilter>();firstTool.AddComponent<MeshRenderer>().sharedMaterial=material;firstTool.SetActive(false);
            var particles=new GameObject("Bounded tool debris");particles.transform.SetParent(transform,false);debris=particles.AddComponent<ParticleSystem>();debris.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);var main=debris.main;main.loop=false;main.maxParticles=96;main.startLifetime=.5f;main.startSize=.045f;main.startSpeed=2;main.gravityModifier=1.2f;main.simulationSpace=ParticleSystemSimulationSpace.World;var emission=debris.emission;emission.enabled=false;debris.GetComponent<ParticleSystemRenderer>().sharedMaterial=Resources.Load<Material>("WorldRain");debris.useAutoRandomSeed=false;debris.randomSeed=913;
            groundObject=new GameObject("Bounded exploration collision");groundObject.transform.SetParent(transform,false);groundObject.AddComponent<MeshCollider>();
        }
        public void Toggle()
        {
            var old=camera.transform.position;var origin=WorldOrigin.Offset;var position=new SolidPoint((double)old.x+origin.x,old.y,(double)old.z+origin.z);var rotation=camera.transform.rotation;var s=app.Living.Traveler;
            if(s.Exploring)Exit();else EnterAt(new GeoPoint(app.Controller.Focus.x,app.Controller.Focus.z));
            if(viewTransition==null)viewTransition=gameObject.AddComponent<ViewModeTransition>();viewTransition.Begin(camera,position,rotation);
        }
        public bool SetAppearance(string id)
        {
            if(!CharacterProfiles.Valid(id))return false;
            var s=app.Living.Traveler;s.CharacterPreset=id;if(soldier!=null&&soldier.Appearance==CharacterProfiles.Appearance(id))return true;
            var next=new GameObject("Traveler appearance · "+id);next.layer=2;next.transform.SetParent(player.transform,false);var replacement=next.AddComponent<HumanoidPresentation>();replacement.Initialize(CharacterProfiles.Appearance(id));
            if(held!=null){held.transform.SetParent(replacement.ToolHand,false);held.transform.localPosition=new Vector3(0,-.06f,.025f);held.transform.localRotation=EquipmentGrip012.Rotation(app.Living.Traveler.Tool,IsDoctor,false,false,0);}
            var old=body;body=next;soldier=replacement;soldier.GroundContactEnabled=true;rightArm=replacement.ToolHand;SetStance(app.Living.Traveler.Crouched);if(old!=null){old.SetActive(false);Destroy(old);}return true;
        }
        public bool UnlockDoctor(string phrase)
        {
            if(!CharacterProfiles.UnlocksDoctor(phrase))return false;var settings=app.Settings.Copy();settings.DoctorUnlocked=true;app.ApplySettings(settings);Message="A Mysterious Doctor Has Appeared";return true;
        }
        private bool SpawnClear(GeoPoint p)
        {
            foreach(var o in app.Living.Objects)if(Math.Abs(o.X-p.X)<1.5&&Math.Abs(o.Z-p.Y)<1.5)return false;
            int nx=(int)Math.Floor(p.X/32),nz=(int)Math.Floor(p.Y/32);
            for(int z=nz-1;z<=nz+1;z++)for(int x=nx-1;x<=nx+1;x++)
                if(app.Living.TryNatural(x,z,out var s)&&(s.Kind==NaturalKind.Boulder||s.Kind==NaturalKind.Log)&&app.Living.Available(s.Id,s.Quantity,app.Clock.State.Tick,false)>0&&Math.Pow(s.Position.X-p.X,2)+Math.Pow(s.Position.Y-p.Y,2)<Math.Pow(s.Kind==NaturalKind.Boulder?s.Scale+1:3,2))return false;
            int tx=(int)Math.Floor(p.X/16),tz=(int)Math.Floor(p.Y/16);
            for(int z=tz-1;z<=tz+1;z++)for(int x=tx-1;x<=tx+1;x++)if(VegetationDistribution.TryTree(app.Terrain.Geography,x,z,out var tree,true)&&Math.Pow(tree.East-p.X,2)+Math.Pow(tree.North-p.Y,2)<1.5&&!app.Living.TreeDepleted(tree.East,tree.North,app.Clock.State.Tick))return false;
            return true;
        }
        public bool EnterAt(GeoPoint requested,bool restore=false)
        {
            var geo=app.Terrain.Geography;GeoPoint selected=requested;bool found=restore&&geo.Configuration.Contains(requested)&&ClearSavedPose(requested,app.Living.Traveler.Y);
            for(int ring=0;ring<20&&!found;ring++)for(int direction=0;direction<8&&!found;direction++)
            {double angle=direction*Math.PI/4;var p=new GeoPoint(requested.X+Math.Cos(angle)*ring*24,requested.Y+Math.Sin(angle)*ring*24);if(!geo.Configuration.Contains(p))continue;float h=geo.ElevationAt(p.X,p.Y);if(h<geo.SeaLevel+4||app.Waters.Layout?.LakeAt(p.X,p.Y,out _)==true||geo.Hydrology.IsRiverWater(p.X,p.Y,h))continue;float slope=Math.Abs(geo.ElevationAt(p.X+3,p.Y)-geo.ElevationAt(p.X-3,p.Y))/6;if(slope>.6||Math.Abs(geo.ElevationAt(p.X,p.Y+3)-geo.ElevationAt(p.X,p.Y-3))/6>.6||!SpawnClear(p)||app.Living.SolidDensity(p.X,LocalSolidField.RenderedGround(geo,p.X,p.Y)-.15,p.Y)<0)continue;selected=p;found=true;}
            if(!found){Message="No safe dry ground near the selected focus; choose a different location.";return false;}
            var state=app.Living.Traveler;double height=LocalSolidField.RenderedGround(geo,selected.X,selected.Y);if(!restore)state.Flying=false;flightVelocity=Vector3.zero;
            WorldOrigin.Follow(new Vector3((float)selected.X,0,(float)selected.Y));controller.enabled=false;player.transform.position=new Vector3((float)(selected.X-WorldOrigin.Offset.x),restore&&Math.Abs(selected.X-requested.X)+Math.Abs(selected.Y-requested.Y)<1?(float)state.Y:(float)height+.2f,(float)(selected.Y-WorldOrigin.Offset.z));player.transform.rotation=Quaternion.Euler(0,(float)state.Yaw,0);player.SetActive(true);groundObject.SetActive(true);state.Exploring=true;state.HasPosition=true;if(!restore){state.Yaw=camera.transform.eulerAngles.y;state.Pitch=12;}
            controller.height=state.Crouched?CrouchHeight:StandingHeight;controller.center=Vector3.up*(controller.height*.5f);body.transform.localScale=Vector3.one;
            vertical=restore?(float)state.VelocityY:0;fallVelocity=restore&&!Flying?new Vector3((float)state.VelocityX,0,(float)state.VelocityZ):Vector3.zero;if(restore&&Flying)flightVelocity=new Vector3((float)state.VelocityX,(float)state.VelocityY,(float)state.VelocityZ);pendingSpawn=true;colliderCenter=new Vector2(float.MaxValue,float.MaxValue);app.Controller.enabled=false;app.Controller.TrackFocus(Position);SavePosition();app.Excavations.RequestNearTraveler();Message="";return true;
        }
        public void Exit(){SavePosition();app.Living.Traveler.Exploring=false;InventoryOpen=false;player.SetActive(false);groundObject.SetActive(false);firstTool.SetActive(false);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;app.Controller.SetView(new GeoPoint(app.Living.Traveler.X,app.Living.Traveler.Z),(float)app.Living.Traveler.Yaw,45,65);app.Controller.enabled=app.Menu.Page==ApplicationPage.World;}
        public void ReturnToPlayer(){var p=app.Living.Traveler;if(p.HasPosition)app.Controller.SetView(new GeoPoint(p.X,p.Z),(float)p.Yaw,45,80);}
        public void SetFirstPerson(bool enabled){app.Living.Traveler.FirstPerson=enabled;app.Living.Traveler.ReverseThirdPerson=false;}
        public void CyclePerspective(){var s=app.Living.Traveler;if(s.FirstPerson){s.FirstPerson=false;s.ReverseThirdPerson=false;}else if(!s.ReverseThirdPerson)s.ReverseThirdPerson=true;else{s.FirstPerson=true;s.ReverseThirdPerson=false;}}
        private void Update()
        {
            if(app?.Living==null)return;var state=app.Living.Traveler;
            if(groundJob!=null&&groundJob.IsCompleted)
            {
                if(groundJob.IsFaulted)Debug.LogException(groundJob.Exception);
                else{installedGround=groundJob.Result;installedCenter=colliderCenter;PublishGround();}
                groundJob=null;
            }
            if(pendingSpawn&&groundMesh!=null&&groundJob==null&&app.Excavations.Ready){pendingSpawn=false;controller.enabled=state.Exploring;}
            if(!state.Exploring)return;app.Controller.TrackFocus(Position);
            var center=new Vector2(Mathf.Floor(Position.x/16)*16,Mathf.Floor(Position.z/16)*16);
            bool needsSupport=pendingSpawn||player.transform.position.y-LocalSolidField.RenderedGround(app.Terrain.Geography,app.Living.Traveler.X,app.Living.Traveler.Z)<Mathf.Max(128,Mathf.Abs(vertical)*2);
            if(needsSupport&&groundJob==null&&(Vector2.Distance(center,colliderCenter)>12||groundRevision!=app.Excavations.InstalledRevision))
            {
                colliderCenter=center;groundRevision=app.Excavations.InstalledRevision;var geo=app.Terrain.Geography;
                groundJob=Task.Run(()=>GenerateGround(geo,center));
            }
            bool playing=app.Menu.Page==ApplicationPage.World;var keyboard=Keyboard.current;
            if(playing&&keyboard!=null&&AcceptInput&&Application.isFocused&&!GameInput.Blocked&&!Aevoria.UI.DeveloperHud.IsEditingText){if(GameInput.Pressed(GameAction.Inventory)&&app.Navigation?.Open!=true)InventoryOpen=!InventoryOpen;if(!Aevoria.UI.DeveloperHud.IsEditingText&&GameInput.Pressed(GameAction.Perspective))CyclePerspective();if(GameInput.Pressed(GameAction.Cursor))manualCursor=!manualCursor;for(int i=0;i<6;i++)if(GameInput.Pressed((GameAction)((int)GameAction.Tool1+i)))state.Tool=(ToolKind)i;}
            bool ui=GameInput.Blocked||Aevoria.UI.DeveloperHud.IsEditingText||InventoryOpen||app.Navigation?.Open==true||!playing||manualCursor||!Application.isFocused||!AcceptInput;
            Cursor.lockState=ui?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=ui;
            if(ui||pendingSpawn||keyboard==null){if(AcceptInput)walkBlend=Mathf.MoveTowards(walkBlend,0,Time.unscaledDeltaTime*5);return;}
            var mouse=Mouse.current;var delta=mouse?.delta.ReadValue()??Vector2.zero;state.Yaw+=delta.x*.13;state.Pitch=Math.Max(-85,Math.Min(85,state.Pitch-delta.y*.13));
            float wheel=GameInput.WheelNotches();if(Mathf.Abs(wheel)>.01f){if(Flying){var settings=app.Settings.Copy();settings.FlightSpeed=Mathf.Clamp(settings.FlightSpeed*Mathf.Pow(1.3f,wheel),.5f,5000);app.ApplySettings(settings);Message=UnitFormatting.Speed(settings.FlightSpeed,settings.Units);}else state.Tool=(ToolKind)(((int)state.Tool-(int)Mathf.Sign(wheel)+6)%6);}
            float dt=Mathf.Min(Time.unscaledDeltaTime,.05f);bool crouch=GameInput.Held(GameAction.Crouch),sprint=GameInput.Held(GameAction.Sprint);
            Vector2 input=new Vector2((GameInput.Held(GameAction.Right)?1:0)-(GameInput.Held(GameAction.Left)?1:0),(GameInput.Held(GameAction.Forward)?1:0)-(GameInput.Held(GameAction.Backward)?1:0));input=Vector2.ClampMagnitude(input,1);
            if(GameInput.Pressed(GameAction.Jump))RegisterSpaceTap(Time.unscaledTime);
            if(Flying)StepFlight(input,GameInput.Held(GameAction.Jump),crouch,sprint,dt);
            else StepMovement(input,sprint,crouch,GameInput.Pressed(GameAction.Jump),dt);
            if(GameInput.Held(GameAction.Primary)&&Time.unscaledTime>=nextAction){nextAction=Time.unscaledTime+.32f;Interact(false);}if(GameInput.Pressed(GameAction.Secondary))Interact(true);SavePosition();
        }
        public void StepMovement(Vector2 input,bool sprint,bool crouch,bool jump,float dt)
        {
            if(Flying)return;
            if(!controller.enabled||pendingSpawn||app?.Living==null||!app.Living.Traveler.Exploring)return;
            dt=Mathf.Clamp(dt,0,.05f);var state=app.Living.Traveler;
            if(!crouch&&controller.height<StandingHeight-.1f&&Physics.CheckCapsule(player.transform.position+Vector3.up*.3f,player.transform.position+Vector3.up*(StandingHeight-.25f),.22f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))crouch=true;
            state.Crouched=crouch;
            SetStance(crouch);body.transform.localScale=Vector3.one;
            input=Vector2.ClampMagnitude(input,1);
            running=sprint;
            Vector3 motion=Quaternion.Euler(0,(float)state.Yaw,0)*new Vector3(input.x,0,input.y)*(crouch?(IsDoctor?.8f:2f):sprint?(IsDoctor?3.3f:9f):(IsDoctor?1.65f:4.5f));var next=WorldOrigin.Geographic(player.transform.position+motion*dt);var nextPoint=new GeoPoint(next.x,next.z);float groundHeight=app.Terrain.Geography.ElevationAt(next.x,next.z);bool underground=player.transform.position.y<groundHeight-2.4f;if(!app.Terrain.Geography.Configuration.Contains(nextPoint)||!underground&&(groundHeight<app.Terrain.Geography.SeaLevel+.5f||app.Waters.Layout?.LakeAt(next.x,next.z,out _)==true)){motion=Vector3.zero;Message="Deep water and the world boundary are outside prototype walking.";}if(controller.isGrounded&&vertical<0){vertical=-2;fallVelocity=Vector3.zero;}if(controller.isGrounded&&jump)vertical=5.2f;vertical=Mathf.Max(-1800,vertical-18*dt);var beforeMove=player.transform.position;MoveWalking(motion+fallVelocity+Vector3.up*vertical,dt);var travelled=player.transform.position-beforeMove;float horizontal=new Vector2(travelled.x,travelled.z).magnitude;walkBlend=Mathf.MoveTowards(walkBlend,Mathf.Clamp01(horizontal/Mathf.Max(.001f,dt)/(sprint?(IsDoctor?3.3f:9f):(IsDoctor?1.65f:4.5f))),dt*5);fallVelocity*=Mathf.Exp(-.35f*dt);phase+=horizontal*(Mathf.PI*2/(sprint?3.2f:2.4f));player.transform.rotation=Quaternion.Euler(0,(float)state.Yaw,0);
            SavePosition();
        }
        private void MoveWalking(Vector3 velocity,float dt)
        {
            var config=app.Terrain.Geography.Configuration;var position=player.transform.position;var next=position+velocity*dt;
            float east=(float)(config.WidthMeters*.5)-1,north=(float)(config.DepthMeters*.5)-1;
            float x=Mathf.Clamp(next.x,-east-WorldOrigin.Offset.x,east-WorldOrigin.Offset.x),z=Mathf.Clamp(next.z,-north-WorldOrigin.Offset.z,north-WorldOrigin.Offset.z);
            if(x!=next.x)fallVelocity.x=0;if(z!=next.z)fallVelocity.z=0;next.x=x;next.z=z;
            float altitude=Mathf.Clamp(next.y,-(float)LivingWorldState.MaximumTravelerAltitude+2,(float)LivingWorldState.MaximumTravelerAltitude-2);
            if(altitude!=next.y)vertical=0;next.y=altitude;
            double geographicX=(double)next.x+WorldOrigin.Offset.x,geographicZ=(double)next.z+WorldOrigin.Offset.z;
            if(next.y<LocalSolidField.RenderedGround(app.Terrain.Geography,geographicX,geographicZ)-2.4&&!app.Excavations.TryInstalledDensity(geographicX,next.y+.9,geographicZ,out _)){Message="Preparing the next underground volume";return;}
            controller.Move(next-position);
        }
        public bool RegisterSpaceTap(float time)
        {
            bool twice=time>=lastSpaceTap&&time-lastSpaceTap<=app.Settings.DoubleTapWindow;lastSpaceTap=twice?float.NegativeInfinity:time;
            if(twice)return SetFlight(!Flying);return false;
        }
        public bool SetFlight(bool enabled)
        {
            if(app?.Living==null||!app.Living.Traveler.Exploring||pendingSpawn)return false;
            if(enabled==Flying)return true;
            // Changing propulsion must never invoke spawn/recovery. The live
            // capsule keeps its pose and carries its powered momentum into gravity.
            var s=app.Living.Traveler;s.Flying=enabled;s.Crouched=false;
            if(enabled){flightVelocity=new Vector3(fallVelocity.x,vertical,fallVelocity.z);fallVelocity=Vector3.zero;vertical=0;}
            else{vertical=flightVelocity.y;fallVelocity=new Vector3(flightVelocity.x,0,flightVelocity.z);flightVelocity=Vector3.zero;}
            Message=enabled?"Flight":"Freefall";SavePosition();return true;
        }
        public void StepFlight(Vector2 input,bool ascend,bool descend,bool accelerate,float dt)
        {
            if(!Flying||pendingSpawn||!controller.enabled)return;dt=Mathf.Clamp(dt,0,.05f);var state=app.Living.Traveler;
            var direction=Quaternion.Euler(0,(float)state.Yaw,0)*new Vector3(input.x,(ascend?1:0)-(descend?1:0),input.y);
            direction=Vector3.ClampMagnitude(direction,1);flightVelocity=Vector3.Lerp(flightVelocity,direction*app.Settings.FlightSpeed*(accelerate?3:1),1-Mathf.Exp(-8*dt));
            var next=player.transform.position+flightVelocity*dt;var geo=app.Terrain.Geography;
            float edgeX=(float)(geo.Configuration.WidthMeters*.5)-1,edgeZ=(float)(geo.Configuration.DepthMeters*.5)-1;next.x=Mathf.Clamp(next.x,-edgeX-WorldOrigin.Offset.x,edgeX-WorldOrigin.Offset.x);next.z=Mathf.Clamp(next.z,-edgeZ-WorldOrigin.Offset.z,edgeZ-WorldOrigin.Offset.z);
            float limitedY=Mathf.Min(next.y,(float)LivingWorldState.MaximumTravelerAltitude-2);if(limitedY!=next.y)flightVelocity.y=0;next.y=limitedY;
            state.Crouched=false;SetStance(false);body.transform.localScale=Vector3.one;var requested=next-player.transform.position;var constrained=ConstrainFlight(requested);
            var collisions=controller.Move(constrained);if(constrained!=requested||collisions!=CollisionFlags.None)flightVelocity=Vector3.zero;
            player.transform.rotation=Quaternion.Euler(0,(float)state.Yaw,0);walkBlend=0;vertical=0;SavePosition();
        }
        private Vector3 ConstrainFlight(Vector3 delta)
        {
            var start=player.transform.position;var geo=app.Terrain.Geography;
            double startX=(double)start.x+WorldOrigin.Offset.x,startZ=(double)start.z+WorldOrigin.Offset.z;
            if(start.y<LocalSolidField.RenderedGround(geo,startX,startZ)-2.4)return ConstrainUndergroundFlight(delta);
            if(Mathf.Min(start.y,start.y+delta.y)>GenerationSettings.MaximumMountainRelief+2500)return delta;
            double earliest=1,cell=geo.Configuration.CellSizeMeters,radius=Math.Max(.02,controller.radius-controller.skinWidth-.02);
            void Crossings(double value,double change)
            {
                if(Math.Abs(change)<1e-10)return;double end=value+change;
                long first=(long)Math.Floor(Math.Min(value,end)/cell)+1,last=(long)Math.Ceiling(Math.Max(value,end)/cell)-1;
                for(long k=first;k<=last;k++){double t=(k*cell-value)/change;if(t>0&&t<earliest)flightCrossings.Add(t);}
            }
            foreach(var direction in footprint)
            {
                double ox=direction.x*radius,oz=direction.y*radius,offsetY=controller.radius-Math.Sqrt(Math.Max(0,controller.radius*controller.radius-ox*ox-oz*oz));
                double x=(double)start.x+WorldOrigin.Offset.x+ox,z=(double)start.z+WorldOrigin.Offset.z+oz;
                double Clearance(double t)
                {
                    double e=x+delta.x*t,n=z+delta.z*t,ground=Math.Max(geo.SeaLevel,LocalSolidField.RenderedGround(geo,e,n));
                    if(app.Excavations.TryInstalledDensity(e,ground-.05,n,out double solid)&&solid<0)return 100000;
                    return start.y+delta.y*t+offsetY-ground-.015;
                }
                flightCrossings.Clear();flightCrossings.Add(earliest);Crossings(x,delta.x);Crossings(z,delta.z);Crossings(x+z,delta.x+delta.z);flightCrossings.Sort();
                double previous=0,clearance=Clearance(0);
                // Native collision owns the capsule. This conservative sweep
                // covers the next heightfield triangles before a fast traveler
                // can outrun the bounded support collider. It never lifts the
                // body onto a cliff merely because its next X/Z lies inside it.
                if(clearance<0){if(Clearance(earliest)>clearance)continue;return Vector3.zero;}
                foreach(double t in flightCrossings)
                {
                    if(Clearance(t)<0)
                    {
                        double low=previous,high=t;for(int j=0;j<16;j++){double middle=(low+high)*.5;if(Clearance(middle)>=0)low=middle;else high=middle;}
                        earliest=Math.Min(earliest,low);break;
                    }
                    previous=t;
                }
            }
            var constrained=delta*(float)earliest;float length=constrained.magnitude;
            if(length>.0001f&&Physics.CapsuleCast(start+Vector3.up*controller.radius,start+Vector3.up*(controller.height-controller.radius),Mathf.Max(.05f,controller.radius-controller.skinWidth-.02f),constrained/length,out var hit,length,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))constrained=constrained/length*Mathf.Max(0,hit.distance-.02f);
            return constrained;
        }
        private Vector3 ConstrainUndergroundFlight(Vector3 delta)
        {
            double length=delta.magnitude;if(length<1e-8)return delta;var start=player.transform.position;double safe=0,t=0;
            double radius=Math.Max(.05,controller.radius-controller.skinWidth*.5);
            for(int iteration=0;iteration<256;iteration++)
            {
                double clearance=double.MaxValue;bool loaded=true;
                for(int sample=0;sample<3;sample++)
                {
                    double height=sample==0?controller.radius:sample==1?controller.height*.5:controller.height-controller.radius;
                    double x=(double)start.x+WorldOrigin.Offset.x+delta.x*t,z=(double)start.z+WorldOrigin.Offset.z+delta.z*t,y=start.y+delta.y*t+height;
                    if(!app.Excavations.TryInstalledDensity(x,y,z,out double density)){loaded=false;break;}clearance=Math.Min(clearance,-density-radius);
                }
                if(!loaded){Message="Preparing the next underground volume";return delta*(float)safe;}
                if(clearance<.003)return delta*(float)safe;if(t>=1)return delta;safe=t;t=Math.Min(1,t+Math.Max(.001,clearance*.7)/length);
            }
            return delta*(float)safe;
        }
        private bool IsDoctor=>app?.Living?.Traveler.CharacterPreset==CharacterProfiles.Doctor;
        private float StandingHeight=>IsDoctor?1.91f:1.8f;
        private float CrouchHeight=>IsDoctor?1.30f:1.23f;
        private void SetStance(bool crouched)
        {
            // Powered flight uses a collision capsule, never the walking
            // controller's automatic stair and slope climbing.
            controller.stepOffset=Flying?0:.25f;controller.slopeLimit=Flying?0:48;
            float height=crouched?CrouchHeight:StandingHeight;
            if(Mathf.Abs(controller.height-height)<.001f)return;
            float next=Mathf.MoveTowards(controller.height,height,Mathf.Clamp(Time.unscaledDeltaTime,1/240f,.05f)*3);controller.height=next;controller.center=Vector3.up*(next*.5f);
        }
        private void MaskOwnedGround(GroundData data,Vector2 center)
        {
            var vertices=new List<SolidPoint>();var indices=new List<int>();var source=data.Source;
            for(int z=0;z<64;z++)for(int x=0;x<64;x++)
            {
                int a=z*65+x;HeightfieldSupportClipper.AppendOutside(source[a],source[a+65],source[a+1],groundOwned,vertices,indices);
                HeightfieldSupportClipper.AppendOutside(source[a+1],source[a+65],source[a+66],groundOwned,vertices,indices);
            }
            data.Vertices=new Vector3[vertices.Count];for(int i=0;i<vertices.Count;i++){var p=vertices[i];data.Vertices[i]=new Vector3((float)(p.X-center.x),(float)p.Y,(float)(p.Z-center.y));}data.Indices=indices.ToArray();
        }
        private void PublishGround()
        {
            MaskOwnedGround(installedGround,installedCenter);var mesh=new Mesh{name="Near-field terrain collision",indexFormat=IndexFormat.UInt32};mesh.vertices=installedGround.Vertices;mesh.triangles=installedGround.Indices;mesh.RecalculateBounds();
            groundObject.transform.position=WorldOrigin.Relative(new Vector3(installedCenter.x,0,installedCenter.y));groundObject.GetComponent<MeshCollider>().sharedMesh=installedGround.Indices.Length>0?mesh:null;
            if(groundMesh!=null)Destroy(groundMesh);groundMesh=mesh;groundRevision=app.Excavations.InstalledRevision;
        }
        private void RefreshGroundOwnership()
        {
            if(installedGround==null||groundMesh==null)return;PublishGround();
        }
        private static GroundData GenerateGround(ProceduralGeography geo,Vector2 center)
        {
            return new GroundData{Source=GroundContactPatch.Sample(geo,center.x,center.y)};
        }
        private bool ClearSavedPose(GeoPoint point,double feet)
        {
            foreach(double y in new[]{.28,.9,1.55})foreach(var offset in new[]{Vector2.zero,Vector2.right,Vector2.left,Vector2.up,Vector2.down})if(app.Living.SolidDensity(point.X+offset.x*.18,feet+y,point.Y+offset.y*.18)>.05)return false;return true;
        }
        private void SavePosition(){if(player==null||app?.Living==null)return;var p=player.transform.position;var s=app.Living.Traveler;s.X=(double)p.x+WorldOrigin.Offset.x;s.Y=p.y;s.Z=(double)p.z+WorldOrigin.Offset.z;WorldOrigin.Follow(Position);var velocity=Flying?flightVelocity:new Vector3(fallVelocity.x,vertical,fallVelocity.z);s.Airborne=!controller.isGrounded;s.VelocityX=velocity.x;s.VelocityY=velocity.y;s.VelocityZ=velocity.z;}
        public bool DigAt(Vector3 point,ResourceKind kind)=>DigAt(new SolidPoint(point.x,point.y,point.z),kind);
        public bool DigAt(SolidPoint point,ResourceKind kind)
        {bool ok=app.Living.Dig(point.X,point.Y,point.Z,kind,out string reason);Message=ok?"Solid excavation queued · one material unit collected":reason;actionUntil=Time.time+.4f;if(ok){debris.transform.position=new Vector3((float)(point.X-WorldOrigin.Offset.x),(float)point.Y,(float)(point.Z-WorldOrigin.Offset.z))+Vector3.up*.04f;debris.transform.rotation=Quaternion.Euler(-90,0,0);var main=debris.main;main.startColor=kind==ResourceKind.Stone?new Color(.4f,.41f,.39f):new Color(.45f,.30f,.15f);debris.Emit(12);}return ok;}
        public void Interact(bool place)
        {
            var state=app.Living.Traveler;Vector3 eye=player.transform.position+Vector3.up*(controller.height-.15f);var ray=AimRay;actionUntil=Time.time+.4f;
            if(!place&&state.Tool==ToolKind.Pickaxe)dialogue?.Speak(19);
            if(place){if(state.Tool!=ToolKind.Crate){Message="Equip the test crate to place it (3 wood).";return;}if(Physics.Raycast(ray,out var ground,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)&&app.Living.Place((double)ground.point.x+WorldOrigin.Offset.x,ground.point.y,(double)ground.point.z+WorldOrigin.Offset.z,state.Yaw))Message="Placed a persistent test crate";else Message="Aim at nearby ground; placement needs 3 wood.";return;}
            float reach=Physics.Raycast(ray,out var obstruction,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)?obstruction.distance+.15f:4;
            if(app.Settlements?.Residents!=null&&app.Settlements.Residents.TryAttack(obstruction.collider,state.Tool,out string injury)){Message=injury??"Resident is no longer present";return;}
            if(app.Details!=null&&app.Details.TryMineBoulder(obstruction.collider,out int stone)){Message=stone>0?$"Collected {stone} Stone":"Equip a pickaxe to mine this boulder";return;}
            if(app.Details!=null)foreach(var site in app.Details.Sites)
            {if(site.Kind>NaturalKind.Boulder)continue;var p=WorldOrigin.Relative(new Vector3((float)site.Position.X,site.Height+(site.Kind==NaturalKind.Boulder?.42f*site.Scale:.25f),(float)site.Position.Y));float t=Vector3.Dot(p-ray.origin,ray.direction),radius=site.Kind==NaturalKind.Boulder?site.Scale*1.15f:site.Kind==NaturalKind.Log?1.8f*site.Scale:.65f;float perpendicular=Vector3.SqrMagnitude(ray.GetPoint(t)-p);if(perpendicular>radius*radius||t+radius<0)continue;float contact=Mathf.Max(0,t-Mathf.Sqrt(radius*radius-perpendicular));if(contact>4||contact>reach)continue;ToolKind required=site.Kind==NaturalKind.Boulder?ToolKind.Pickaxe:site.Kind==NaturalKind.Log?ToolKind.Axe:ToolKind.Hands;int taken=app.Living.Harvest(site.Id,site.Resource,site.Quantity,required,app.Clock.State.Tick);Message=taken>0?$"Collected {taken} {site.Resource}":"Depleted, or equip "+required;return;}
            int cellX=(int)Math.Floor(app.Living.Traveler.X/16),cellZ=(int)Math.Floor(app.Living.Traveler.Z/16);
            for(int z=cellZ-1;z<=cellZ+1;z++)for(int x=cellX-1;x<=cellX+1;x++)if(VegetationDistribution.TryTree(app.Terrain.Geography,x,z,out var tree,true)&&!app.Living.TreeDepleted(tree.East,tree.North,app.Clock.State.Tick))
            {int region=app.Ecology.RegionAt(new GeoPoint(tree.East,tree.North));if(region<0||LivingWorldState.Hash(x,z,303)>Math.Min(1,app.Ecology.Forest(region)*5)*(1-app.Ecology.Burn(region)))continue;var p=WorldOrigin.Relative(new Vector3((float)tree.East,tree.Height+Mathf.Clamp(eye.y-tree.Height,0,tree.Scale*10),(float)tree.North));float t=Vector3.Dot(p-ray.origin,ray.direction);if(t<0||t>reach||Vector3.Distance(ray.GetPoint(t),p)>.6)continue;int taken=app.Living.Harvest("tree:"+x+":"+z,ResourceKind.Wood,30,ToolKind.Axe,app.Clock.State.Tick,5,true);if(taken>0)app.Ecology.HarvestForest(region,taken/30.0*.00003/Math.Max(.001,app.Ecology.EnvironmentAt(region).Area));Message=taken>0?$"Harvested {taken} wood from a standing tree":"Equip an axe";return;}
            if(Physics.Raycast(ray,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
            {if(!IsTerrainCollider(hit.collider)){Message="Aim at solid terrain to excavate.";return;}var kind=DigMaterial(hit);
                if(state.Tool==ToolKind.Stick){Message="The strike hit the ground.";return;}
                if(kind==ResourceKind.Stone&&state.Tool==ToolKind.Pickaxe){if(Vector3.Distance(lastImpact,hit.point)>1)stoneEffort=0;lastImpact=hit.point;if(++stoneEffort<3){Message="Stone impact "+stoneEffort+" / 3";return;}stoneEffort=0;}
                DigAt(new SolidPoint((double)hit.point.x+WorldOrigin.Offset.x,hit.point.y,(double)hit.point.z+WorldOrigin.Offset.z),kind);
            }
            else Message="Aim within "+UnitFormatting.Length(4,app.Settings.Units,"0.0")+".";
        }
        private void LateUpdate()
        {
            if(app?.Living==null||!app.Living.Traveler.Exploring){if(firstTool!=null)firstTool.SetActive(false);return;}
            var s=app.Living.Traveler;player.transform.rotation=Quaternion.Euler(0,(float)s.Yaw,0);double depth=LocalSolidField.RenderedGround(app.Terrain.Geography,s.X,s.Z)-s.Y;undergroundLamp.intensity=Mathf.SmoothStep(0,4,Mathf.InverseLerp(1.5f,6,(float)depth));undergroundLamp.transform.rotation=Quaternion.Euler((float)s.Pitch,(float)s.Yaw,0);soldier.ToolEquipped=s.Tool!=ToolKind.Hands;soldier.ToolActionProgress=Mathf.Clamp01(1-(actionUntil-Time.time)/.4f);soldier.SetFirstPerson(s.FirstPerson);soldier.FirstPersonAim=Quaternion.Euler((float)s.Pitch,(float)s.Yaw,0);soldier.Animate(walkBlend,running,controller.isGrounded,Flying,vertical,Time.time<actionUntil,s.Crouched,Mathf.Min(.05f,Time.unscaledDeltaTime));
            var rotation=Quaternion.Euler((float)s.Pitch,(float)s.Yaw,0);Vector3 eye=IsDoctor?soldier.EyePosition:player.transform.position+Vector3.up*(controller.height-.15f);Vector3 target;
            if(s.FirstPerson){target=eye+Vector3.up*Mathf.Sin(phase*2)*app.Settings.HeadBob*walkBlend*(controller.isGrounded?1:0);body.SetActive(IsDoctor);}
            else{body.SetActive(true);var offset=(s.ReverseThirdPerson?Quaternion.Euler(-(float)s.Pitch,(float)s.Yaw,0):rotation)*new Vector3(0,.3f,s.ReverseThirdPerson?4:-4);if(Physics.SphereCast(eye,.18f,offset.normalized,out var hit,offset.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))offset=offset.normalized*Mathf.Max(.35f,hit.distance-.15f);target=eye+offset;}
            camera.transform.position=target;camera.transform.rotation=s.FirstPerson?rotation:Quaternion.LookRotation(eye+rotation*Vector3.forward*(s.ReverseThirdPerson?0:2)-camera.transform.position);camera.nearClipPlane=CameraRays.PrecisionNear(camera.transform.position);camera.farClipPlane=180000;
            int tool=(int)s.Tool;held.sharedMesh=tools[tool];held.gameObject.SetActive(tool>0);firstHeld.sharedMesh=tools[tool];firstTool.SetActive(s.FirstPerson&&!IsDoctor&&tool>0&&app.Menu.Page==ApplicationPage.World);firstTool.transform.localPosition=new Vector3(.28f,-.32f,.58f);firstTool.transform.localRotation=EquipmentGrip012.ViewModelRotation(s.Tool,Time.time<actionUntil,soldier.ToolActionProgress);held.transform.localRotation=EquipmentGrip012.Rotation(s.Tool,IsDoctor,IsDoctor&&s.FirstPerson,Time.time<actionUntil,soldier.ToolActionProgress,s.Crouched&&!s.FirstPerson?60:0);
        }
        private void OnGUI()
        {
            if(app?.Living==null||app.Menu.Page!=ApplicationPage.World)return;var old=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));var state=app.Living.Traveler;
            if(state.Exploring)
            {if(!state.ReverseThirdPerson&&!GameInput.Blocked)GUI.Label(new Rect(626,346,30,30),"+");
                if(!InventoryOpen&&!GameInput.TextEntry){for(int i=0;i<6;i++){var rect=new Rect(370+i*90,655,86,45);var color=GUI.color;if(i==(int)state.Tool)GUI.color=new Color(1,.85f,.5f);if(GUI.Button(rect,ToolNames[i]))state.Tool=(ToolKind)i;GUI.color=color;}if(Flying)GUI.Label(new Rect(540,625,250,25),UnitFormatting.Speed(app.Settings.FlightSpeed,app.Settings.Units));}
                if(pendingSpawn)GUI.Label(new Rect(490,590,350,24),"Preparing collision…");else if(Time.unscaledTime<messageUntil&&!GameInput.TextEntry)GUI.Label(new Rect(390,595,620,25),Message);}

            if(InventoryOpen&&state.Exploring)
            {GUI.Box(new Rect(365,165,550,420),"Experimental inventory");GUI.Label(new Rect(390,205,500,50),$"Wood {state.Wood} · Stone {state.Stone} · Soil {state.Soil} · Sand {state.Sand}");state.Tool=(ToolKind)GUI.SelectionGrid(new Rect(390,265,500,86),(int)state.Tool,ToolNames,3);GUI.Label(new Rect(390,370,500,80),"Starter tools are prototypes. Shovel: soil/sand. Pickaxe: three stone strikes. Axe: wood. Hands: branches. Test crate: right-click nearby ground, 3 wood.");if(GUI.Button(new Rect(390,465,235,36),"Recover to nearby surface")){InventoryOpen=false;EnterAt(new GeoPoint(state.X,state.Z));}if(GUI.Button(new Rect(650,465,235,36),"Top-Down Mode"))Exit();if(GUI.Button(new Rect(390,520,495,36),"Close · E"))InventoryOpen=false;}
            GUI.matrix=old;
        }
        private void OnDestroy(){if(dialogue!=null)Destroy(dialogue);if(viewTransition!=null)Destroy(viewTransition);if(targeting!=null)Destroy(targeting);if(app?.Excavations!=null)app.Excavations.GroundOwnershipChanged-=RefreshGroundOwnership;if(groundJob!=null)Observe(groundJob);if(player!=null)Destroy(player);if(groundObject!=null)Destroy(groundObject);if(firstTool!=null)Destroy(firstTool);if(debris!=null)Destroy(debris.gameObject);if(groundMesh!=null)Destroy(groundMesh);foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
