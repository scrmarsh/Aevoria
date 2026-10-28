using System;
using System.Collections.Generic;
using Aevoria.World;
using UnityEngine;
namespace Aevoria.Presentation
{
    public sealed class SettlementResidents:MonoBehaviour
    {
        public const int ActorLimit=24;
        private sealed class Actor
        {public SettlementState Settlement;public ResidentState Record;public GameObject Body;public CharacterController Controller;public HumanoidPresentation Rig;public SettlementPathNetwork Network;public int Path,Point,Direction=1,Journey;public float IdleUntil,Stuck;}
        private AevoriaBootstrap app;private SettlementPathPresentation paths;private float refresh;
        private readonly Dictionary<string,Actor> actors=new Dictionary<string,Actor>();private readonly Dictionary<Collider,Actor> colliders=new Dictionary<Collider,Actor>();private readonly HashSet<string> wanted=new HashSet<string>();
        public int VisibleResidents=>actors.Count;public IEnumerable<string> ResidentIds=>actors.Keys;
        public void Initialize(AevoriaBootstrap value,SettlementPathPresentation navigation){app=value;paths=navigation;}
        public bool TryPosition(string id,out Vector3 position){if(actors.TryGetValue(id,out var actor)){position=new Vector3((float)actor.Record.X,(float)actor.Record.Y,(float)actor.Record.Z);return true;}position=default;return false;}
        public bool TryAttack(Collider collider,ToolKind tool,out string message)
        {
            message=null;if(collider==null||!colliders.TryGetValue(collider,out var actor))return false;
            double damage=tool==ToolKind.Stick?35:tool==ToolKind.Axe||tool==ToolKind.Pickaxe?30:tool==ToolKind.Shovel?15:10;
            if(!app.Society.DamageResident(actor.Settlement.Id,actor.Record.Id,damage)){Remove(actor.Record.Id);return true;}
            message=actor.Record.Health>0?"Resident health "+actor.Record.Health.ToString("0")+" / 100":"Resident killed";if(actor.Record.Health<=0)Remove(actor.Record.Id);return true;
        }
        private void Update()
        {
            if(app?.Society==null)return;bool active=app.Menu.Page==ApplicationPage.World&&(app.Living.Traveler.Exploring||app.Controller.Distance<400);
            if(!active){if(actors.Count>0)Clear();return;}var focus=app.Living.Traveler.Exploring?app.Explorer.Position:app.Controller.Focus;
            if(Time.unscaledTime>=refresh){refresh=Time.unscaledTime+.5f;Refresh(focus);}
            float dt=Mathf.Min(.05f,Time.unscaledDeltaTime);foreach(var actor in actors.Values)
            {
                actor.Body.transform.position=Relative(actor.Record);
                if(actor.Record.Health<=0)continue;float movement=0;
                if(app.Clock.Running&&Time.unscaledTime>=actor.IdleUntil)movement=Walk(actor,dt);
                actor.Rig.Animate(movement,false,true,false,0,false,false,dt,.415f);
            }
        }
        private void Refresh(Vector3 focus)
        {
            wanted.Clear();var settlements=new List<SettlementState>();foreach(var settlement in app.Society.Settlements)if(Distance(focus,settlement.X,settlement.Z)<320)settlements.Add(settlement);
            settlements.Sort((a,b)=>Distance(focus,a.X,a.Z).CompareTo(Distance(focus,b.X,b.Z)));
            foreach(var settlement in settlements)
            {
                var network=paths.Network(settlement.Id);if(network==null||network.Paths.Count==0)continue;int target=Math.Min(settlement.Residents.Count,Math.Max(1,(int)Math.Ceiling(settlement.Population*.2)));
                for(int i=0;i<target&&wanted.Count<ActorLimit;i++)
                {
                    var record=settlement.Residents[i];if(record.Health<=0)continue;wanted.Add(record.Id);
                    if(!actors.TryGetValue(record.Id,out var actor)){actor=Create(settlement,record,network);actors.Add(record.Id,actor);colliders.Add(actor.Controller,actor);}
                    else if(!ReferenceEquals(actor.Network,network)){actor.Network=network;AttachToPath(actor);}
                }
            }
            var remove=new List<string>();foreach(var id in actors.Keys)if(!wanted.Contains(id))remove.Add(id);foreach(string id in remove)Remove(id);
        }
        private static double Distance(Vector3 p,double x,double z)=>Math.Sqrt((p.x-x)*(p.x-x)+(p.z-z)*(p.z-z));
        private static Vector3 Relative(ResidentState record)=>new Vector3((float)(record.X-WorldOrigin.Offset.x),(float)record.Y,(float)(record.Z-WorldOrigin.Offset.z));
        private Actor Create(SettlementState settlement,ResidentState record,SettlementPathNetwork network)
        {
            var body=new GameObject("Resident · "+record.Id);body.transform.SetParent(transform,false);var controller=body.AddComponent<CharacterController>();controller.enabled=false;controller.height=1.75f;controller.center=Vector3.up*.875f;controller.radius=.27f;controller.skinWidth=.025f;controller.stepOffset=.25f;controller.slopeLimit=45;controller.minMoveDistance=0;
            var visual=new GameObject("Resident appearance");visual.transform.SetParent(body.transform,false);var rig=visual.AddComponent<HumanoidPresentation>();rig.Initialize(CharacterAppearance.Explorer);
            var palette=new[]{new Color(.32f,.23f,.14f),new Color(.18f,.24f,.31f),new Color(.35f,.29f,.22f),new Color(.29f,.20f,.23f)};rig.SetClothColor(palette[(int)(record.Number%palette.Length)]);
            var actor=new Actor{Body=body,Controller=controller,Rig=rig,Record=record,Settlement=settlement,Network=network};
            if(!record.HasPosition)
            {
                int path=(int)(record.Number*7%network.Paths.Count);var points=network.Paths[path].Points;int segment=(int)(record.Number*13%Math.Max(1,points.Count-1));double t=.15+(record.Number*.61803398875%1)*.7;
                record.X=points[segment].X+(points[segment+1].X-points[segment].X)*t;record.Z=points[segment].Y+(points[segment+1].Y-points[segment].Y)*t;record.Y=LocalSolidField.RenderedGround(app.Terrain.Geography,record.X,record.Z)+.05;record.HasPosition=true;
            }
            body.transform.position=Relative(record);AttachToPath(actor);controller.enabled=true;return actor;
        }
        private void AttachToPath(Actor actor)
        {
            double best=double.MaxValue;for(int path=0;path<actor.Network.Paths.Count;path++)for(int point=0;point<actor.Network.Paths[path].Points.Count;point++)
            {var p=actor.Network.Paths[path].Points[point];double d=(p.X-actor.Record.X)*(p.X-actor.Record.X)+(p.Y-actor.Record.Z)*(p.Y-actor.Record.Z);if(d<best){best=d;actor.Path=path;actor.Point=point;}}
            actor.Direction=actor.Record.Number%2==0?1:-1;actor.Stuck=0;
        }
        private void NextRoute(Actor actor)
        {
            var previous=actor.Network.Paths[actor.Path];string endpoint=actor.Direction>0?previous.To:previous.From;var choices=new List<int>();for(int i=0;i<actor.Network.Paths.Count;i++)if(i!=actor.Path&&(actor.Network.Paths[i].From==endpoint||actor.Network.Paths[i].To==endpoint))choices.Add(i);
            if(choices.Count>0){actor.Path=choices[(int)((actor.Record.Number+actor.Journey++)%choices.Count)];var next=actor.Network.Paths[actor.Path];actor.Direction=next.From==endpoint?1:-1;actor.Point=actor.Direction>0?0:next.Points.Count-1;}
            else {actor.Direction=-actor.Direction;actor.Point=actor.Direction>0?0:previous.Points.Count-1;}
            actor.IdleUntil=Time.unscaledTime+.3f+(float)(actor.Record.Number%4)*.2f;
        }
        private float Walk(Actor actor,float dt)
        {
            var points=actor.Network.Paths[actor.Path].Points;if(actor.Point<0||actor.Point>=points.Count){NextRoute(actor);return 0;}var target=points[actor.Point];double dx=target.X-actor.Record.X,dz=target.Y-actor.Record.Z,length=Math.Sqrt(dx*dx+dz*dz);
            if(length<.35){actor.Point+=actor.Direction;return 0;}float speed=1.4f;var desired=new Vector3((float)(dx/length),0,(float)(dz/length));var lateral=new Vector3(desired.z,0,-desired.x)*(actor.Record.Number%2==0?.15f:-.15f);var motion=(desired+lateral)*speed*dt;
            double x=actor.Record.X+motion.x,z=actor.Record.Z+motion.z;double ground=LocalSolidField.RenderedGround(app.Terrain.Geography,x,z);
            if(app.Living.SolidDensity(x,ground-.15,z)<0){actor.Stuck+=dt;motion=Vector3.zero;}else motion.y=Mathf.Clamp((float)(ground+.04-actor.Record.Y),-.8f,.3f);
            var before=actor.Body.transform.position;actor.Controller.Move(motion);var travelled=actor.Body.transform.position-before;float distance=new Vector2(travelled.x,travelled.z).magnitude;
            if(distance<speed*dt*.1f)actor.Stuck+=dt;else actor.Stuck=0;
            if(actor.Stuck>1.5f){actor.Direction=-actor.Direction;actor.Point=Mathf.Clamp(actor.Point+actor.Direction*2,0,points.Count-1);actor.IdleUntil=Time.unscaledTime+.3f;actor.Stuck=0;}
            if(distance>.0001f)actor.Body.transform.rotation=Quaternion.Slerp(actor.Body.transform.rotation,Quaternion.LookRotation(new Vector3(travelled.x,0,travelled.z)),1-Mathf.Exp(-10*dt));
            var location=actor.Body.transform.position;actor.Record.X=(double)location.x+WorldOrigin.Offset.x;actor.Record.Y=location.y;actor.Record.Z=(double)location.z+WorldOrigin.Offset.z;return Mathf.Clamp01(distance/Mathf.Max(.001f,speed*dt));
        }
        private void Remove(string id)
        {if(!actors.TryGetValue(id,out var actor))return;colliders.Remove(actor.Controller);if(actor.Body!=null){actor.Body.SetActive(false);Destroy(actor.Body);}actors.Remove(id);}
        private void Clear(){var ids=new List<string>(actors.Keys);foreach(string id in ids)Remove(id);}
        private void OnGUI()
        {
            if(app?.Explorer?.AimingAvailable!=true||app.Menu.Page!=ApplicationPage.World)return;
            if(!Physics.Raycast(app.Explorer.AimRay,out var hit,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore)||!colliders.TryGetValue(hit.collider,out var actor))return;
            GUI.Label(new Rect(Screen.width*.5f-80,Screen.height*.5f+25,250,28),"Resident · "+actor.Record.Health.ToString("0")+" / 100");
        }
        private void OnDestroy(){Clear();}
    }
}
