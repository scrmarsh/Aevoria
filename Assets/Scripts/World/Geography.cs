using System;

namespace Aevoria.World
{
    // X east, Y north, elevation in meters. Rendering maps (X,Y,Z) to (X,Z,Y).
    public readonly struct GeoPoint
    {
        public readonly double X;
        public readonly double Y;
        public GeoPoint(double x, double y) { X = x; Y = y; }
    }

    public readonly struct ChunkKey : IEquatable<ChunkKey>
    {
        public readonly int X;
        public readonly int Y;
        public ChunkKey(int x, int y) { X = x; Y = y; }
        public bool Equals(ChunkKey other) { return X == other.X && Y == other.Y; }
        public override bool Equals(object obj) { return obj is ChunkKey other && Equals(other); }
        public override int GetHashCode() { unchecked { return X * 397 ^ Y; } }
    }

    public enum Biome : byte { Ocean, Coast, Grassland, Forest, Desert, Mountain, Tundra, BorealForest, TropicalForest, Savanna, Shrubland, Alpine, Wetland, Snow, TropicalSeasonalForest,TemperateConiferousForest,ColdDesert,Mediterranean,
        MediterraneanWoodland,MediterraneanScrub,RiparianForest,RockyCoast,SandyCoast,Steppe,RockyDesert,SaltFlat,Oasis,AlpineScree }

    public readonly struct GeographicSample
    {
        public readonly float ElevationMeters;
        public readonly float Moisture;
        public readonly float TemperatureCelsius;
        public readonly float Fertility;
        public readonly Biome Biome;
        public GeographicSample(float elevation, float moisture, float temperature, float fertility, Biome biome)
        {
            ElevationMeters = elevation; Moisture = moisture; TemperatureCelsius = temperature;
            Fertility = fertility; Biome = biome;
        }
    }

    public sealed class WorldConfiguration
    {
        public string Seed { get; }
        public int ChunksX { get; }
        public int ChunksY { get; }
        public int CellsPerChunk { get; }
        public double CellSizeMeters { get; }
        public double WidthMeters => ChunksX * CellsPerChunk * CellSizeMeters;
        public double DepthMeters => ChunksY * CellsPerChunk * CellSizeMeters;
        public double ChunkSizeMeters => CellsPerChunk * CellSizeMeters;
        public GenerationSettings Terrain { get; }
        public WorldConfiguration(string seed, int chunksX, int chunksY, int cellsPerChunk, double cellSizeMeters)
            : this(seed, chunksX, chunksY, cellsPerChunk, cellSizeMeters, new GenerationSettings()) { }

        public WorldConfiguration(string seed, int chunksX, int chunksY, int cellsPerChunk, double cellSizeMeters, GenerationSettings terrain)
        {
            if (string.IsNullOrWhiteSpace(seed) || seed.Length > 128 || chunksX < 1 || chunksY < 1 ||
                chunksX > 4000 || chunksY > 4000 || (chunksX>128||chunksY>128)&&!(chunksX==chunksY&&(chunksX==250||chunksX==500||chunksX==1000||chunksX==2000||chunksX==4000)&&cellsPerChunk==64&&cellSizeMeters==8) || cellsPerChunk < 8 || cellsPerChunk > 128 ||
                (cellsPerChunk & (cellsPerChunk - 1)) != 0 || !Finite(cellSizeMeters) || cellSizeMeters < 1 || cellSizeMeters > 128 || terrain == null)
                throw new ArgumentException("Invalid world configuration.");
            Seed = seed; ChunksX = chunksX; ChunksY = chunksY;
            CellsPerChunk = cellsPerChunk; CellSizeMeters = cellSizeMeters;
            Terrain = terrain;
        }
        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        public bool Contains(GeoPoint p) => Math.Abs(p.X) <= WidthMeters / 2 && Math.Abs(p.Y) <= DepthMeters / 2;
        public GeoPoint ChunkOrigin(ChunkKey key) => new GeoPoint(key.X * ChunkSizeMeters, key.Y * ChunkSizeMeters);
        public ChunkKey ChunkAt(GeoPoint point)
        {
            double side = CellsPerChunk * CellSizeMeters;
            return new ChunkKey((int)Math.Floor(point.X / side), (int)Math.Floor(point.Y / side));
        }
    }

    public interface IGeography
    {
        WorldConfiguration Configuration { get; }
        float SeaLevel { get; }
        float ElevationAt(double east, double north);
        TerrainRegion RegionAt(GeoPoint point);
        GeographicSample Sample(GeoPoint point);
    }

    // Flat, data-only Phase 0 geography. It is not the future terrain generator.
    public sealed class PlaceholderGeography : IGeography
    {
        public WorldConfiguration Configuration { get; }
        public float SeaLevel => Configuration.Terrain.SeaLevel;
        public float ElevationAt(double east, double north) => 0;
        public TerrainRegion RegionAt(GeoPoint point) => TerrainRegion.Plain;
        public PlaceholderGeography(WorldConfiguration configuration) { Configuration = configuration; }
        public GeographicSample Sample(GeoPoint point)
        {
            return new GeographicSample(0, 0.5f, 18, 0.7f, Biome.Grassland);
        }
    }
}
