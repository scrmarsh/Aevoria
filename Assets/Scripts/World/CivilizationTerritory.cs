using System;
using System.Collections.Generic;
using System.Threading;

namespace Aevoria.World
{
    public sealed partial class CivilizationModel
    {
        private bool[] habitable;
        private double[] terrainCost;
        private byte[] territoryLinks;
        private double[] resourcePotential;
        private double territoryStepX,territoryStepZ;
        private void PrepareTerritory(CancellationToken token)
        {
            int n=state.TerritoryResolution;habitable=new bool[n*n];terrainCost=new double[n*n];territoryLinks=new byte[n*n];resourcePotential=new double[n*n*4];territoryStepX=geography.Configuration.WidthMeters/n;territoryStepZ=geography.Configuration.DepthMeters/n;
            for(int z=0;z<n;z++){token.ThrowIfCancellationRequested();for(int x=0;x<n;x++)
            {
                int i=z*n+x;var p=TerritoryCenter(i);var sample=geography.Sample(p);float h=sample.ElevationMeters;
                habitable[i]=h>geography.SeaLevel+3&&h<3200&&sample.TemperatureCelsius> -15&&!geography.InlandSurfaces.LakeAt(p.X,p.Y,out _);
                double gradient=(Math.Abs(geography.ElevationAt(p.X+100,p.Y)-geography.ElevationAt(p.X-100,p.Y))+Math.Abs(geography.ElevationAt(p.X,p.Y+100)-geography.ElevationAt(p.X,p.Y-100)))/400;
                terrainCost[i]=1+Math.Min(8,gradient*8)+Math.Max(0,h-300)/700+Math.Max(0,8-sample.TemperatureCelsius)/12;
                if(habitable[i]){resourcePotential[i*4]=sample.Fertility*CivilIdentity.Clamp(1-Math.Abs(sample.TemperatureCelsius-17)/28.0);resourcePotential[i*4+1]=sample.Moisture*(sample.Biome==Biome.Forest||sample.Biome==Biome.TropicalForest||sample.Biome==Biome.TropicalSeasonalForest||sample.Biome==Biome.TemperateConiferousForest||sample.Biome==Biome.BorealForest?.85:.2);resourcePotential[i*4+2]=CivilIdentity.Clamp((h-100)/1800.0+.12);resourcePotential[i*4+3]=resourcePotential[i*4+2]*(.2+geography.SiteRandom((int)Math.Floor(p.X/2048),(int)Math.Floor(p.Y/2048),972)*.8);}
            }}
            for(int z=0;z<n;z++){token.ThrowIfCancellationRequested();for(int x=0;x<n;x++)
            {
                int i=z*n+x;if(!habitable[i])continue;var a=TerritoryCenter(i);
                for(int edge=0;edge<4;edge++)
                {
                    int nx=x+(edge==0?-1:edge==1?1:0),nz=z+(edge==2?-1:edge==3?1:0);if(nx<0||nz<0||nx>=n||nz>=n)continue;int next=nz*n+nx;if(!habitable[next])continue;var b=TerritoryCenter(next);bool connected=true;
                    for(int probe=1;probe<=3;probe++){double east=a.X+(b.X-a.X)*probe/4,north=a.Y+(b.Y-a.Y)*probe/4;float height=geography.ElevationAt(east,north);if(height<=geography.SeaLevel+2||height>3200||geography.InlandSurfaces.LakeAt(east,north,out _)){connected=false;break;}}
                    if(connected)territoryLinks[i]|=(byte)(1<<edge);
                }
            }}
        }
        public GeoPoint TerritoryCenter(int index)
        {
            int n=state.TerritoryResolution;return new GeoPoint(((index%n+.5)/n-.5)*geography.Configuration.WidthMeters,((index/n+.5)/n-.5)*geography.Configuration.DepthMeters);
        }
        public int TerritoryCell(double x,double z)
        {
            var c=geography.Configuration;if(!c.Contains(new GeoPoint(x,z)))return -1;int n=state.TerritoryResolution,ix=Math.Min(n-1,Math.Max(0,(int)((x/c.WidthMeters+.5)*n))),iz=Math.Min(n-1,Math.Max(0,(int)((z/c.DepthMeters+.5)*n)));return iz*n+ix;
        }
        public int OwnerAt(double x,double z){int i=TerritoryCell(x,z);return i<0||state.Territory==null?-1:state.Territory[i];}
        public int OwnerCell(int index)=>state.Territory[index];
        public double CellResourcePotential(int index,int resource)=>resourcePotential[index*4+resource];
        public double CellPopulationDensity(int index)
        {
            int owner=state.Territory[index];if(owner<0)return 0;var civ=state.Civilizations[owner];return civ.Population/Math.Max(1,civ.ControlledSquareKilometres);
        }
        private void RebuildTerritory(bool recordEvents)
        {
            int n=state.TerritoryResolution,count=n*n;var owner=new int[count];var best=new double[count];for(int i=0;i<count;i++){owner[i]=-1;best[i]=double.PositiveInfinity;}
            for(int c=0;c<state.Civilizations.Count;c++)
            {
                var civ=state.Civilizations[c];if(civ.Population<1)continue;double reach=6000+Math.Sqrt(civ.Population)*500+civ.Development*7000;
                var distance=new double[count];for(int i=0;i<count;i++)distance[i]=double.PositiveInfinity;var heap=new TerritoryHeap();
                foreach(string id in civ.Settlements){var settlement=Settlement(id);if(settlement.Population<1)continue;int cell=TerritoryCell(settlement.X,settlement.Z);distance[cell]=0;heap.Push(cell,0);}
                while(heap.Count>0)
                {
                    var at=heap.Pop();int i=at.Index;if(at.Cost!=distance[i]||at.Cost>1)continue;
                    if(at.Cost<best[i]||(at.Cost==best[i]&&(owner[i]<0||c<owner[i]))){best[i]=at.Cost;owner[i]=c;}
                    // A coarse coastal seed cell may include water. It can anchor
                    // a settlement but never becomes a bridge across the ocean.
                    if(!habitable[i])continue;int x=i%n,z=i/n;
                    for(int edge=0;edge<4;edge++)
                    {
                        if((territoryLinks[i]&(1<<edge))==0)continue;int nx=x+(edge==0?-1:edge==1?1:0),nz=z+(edge==2?-1:edge==3?1:0);if(nx<0||nz<0||nx>=n||nz>=n)continue;int next=nz*n+nx;if(!habitable[next])continue;
                        double cost=at.Cost+(edge<2?territoryStepX:territoryStepZ)*(terrainCost[i]+terrainCost[next])*.5/reach;if(cost>=distance[next]||cost>1)continue;distance[next]=cost;heap.Push(next,cost);
                    }
                }
            }
            state.Territory=owner;var areas=new double[state.Civilizations.Count];double area=territoryStepX*territoryStepZ/1000000;foreach(int c in owner)if(c>=0)areas[c]+=area;
            for(int c=0;c<areas.Length;c++){var civ=state.Civilizations[c];double previous=civ.ControlledSquareKilometres;civ.ControlledSquareKilometres=areas[c];if(recordEvents&&Math.Abs(previous-areas[c])>=area*.99)Emit(areas[c]>previous?SocialEventKind.TerritoryExpansion:SocialEventKind.TerritoryContraction,civ,null,civ.Name+(areas[c]>previous?" expands":" contracts"),FormattableString.Invariant($"Geographic influence changed from {previous:0} to {areas[c]:0} square kilometres."),previous,areas[c]);}
        }
        private void UpdateRelationships()
        {
            int n=state.TerritoryResolution;var borders=new double[state.Civilizations.Count,state.Civilizations.Count];
            for(int z=0;z<n;z++)for(int x=0;x<n;x++){int a=state.Territory[z*n+x];if(a<0)continue;for(int edge=0;edge<2;edge++){int nx=x+(edge==0?1:0),nz=z+(edge==1?1:0);if(nx>=n||nz>=n)continue;int b=state.Territory[nz*n+nx];if(b>=0&&b!=a){double length=(edge==0?territoryStepZ:territoryStepX)/1000;borders[a,b]+=length;borders[b,a]+=length;}}}
            foreach(var relation in state.Relationships)
            {
                int ai=state.Civilizations.FindIndex(c=>c.Id==relation.A),bi=state.Civilizations.FindIndex(c=>c.Id==relation.B);var a=state.Civilizations[ai];var b=state.Civilizations[bi];relation.SharedBorderKilometres=borders[ai,bi];double pressure=0;int settlements=0;
                foreach(var s in state.Settlements)if(s.CivilizationId==a.Id||s.CivilizationId==b.Id){pressure+=1-s.Prosperity;settlements++;}relation.Competition=settlements>0?pressure/settlements:0;
                double target=relation.SharedBorderKilometres>0?20-relation.Competition*100:Distance(a.OriginX,a.OriginZ,b.OriginX,b.OriginZ)<160000?5:0;
                relation.Value=CivilIdentity.Clamp(relation.Value+(target-relation.Value)*.08,-100,100);var previous=relation.Disposition;relation.Disposition=relation.Value< -25?DiplomaticDisposition.Hostile:relation.Value>15?DiplomaticDisposition.Friendly:DiplomaticDisposition.Neutral;
                if(previous!=relation.Disposition)Emit(SocialEventKind.RelationshipChanged,a,null,a.Name+" / "+b.Name,FormattableString.Invariant($"Relations became {relation.Disposition}; shared border {relation.SharedBorderKilometres:0} km and resource pressure {relation.Competition:P0} explain the current trend."),(int)previous,(int)relation.Disposition,b.Id);
            }
            foreach(var c in state.Civilizations){double total=0;int count=0;foreach(var s in state.Settlements)if(s.CivilizationId==c.Id){total+=s.Prosperity;count++;}c.Cohesion=count==0?0:total/count;}
        }
        private readonly struct TerritoryNode{public readonly int Index;public readonly double Cost;public TerritoryNode(int i,double c){Index=i;Cost=c;}}
        private sealed class TerritoryHeap
        {
            private readonly List<TerritoryNode> data=new List<TerritoryNode>();public int Count=>data.Count;
            private static bool Before(TerritoryNode a,TerritoryNode b)=>a.Cost<b.Cost||a.Cost==b.Cost&&a.Index<b.Index;
            public void Push(int index,double cost){var value=new TerritoryNode(index,cost);int i=data.Count;data.Add(value);while(i>0){int p=(i-1)/2;if(!Before(value,data[p]))break;data[i]=data[p];i=p;}data[i]=value;}
            public TerritoryNode Pop(){var first=data[0];var value=data[data.Count-1];data.RemoveAt(data.Count-1);if(data.Count==0)return first;int i=0;while(true){int child=i*2+1;if(child>=data.Count)break;if(child+1<data.Count&&Before(data[child+1],data[child]))child++;if(!Before(data[child],value))break;data[i]=data[child];i=child;}data[i]=value;return first;}
        }
    }
}
